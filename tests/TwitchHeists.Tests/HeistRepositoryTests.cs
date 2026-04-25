using TwitchHeists.Core.Models;
using TwitchHeists.Data.Sqlite.Repositories;
using TwitchHeists.Data.Sqlite.Schema;

namespace TwitchHeists.Tests;

public sealed class HeistRepositoryTests : IDisposable
{
    private readonly string databasePath;

    public HeistRepositoryTests()
    {
        databasePath = Path.Combine(AppContext.BaseDirectory, $"{Guid.NewGuid():N}.heist.db");
    }

    [Fact]
    public void StartRound_ReservesTheStarterStakeAndCreatesAnOpenRound()
    {
        var repository = CreateRepository();
        repository.SetViewerBalance("starter", 5_000m);

        var roundId = repository.StartRound(
            CreateViewer("starter"),
            1_000m,
            new DateTimeOffset(2026, 4, 23, 20, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 4, 23, 20, 2, 0, TimeSpan.Zero));

        Assert.Equal(roundId, repository.GetOpenRoundId());
        Assert.Equal(4_000m, repository.GetViewerBalance("starter"));
        Assert.Single(repository.GetParticipants(roundId));
    }

    [Fact]
    public void JoinOpenRound_ReservesStakeAndRejectsDuplicateJoins()
    {
        var repository = CreateRepository();
        repository.SetViewerBalance("starter", 5_000m);
        repository.SetViewerBalance("joiner", 5_000m);
        var roundId = repository.StartRound(
            CreateViewer("starter"),
            1_000m,
            new DateTimeOffset(2026, 4, 23, 20, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 4, 23, 20, 2, 0, TimeSpan.Zero));

        repository.JoinOpenRound(CreateViewer("joiner"), 1_000m, new DateTimeOffset(2026, 4, 23, 20, 1, 0, TimeSpan.Zero));

        Assert.Equal(4_000m, repository.GetViewerBalance("joiner"));
        Assert.Equal(2, repository.GetParticipants(roundId).Count);
        Assert.Throws<InvalidOperationException>(() =>
            repository.JoinOpenRound(CreateViewer("joiner"), 1_000m, new DateTimeOffset(2026, 4, 23, 20, 1, 30, TimeSpan.Zero)));
    }

    [Fact]
    public void StartRound_RejectsAnInsufficientBalance()
    {
        var repository = CreateRepository();
        repository.SetViewerBalance("starter", 500m);

        Assert.Throws<InvalidOperationException>(() =>
            repository.StartRound(
                CreateViewer("starter"),
                1_000m,
                new DateTimeOffset(2026, 4, 23, 20, 0, 0, TimeSpan.Zero),
                new DateTimeOffset(2026, 4, 23, 20, 2, 0, TimeSpan.Zero)));
    }

    [Fact]
    public void ApplyResolution_RollsBackIfTheResolutionPayloadIsInvalid()
    {
        var repository = CreateRepository();
        repository.SetViewerBalance("starter", 5_000m);
        repository.SetViewerBalance("joiner", 5_000m);
        var roundId = repository.StartRound(
            CreateViewer("starter"),
            1_000m,
            new DateTimeOffset(2026, 4, 23, 20, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 4, 23, 20, 2, 0, TimeSpan.Zero));
        repository.JoinOpenRound(CreateViewer("joiner"), 1_000m, new DateTimeOffset(2026, 4, 23, 20, 1, 0, TimeSpan.Zero));

        var invalidResolution = new HeistResolutionResult
        {
            RoundId = roundId,
            FinalState = HeistRoundState.ResolvedSuccess,
            SuccessChance = 0.75m,
            OriginalPot = 2_000m,
            ResolvedPot = 4_000m,
            ResolvedAtUtc = new DateTimeOffset(2026, 4, 23, 20, 2, 0, TimeSpan.Zero),
            Winners =
            [
                new HeistParticipant
                {
                    Identity = CreateViewer("missingwinner"),
                    StakeAmount = 1_000m,
                    PayoutAmount = 4_000m
                }
            ],
            Losers = Array.Empty<HeistParticipant>()
        };

        Assert.Throws<InvalidOperationException>(() => repository.ApplyResolution(invalidResolution));
        Assert.Equal(roundId, repository.GetOpenRoundId());
        Assert.Equal(4_000m, repository.GetViewerBalance("starter"));
        Assert.Equal(4_000m, repository.GetViewerBalance("joiner"));
    }

    [Fact]
    public void ApplyResolution_RefundsParticipantsWhenTheCrewIsBelowMinimumPlayers()
    {
        var repository = CreateRepository();
        repository.SetViewerBalance("starter", 5_000m);
        repository.SetViewerBalance("joiner", 5_000m);
        var roundId = repository.StartRound(
            CreateViewer("starter"),
            1_000m,
            new DateTimeOffset(2026, 4, 23, 20, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 4, 23, 20, 2, 0, TimeSpan.Zero));
        repository.JoinOpenRound(CreateViewer("joiner"), 1_000m, new DateTimeOffset(2026, 4, 23, 20, 1, 0, TimeSpan.Zero));

        var refundedParticipants = repository.GetParticipants(roundId);
        var resolution = new HeistResolutionResult
        {
            RoundId = roundId,
            FinalState = HeistRoundState.InsufficientCrew,
            SuccessChance = 0.75m,
            OriginalPot = 2_000m,
            ResolvedPot = 2_000m,
            ResolvedAtUtc = new DateTimeOffset(2026, 4, 23, 20, 2, 0, TimeSpan.Zero),
            Winners = Array.Empty<HeistParticipant>(),
            Losers = Array.Empty<HeistParticipant>(),
            RefundedParticipants = refundedParticipants
        };

        repository.ApplyResolution(resolution);

        Assert.Null(repository.GetOpenRoundId());
        Assert.Equal(5_000m, repository.GetViewerBalance("starter"));
        Assert.Equal(5_000m, repository.GetViewerBalance("joiner"));
    }

    public void Dispose()
    {
        if (File.Exists(databasePath))
        {
            File.Delete(databasePath);
        }
    }

    private HeistRepository CreateRepository()
    {
        return new HeistRepository($"Data Source={databasePath};Pooling=False", new SchemaBootstrapper());
    }

    private static ViewerIdentity CreateViewer(string normalizedUsername)
    {
        return new ViewerIdentity
        {
            Username = normalizedUsername,
            NormalizedUsername = normalizedUsername,
            DisplayName = normalizedUsername
        };
    }
}
