using TwitchHeists.Core.Models;
using TwitchHeists.Core.Options;
using TwitchHeists.Core.Services;

namespace TwitchHeists.Tests;

public sealed class HeistResolverTests
{
    [Fact]
    public void Resolve_RoundsResolvedPotAndPayoutsToWholeNumbers()
    {
        var randomValues = new Queue<double>(new[] { 0.01, 0.10, 0.20 });
        var resolver = new HeistResolver(
            new HeistSettings
            {
                MinimumPlayers = 2,
                MaximumWinnerCount = 2,
                SuccessfulPotMultiplier = 1.5m,
                WinnerBands = new List<HeistWinnerBand> { new(2, 2, 2, 2) }
            },
            () => randomValues.Dequeue());
        var participants = new[]
        {
            CreateParticipant("smallstake", 1m),
            CreateParticipant("bigstake", 2m)
        };

        var result = resolver.Resolve(
            Guid.NewGuid(),
            participants,
            0.75m,
            new DateTimeOffset(2026, 4, 23, 20, 10, 0, TimeSpan.Zero));

        Assert.Equal(HeistRoundState.ResolvedSuccess, result.FinalState);
        Assert.Equal(5m, result.ResolvedPot);
        Assert.Equal(5m, result.Winners.Sum(winner => winner.PayoutAmount));
        Assert.All(result.Winners, winner => Assert.Equal(winner.PayoutAmount, decimal.Truncate(winner.PayoutAmount)));
    }

    [Fact]
    public void Resolve_MarksAllParticipantsAsLosersWhenTheHeistFails()
    {
        var randomValues = new Queue<double>(new[] { 0.95 });
        var resolver = new HeistResolver(
            new HeistSettings { MaximumWinnerCount = 5, SuccessfulPotMultiplier = 2.0m },
            () => randomValues.Dequeue());
        var participants = CreateParticipants(3, 1_000m);

        var result = resolver.Resolve(
            Guid.NewGuid(),
            participants,
            0.75m,
            new DateTimeOffset(2026, 4, 23, 20, 10, 0, TimeSpan.Zero));

        Assert.Equal(HeistRoundState.ResolvedFailure, result.FinalState);
        Assert.Empty(result.Winners);
        Assert.Equal(3, result.Losers.Count);
        Assert.Equal(3_000m, result.OriginalPot);
        Assert.Equal(3_000m, result.ResolvedPot);
    }

    [Fact]
    public void Resolve_DoublesThePotAndUsesTheConfiguredWinnerBandOnSuccess()
    {
        var randomValues = new Queue<double>(new[] { 0.01, 0.10, 0.20, 0.30, 0.40, 0.50 });
        var resolver = new HeistResolver(
            new HeistSettings { MaximumWinnerCount = 5, SuccessfulPotMultiplier = 2.0m },
            () => randomValues.Dequeue());
        var participants = CreateParticipants(10, 1_000m);

        var result = resolver.Resolve(
            Guid.NewGuid(),
            participants,
            0.75m,
            new DateTimeOffset(2026, 4, 23, 20, 10, 0, TimeSpan.Zero));

        Assert.Equal(HeistRoundState.ResolvedSuccess, result.FinalState);
        Assert.Equal(10_000m, result.OriginalPot);
        Assert.Equal(20_000m, result.ResolvedPot);
        Assert.Equal(3, result.Winners.Count);
        Assert.Equal(3, result.Winners.Count(winner => winner.PayoutAmount > 0));
        Assert.Equal(7, result.Losers.Count);
    }

    [Fact]
    public void Resolve_SplitsSuccessfulPayoutsProportionallyByStake()
    {
        var randomValues = new Queue<double>(new[] { 0.01, 0.10, 0.20 });
        var resolver = new HeistResolver(
            new HeistSettings
            {
                MinimumPlayers = 2,
                MaximumWinnerCount = 2,
                SuccessfulPotMultiplier = 2.0m,
                WinnerBands = new List<HeistWinnerBand> { new(2, 2, 2, 2) }
            },
            () => randomValues.Dequeue());
        var participants = new[]
        {
            CreateParticipant("smallstake", 1_000m),
            CreateParticipant("bigstake", 3_000m)
        };

        var result = resolver.Resolve(
            Guid.NewGuid(),
            participants,
            0.75m,
            new DateTimeOffset(2026, 4, 23, 20, 10, 0, TimeSpan.Zero));

        Assert.Equal(HeistRoundState.ResolvedSuccess, result.FinalState);
        Assert.Equal(8_000m, result.ResolvedPot);
        Assert.Collection(
            result.Winners.OrderBy(winner => winner.Identity.NormalizedUsername),
            winner =>
            {
                Assert.Equal("bigstake", winner.Identity.NormalizedUsername);
                Assert.Equal(6_000m, winner.PayoutAmount);
            },
            winner =>
            {
                Assert.Equal("smallstake", winner.Identity.NormalizedUsername);
                Assert.Equal(2_000m, winner.PayoutAmount);
            });
    }

