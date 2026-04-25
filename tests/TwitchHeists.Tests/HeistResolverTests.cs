using TwitchHeists.Core.Models;
using TwitchHeists.Core.Options;
using TwitchHeists.Core.Services;

namespace TwitchHeists.Tests;

public sealed class HeistResolverTests
{
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
    public void Resolve_DoublesThePotAndCapsTheWinnerCountOnSuccess()
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
        Assert.Equal(5, result.Winners.Count);
        Assert.All(result.Winners, winner => Assert.Equal(4_000m, winner.PayoutAmount));
        Assert.Equal(5, result.Losers.Count);
    }

    [Fact]
    public void Resolve_SplitsSuccessfulPayoutsProportionallyByStake()
    {
        var randomValues = new Queue<double>(new[] { 0.01, 0.10, 0.20 });
        var resolver = new HeistResolver(
            new HeistSettings { MaximumWinnerCount = 2, SuccessfulPotMultiplier = 2.0m },
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
