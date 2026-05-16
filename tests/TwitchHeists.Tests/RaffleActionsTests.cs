using System.Globalization;
using Microsoft.Data.Sqlite;
using TwitchHeists.Core.Models;
using TwitchHeists.Core.Options;
using TwitchHeists.Core.Services;
using TwitchHeists.Data.Sqlite.Repositories;
using TwitchHeists.Data.Sqlite.Schema;
using TwitchHeists.StreamerBot.Contracts;
using TwitchHeists.StreamerBot.Services;

namespace TwitchHeists.Tests;

public sealed class RaffleActionsTests : IDisposable
{
    private readonly string databasePath;

    public RaffleActionsTests()
    {
        databasePath = Path.Combine(AppContext.BaseDirectory, $"{Guid.NewGuid():N}.raffle-actions.db");
    }

    [Fact]
    public void RunRaffleAction_UsesCommandWinnerPointsWhenProvided()
    {
        SeedBalance("viewerone", 1000m);
        SeedBalance("viewertwo", 1000m);
        SeedActivePresence("viewerone", "ViewerOne");
        SeedActivePresence("viewertwo", "ViewerTwo");
        var action = CreateAction(
            raffleSettings: new RaffleSettings { WinnerPoints = 5000m },
            chooseIndex: _ => 1);
        var repository = new ViewerRepository($"Data Source={databasePath};Pooling=False", new SchemaBootstrapper());

        var result = action.Execute(new RaffleCommandDto
        {
            SourceUsername = "moduser",
            IsBroadcaster = false,
            SingleWinner = true,
            WinnerPoints = 10000m,
            OccurredAtUtc = new DateTimeOffset(2026, 5, 16, 15, 0, 0, TimeSpan.Zero)
        });

        Assert.True(result.Success);
        Assert.Contains("10000 points", result.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(1000m, repository.GetViewerBalance("viewerone"));
        Assert.Equal(11000m, repository.GetViewerBalance("viewertwo"));
    }

    [Fact]
    public void RunRaffleAction_FallsBackToDefaultWinnerPointsWhenCommandAmountMissing()
    {
        SeedBalance("viewerone", 1000m);
        SeedBalance("viewertwo", 1000m);
        SeedActivePresence("viewerone", "ViewerOne");
        SeedActivePresence("viewertwo", "ViewerTwo");
        var action = CreateAction(
            raffleSettings: new RaffleSettings { WinnerPoints = 5000m },
            chooseIndex: _ => 1);
        var repository = new ViewerRepository($"Data Source={databasePath};Pooling=False", new SchemaBootstrapper());

        var result = action.Execute(new RaffleCommandDto
        {
            SourceUsername = "moduser",
            IsBroadcaster = false,
            SingleWinner = true,
            OccurredAtUtc = new DateTimeOffset(2026, 5, 16, 15, 0, 0, TimeSpan.Zero)
        });

        Assert.True(result.Success);
        Assert.Contains("5000 points", result.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(1000m, repository.GetViewerBalance("viewerone"));
        Assert.Equal(6000m, repository.GetViewerBalance("viewertwo"));
    }

    [Fact]
    public void RunRaffleAction_ModModeIncludesAllParticipantsRegardlessOfBalance()
    {
        SeedBalance("viewerone", 1000m);
        SeedBalance("viewertwo", 7000m);
        SeedBalance("viewerthree", 3000m);
        SeedActivePresence("viewerone", "ViewerOne");
        SeedActivePresence("viewertwo", "ViewerTwo");
        SeedActivePresence("viewerthree", "ViewerThree");

        var action = CreateAction(raffleSettings: new RaffleSettings { ModeratorPointsLimit = 5000m }, chooseIndex: _ => 0);

        var result = action.Execute(new RaffleCommandDto
        {
            SourceUsername = "moduser",
            IsBroadcaster = false,
            SingleWinner = false,
            OccurredAtUtc = new DateTimeOffset(2026, 5, 16, 15, 0, 0, TimeSpan.Zero)
        });

        Assert.True(result.Success);
        Assert.Contains("1/3 entrants", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void RunRaffleAction_BroadcasterIgnoresModeratorPointsLimit()
    {
        SeedBalance("viewerone", 1000m);
        SeedBalance("viewertwo", 7000m);
        SeedBalance("viewerthree", 3000m);
        SeedActivePresence("viewerone", "ViewerOne");
        SeedActivePresence("viewertwo", "ViewerTwo");
        SeedActivePresence("viewerthree", "ViewerThree");

        var action = CreateAction(raffleSettings: new RaffleSettings { ModeratorPointsLimit = 5000m }, chooseIndex: _ => 1);

        var result = action.Execute(new RaffleCommandDto
        {
            SourceUsername = "streamer",
            IsBroadcaster = true,
            SingleWinner = false,
            OccurredAtUtc = new DateTimeOffset(2026, 5, 16, 15, 0, 0, TimeSpan.Zero)
        });

        Assert.True(result.Success);
        Assert.Contains("1/3 entrants", result.Message, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("moderator limit", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void RunRaffleAction_SingleWinnerModeAlwaysChoosesOneWinner()
    {
        for (var index = 1; index <= 12; index++)
        {
            var username = $"viewer{index}";
            SeedBalance(username, 1000m);
            SeedActivePresence(username, $"Viewer{index}");
        }

        var action = CreateAction(chooseIndex: _ => 0);

        var result = action.Execute(new RaffleCommandDto
        {
            SourceUsername = "moduser",
            IsBroadcaster = false,
            SingleWinner = true,
            OccurredAtUtc = new DateTimeOffset(2026, 5, 16, 15, 0, 0, TimeSpan.Zero)
        });

        Assert.True(result.Success);
        Assert.Contains("points", result.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("1/12 entrants", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void RunRaffleAction_SingleWinnerAwardsConfiguredPoints()
    {
        SeedBalance("viewerone", 1000m);
        SeedBalance("viewertwo", 1000m);
        SeedActivePresence("viewerone", "ViewerOne");
        SeedActivePresence("viewertwo", "ViewerTwo");
        var action = CreateAction(
            raffleSettings: new RaffleSettings { WinnerPoints = 250m },
            chooseIndex: _ => 1);
        var repository = new ViewerRepository($"Data Source={databasePath};Pooling=False", new SchemaBootstrapper());

        var result = action.Execute(new RaffleCommandDto
        {
            SourceUsername = "moduser",
            IsBroadcaster = false,
            SingleWinner = true,
            OccurredAtUtc = new DateTimeOffset(2026, 5, 16, 15, 0, 0, TimeSpan.Zero)
        });

        Assert.True(result.Success);
        Assert.Contains("250 points", result.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(250m, result.TotalPointsAwarded);
        Assert.Equal(1000m, repository.GetViewerBalance("viewerone"));
        Assert.Equal(1250m, repository.GetViewerBalance("viewertwo"));
    }

    [Fact]
    public void RunRaffleAction_MultiWinnerAwardsConfiguredPointsToEachWinner()
    {
        for (var index = 1; index <= 10; index++)
        {
            var username = $"viewer{index}";
            SeedBalance(username, 1000m);
            SeedActivePresence(username, $"Viewer{index}");
        }

        var picks = new Queue<int>(new[] { 0, 0, 0, 0, 0 });
        var action = CreateAction(
            raffleSettings: new RaffleSettings { WinnerPoints = 150m },
            chooseIndex: _ => picks.Dequeue());
        var repository = new ViewerRepository($"Data Source={databasePath};Pooling=False", new SchemaBootstrapper());

        var result = action.Execute(new RaffleCommandDto
        {
            SourceUsername = "moduser",
            IsBroadcaster = false,
            SingleWinner = false,
            OccurredAtUtc = new DateTimeOffset(2026, 5, 16, 15, 0, 0, TimeSpan.Zero)
        });

        Assert.True(result.Success);
        Assert.Contains("Each winner received 150 points", result.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(5, result.RewardedViewerCount);
        Assert.Equal(750m, result.TotalPointsAwarded);
        var rewardedCount = Enumerable.Range(1, 10)
            .Select(index => repository.GetViewerBalance($"viewer{index}"))
            .Count(balance => balance == 1150m);
        var unchangedCount = Enumerable.Range(1, 10)
            .Select(index => repository.GetViewerBalance($"viewer{index}"))
            .Count(balance => balance == 1000m);
        Assert.Equal(5, rewardedCount);
        Assert.Equal(5, unchangedCount);
    }

    [Fact]
    public void RunRaffleAction_ModModeStillRunsWhenAllParticipantsAreOverOldLimit()
    {
        SeedBalance("viewerone", 5100m);
        SeedBalance("viewertwo", 7500m);
        SeedActivePresence("viewerone", "ViewerOne");
        SeedActivePresence("viewertwo", "ViewerTwo");
        var action = CreateAction(raffleSettings: new RaffleSettings { ModeratorPointsLimit = 5000m });

        var result = action.Execute(new RaffleCommandDto
        {
            SourceUsername = "moduser",
            IsBroadcaster = false,
            SingleWinner = false,
            OccurredAtUtc = new DateTimeOffset(2026, 5, 16, 15, 0, 0, TimeSpan.Zero)
        });

        Assert.True(result.Success);
        Assert.Contains("1/2 entrants", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void StartRaffleAction_ReturnsStartingMessage()
    {
        var action = CreateStartAction();
        var result = action.Execute(new RaffleCommandDto
        {
            SourceUsername = "streamer",
            IsBroadcaster = true,
            SingleWinner = false,
            OccurredAtUtc = new DateTimeOffset(2026, 5, 16, 15, 0, 0, TimeSpan.Zero)
        });

        Assert.True(result.Success);
        Assert.Contains("Drawing in 2 minutes", result.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Use !rjoin to enter for a chance to win 5000 points.", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ResolveDueRafflesAction_EmitsCountdownReminders()
    {
        SeedBalance("viewerone", 1000m);
        var startedAt = new DateTimeOffset(2026, 5, 16, 15, 0, 0, TimeSpan.Zero);
        var start = CreateStartAction();
        var join = CreateJoinAction();
        var resolve = CreateResolveAction(chooseIndex: _ => 0);

        start.Execute(new RaffleCommandDto
        {
            SourceUsername = "streamer",
            IsBroadcaster = true,
            SingleWinner = false,
            OccurredAtUtc = startedAt
        });
        join.Execute(new RaffleCommandDto
        {
            SourceUsername = "viewerone",
            SourceDisplayName = "ViewerOne",
            OccurredAtUtc = startedAt.AddSeconds(10)
        });

        var oneMinute = resolve.Execute(startedAt.AddMinutes(1));
        var thirtySeconds = resolve.Execute(startedAt.AddSeconds(90));
        var tenSeconds = resolve.Execute(startedAt.AddSeconds(110));

        Assert.Contains("1 minute", oneMinute.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("30 seconds", thirtySeconds.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("10 seconds", tenSeconds.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ResolveDueRafflesAction_ResolvesRoundWhenDue()
    {
        SeedBalance("viewerone", 1000m);
        SeedBalance("viewertwo", 2000m);
        var startedAt = new DateTimeOffset(2026, 5, 16, 15, 0, 0, TimeSpan.Zero);
        var start = CreateStartAction();
        var join = CreateJoinAction();
        var resolve = CreateResolveAction(chooseIndex: _ => 0);

        start.Execute(new RaffleCommandDto
        {
            SourceUsername = "streamer",
            IsBroadcaster = true,
            SingleWinner = true,
            OccurredAtUtc = startedAt
        });
        join.Execute(new RaffleCommandDto
        {
            SourceUsername = "viewerone",
            SourceDisplayName = "ViewerOne",
            OccurredAtUtc = startedAt.AddSeconds(5)
        });
        join.Execute(new RaffleCommandDto
        {
            SourceUsername = "viewertwo",
            SourceDisplayName = "ViewerTwo",
            OccurredAtUtc = startedAt.AddSeconds(8)
        });

        var result = resolve.Execute(startedAt.AddMinutes(2));
        var nextTick = resolve.Execute(startedAt.AddMinutes(3));

        Assert.True(result.Success);
        Assert.Contains("won the raffle", result.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("points", result.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("No due raffles", nextTick.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void JoinRaffleAction_AddsViewerToOpenRaffle()
    {
        SeedBalance("viewerone", 500m);
        var startedAt = new DateTimeOffset(2026, 5, 16, 15, 0, 0, TimeSpan.Zero);
        var start = CreateStartAction();
        var join = CreateJoinAction();

        start.Execute(new RaffleCommandDto
        {
            SourceUsername = "streamer",
            IsBroadcaster = true,
            OccurredAtUtc = startedAt
        });

        var result = join.Execute(new RaffleCommandDto
        {
            SourceUsername = "viewerone",
            SourceDisplayName = "ViewerOne",
            OccurredAtUtc = startedAt.AddSeconds(5)
        });

        Assert.True(result.Success);
        Assert.Contains("joined the raffle", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void JoinRaffleAction_AllowsJoiningModRafflesRegardlessOfViewerBalance()
    {
        SeedBalance("viewerone", 9000m);
        var startedAt = new DateTimeOffset(2026, 5, 16, 15, 0, 0, TimeSpan.Zero);
        var start = CreateStartAction();
        var join = CreateJoinAction();

        start.Execute(new RaffleCommandDto
        {
            SourceUsername = "moduser",
            IsBroadcaster = false,
            OccurredAtUtc = startedAt
        });

        var result = join.Execute(new RaffleCommandDto
        {
            SourceUsername = "viewerone",
            SourceDisplayName = "ViewerOne",
            OccurredAtUtc = startedAt.AddSeconds(5)
        });

        Assert.True(result.Success);
        Assert.Contains("joined the raffle", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    public void Dispose()
    {
        if (File.Exists(databasePath))
        {
            File.Delete(databasePath);
        }
    }

    private RunRaffleAction CreateAction(
        RaffleSettings? raffleSettings = null,
        Func<int, int>? chooseIndex = null)
    {
        return new RunRaffleAction(
            new ViewerRepository($"Data Source={databasePath};Pooling=False", new SchemaBootstrapper()),
            new RaffleWinnerCalculator(),
            raffleSettings ?? new RaffleSettings(),
            chooseIndex);
    }

    private StartRaffleAction CreateStartAction(RaffleSettings? raffleSettings = null)
    {
        return new StartRaffleAction(
            new RaffleRepository($"Data Source={databasePath};Pooling=False", new SchemaBootstrapper()),
            raffleSettings ?? new RaffleSettings());
    }

    private ResolveDueRafflesAction CreateResolveAction(
        RaffleSettings? raffleSettings = null,
        Func<int, int>? chooseIndex = null)
    {
        var settings = raffleSettings ?? new RaffleSettings();
        return new ResolveDueRafflesAction(
            new RaffleRepository($"Data Source={databasePath};Pooling=False", new SchemaBootstrapper()),
            CreateAction(settings, chooseIndex),
            settings);
    }

    private JoinRaffleAction CreateJoinAction()
    {
        return new JoinRaffleAction(
            new RaffleRepository($"Data Source={databasePath};Pooling=False", new SchemaBootstrapper()));
    }

    private void SeedBalance(string normalizedUsername, decimal pointsBalance)
    {
        using var connection = new SqliteConnection($"Data Source={databasePath};Pooling=False");
        connection.Open();
        new SchemaBootstrapper().EnsureCreated(connection);

        using var command = connection.CreateCommand();
        command.CommandText =
            """
            INSERT INTO viewer_balances (
                normalized_username,
                twitch_user_id,
                points_balance,
                total_watch_minutes,
                updated_at_utc
            )
            VALUES (
                $normalizedUsername,
                NULL,
                $pointsBalance,
                0,
                $updatedAtUtc
            )
            ON CONFLICT(normalized_username) DO UPDATE SET
                points_balance = excluded.points_balance,
                updated_at_utc = excluded.updated_at_utc;
            """;
        command.Parameters.AddWithValue("$normalizedUsername", normalizedUsername);
        command.Parameters.AddWithValue("$pointsBalance", pointsBalance.ToString(CultureInfo.InvariantCulture));
        command.Parameters.AddWithValue("$updatedAtUtc", DateTimeOffset.UtcNow.UtcDateTime.ToString("O", CultureInfo.InvariantCulture));
        command.ExecuteNonQuery();
    }

    private void SeedActivePresence(string normalizedUsername, string displayName)
    {
        var repository = new ViewerRepository($"Data Source={databasePath};Pooling=False", new SchemaBootstrapper());
        repository.StoreChatPresence(new ViewerPresenceRecord
        {
            Identity = new ViewerIdentity
            {
                Username = normalizedUsername,
                NormalizedUsername = normalizedUsername,
                DisplayName = displayName
            },
            ActiveSinceUtc = DateTimeOffset.UtcNow.AddMinutes(-5),
            LastSeenUtc = DateTimeOffset.UtcNow,
            PresenceSource = PresenceSource.ChatFallback,
            SubscriberTier = TwitchSubscriberTier.None,
            PresenceExpiresAtUtc = DateTimeOffset.UtcNow.AddMinutes(5)
        });
    }
}