    [Fact]
    public void Resolve_MarksASoloCrewAsInsufficientByDefaultAndRefundsTheStake()
    {
        var resolver = new HeistResolver(
            new HeistSettings { MinimumPlayers = 2, SuccessfulPotMultiplier = 2.0m },
            () => 0.01);
        var participant = CreateParticipant("starter", 1_000m);

        var result = resolver.Resolve(
            Guid.NewGuid(),
            new[] { participant },
            0.75m,
            new DateTimeOffset(2026, 4, 23, 20, 10, 0, TimeSpan.Zero));

        Assert.Equal(HeistRoundState.InsufficientCrew, result.FinalState);
        Assert.Empty(result.Winners);
        Assert.Empty(result.Losers);
        Assert.Collection(
            result.RefundedParticipants,
            refunded =>
            {
                Assert.Equal("starter", refunded.Identity.NormalizedUsername);
                Assert.Equal(1_000m, refunded.StakeAmount);
                Assert.Equal(0m, refunded.PayoutAmount);
            });
        Assert.Equal(1_000m, result.OriginalPot);
        Assert.Equal(1_000m, result.ResolvedPot);
    }

    [Fact]
    public void Resolve_RefundsAllParticipantsWhenCrewSizeIsBelowConfiguredMinimumPlayers()
    {
        var resolver = new HeistResolver(
            new HeistSettings { MinimumPlayers = 3, SuccessfulPotMultiplier = 2.0m },
            () => 0.01);
        var participants = CreateParticipants(2, 1_000m);

        var result = resolver.Resolve(
            Guid.NewGuid(),
            participants,
            0.75m,
            new DateTimeOffset(2026, 4, 23, 20, 10, 0, TimeSpan.Zero));

        Assert.Equal(HeistRoundState.InsufficientCrew, result.FinalState);
        Assert.Empty(result.Winners);
        Assert.Empty(result.Losers);
        Assert.Equal(2, result.RefundedParticipants.Count);
        Assert.All(result.RefundedParticipants, refunded => Assert.Equal(1_000m, refunded.StakeAmount));
        Assert.Equal(2_000m, result.ResolvedPot);
    }

    [Theory]
    [InlineData(2, 1, 2)]
    [InlineData(6, 3, 6)]
    [InlineData(21, 6, 10)]
    [InlineData(51, 12, 16)]
    [InlineData(101, 17, 26)]
    [InlineData(150, 21, 34)]
    [InlineData(250, 21, 34)]
    public void Resolve_UsesWinnerBandsForSuccessfulHeists(int participantCount, int minimumWinners, int maximumWinners)
    {
        var randomValues = new Queue<double>(Enumerable.Repeat(0.0, participantCount + 1));
        var resolver = new HeistResolver(
            new HeistSettings
            {
                MinimumPlayers = 2,
                MaximumWinnerCount = 200,
                SuccessfulPotMultiplier = 2.0m
            },
            () => randomValues.Dequeue());
        var participants = CreateParticipants(participantCount, 100m);

        var result = resolver.Resolve(
            Guid.NewGuid(),
            participants,
            0.75m,
            new DateTimeOffset(2026, 4, 23, 20, 10, 0, TimeSpan.Zero));

        Assert.Equal(HeistRoundState.ResolvedSuccess, result.FinalState);
        Assert.InRange(result.Winners.Count, minimumWinners, maximumWinners);
    }

    private static IReadOnlyList<HeistParticipant> CreateParticipants(int count, decimal stakeAmount)
    {
        var participants = new List<HeistParticipant>();

        for (var index = 1; index <= count; index++)
        {
            participants.Add(CreateParticipant($"viewer{index}", stakeAmount));
        }

        return participants;
    }

    private static HeistParticipant CreateParticipant(string normalizedUsername, decimal stakeAmount)
    {
        return new HeistParticipant
        {
            Identity = new ViewerIdentity
            {
                Username = normalizedUsername,
                NormalizedUsername = normalizedUsername,
                DisplayName = normalizedUsername
            },
            StakeAmount = stakeAmount,
            JoinedAtUtc = new DateTimeOffset(2026, 4, 23, 20, 0, 0, TimeSpan.Zero)
        };
    }
}
