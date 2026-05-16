using System.Globalization;
using Microsoft.Data.Sqlite;
using TwitchHeists.Core.Models;
using TwitchHeists.Core.Options;
using TwitchHeists.Core.Services;
using TwitchHeists.Data.Sqlite.Repositories;
using TwitchHeists.Data.Sqlite.Schema;
using TwitchHeists.StreamerBot.Configuration;
using TwitchHeists.StreamerBot.Contracts;
using TwitchHeists.StreamerBot.Services;

namespace TwitchHeists.Tests;

public sealed class HeistActionsTests : IDisposable
{
    private readonly string databasePath;

    public HeistActionsTests()
    {
        databasePath = Path.Combine(AppContext.BaseDirectory, $"{Guid.NewGuid():N}.heist-actions.db");
    }

    [Fact]
    public void StartHeistAction_ReturnsStartingSoonMessage()
    {
        SeedBalance("starter", 500m);
        var action = CreateStartAction();

        var result = action.Execute(new HeistCommandDto
        {
            Username = "starter",
            DisplayName = "Starter",
            StakeAmount = 100m,
            OccurredAtUtc = new DateTimeOffset(2026, 4, 25, 16, 0, 0, TimeSpan.Zero)
        });

        Assert.True(result.Success);
        Assert.Equal("starter started a heist with 100 points. Starting in 2 minutes. Use !join <points> to join the crew.", result.Message);
    }

    [Fact]
    public void StartHeistAction_UsesTemplateForInsufficientBalanceAndTagsViewerByDefault()
    {
        SeedBalance("starter", 50m);
        var action = CreateStartAction();

        var result = action.Execute(new HeistCommandDto
        {
            Username = "starter",
            DisplayName = "Starter",
            StakeAmount = 100m,
            OccurredAtUtc = new DateTimeOffset(2026, 4, 25, 16, 0, 0, TimeSpan.Zero)
        });

        Assert.False(result.Success);
        Assert.Equal("@starter you need at least 100 points to join this heist.", result.Message);
    }

    [Fact]
    public void StartHeistAction_FailsDuringCooldownWithRemainingTime()
    {
        SeedBalance("starter", 1_000m);
        SeedBalance("secondstarter", 1_000m);
        var repository = CreateRepository();
        var settings = CreateSettings();
        var startAction = CreateStartAction(repository, settings);
        var resolveAction = CreateResolveAction(repository, settings);

        var startedAt = new DateTimeOffset(2026, 4, 25, 16, 0, 0, TimeSpan.Zero);
        var startResult = startAction.Execute(new HeistCommandDto
        {
            Username = "starter",
            DisplayName = "Starter",
            StakeAmount = 100m,
            OccurredAtUtc = startedAt
        });

        Assert.True(startResult.Success);

        var resolution = resolveAction.Execute(startedAt.AddMinutes(2));
        Assert.True(resolution.Success);

        var cooldownResult = startAction.Execute(new HeistCommandDto
        {
            Username = "secondstarter",
            DisplayName = "SecondStarter",
            StakeAmount = 100m,
            OccurredAtUtc = startedAt.AddMinutes(5)
        });

        Assert.False(cooldownResult.Success);
        Assert.Contains("can start in", cooldownResult.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("2m", cooldownResult.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(60, "1 minute")]
    [InlineData(90, "30 seconds")]
    [InlineData(110, "10 seconds")]
    public void ResolveDueHeistsAction_EmitsCountdownReminders(int secondsAfterStart, string expectedCountdown)
    {
        SeedBalance("starter", 500m);
        var repository = CreateRepository();
        var settings = CreateSettings();
        var startAction = CreateStartAction(repository, settings);
        var resolveAction = CreateResolveAction(repository, settings);
        var startedAt = new DateTimeOffset(2026, 4, 25, 16, 0, 0, TimeSpan.Zero);

        var startResult = startAction.Execute(new HeistCommandDto
        {
            Username = "starter",
            DisplayName = "Starter",
            StakeAmount = 100m,
            OccurredAtUtc = startedAt
        });

        Assert.True(startResult.Success);

        var result = resolveAction.Execute(startedAt.AddSeconds(secondsAfterStart));

        Assert.True(result.Success);
        Assert.Contains(expectedCountdown, result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ResolveDueHeistsAction_ReturnsInsufficientCrewByDefaultForASoloHeist()
    {
        SeedBalance("starter", 500m);
        var repository = CreateRepository();
        var settings = CreateSettings();
        var startAction = CreateStartAction(repository, settings);
        var resolveAction = CreateResolveAction(repository, settings);
        var startedAt = new DateTimeOffset(2026, 4, 25, 16, 0, 0, TimeSpan.Zero);

        startAction.Execute(new HeistCommandDto
        {
            Username = "starter",
            DisplayName = "Starter",
            StakeAmount = 100m,
            OccurredAtUtc = startedAt
        });

        var result = resolveAction.Execute(startedAt.AddMinutes(2));

        Assert.True(result.Success);
        Assert.Contains("too small", result.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("100", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void StartHeistAction_UsesProvidedMessageTemplates()
    {
        SeedBalance("starter", 500m);
        var action = CreateStartAction(messageComposer: CreateMessageComposer(new HeistMessageTemplates
        {
            StartMessages = new List<string> { "ACTION START {starter} {stake} {joinWindow}" },
            CooldownMessages = new List<string> { "ACTION COOLDOWN {cooldownRemaining}" },
            ReminderMessages = new List<string> { "ACTION REMINDER {countdown} {pot} {participantCount}" },
            SuccessHeadlines = new List<string> { "ACTION SUCCESS HEADLINE" },
            FailureHeadlines = new List<string> { "ACTION FAILURE HEADLINE" },
            SuccessCallouts = new List<string> { "ACTION WINNER {winner} {payout}" },
            FailureCallouts = new List<string> { "ACTION LOSER {loser}" },
            SacrificeCallouts = new List<string> { "ACTION SACRIFICE {loser} {winner}" },
            ResultSummaries = new List<string> { "ACTION SUMMARY {winnerCount} {loserCount} {resolvedPot} {successChancePercent}" }
        }));

        var result = action.Execute(new HeistCommandDto
        {
            Username = "starter",
            DisplayName = "Starter",
            StakeAmount = 100m,
            OccurredAtUtc = new DateTimeOffset(2026, 4, 25, 16, 0, 0, TimeSpan.Zero)
        });

        Assert.True(result.Success);
        Assert.Equal("ACTION START starter 100 2 minutes", result.Message);
    }

    [Fact]
    public void StartHeistAction_UsesTwitchIdBalanceWhenUsernameChanged()
    {
        SeedBalance("legacyname", "heist-user-1", 500m);
        var action = CreateStartAction();

        var result = action.Execute(new HeistCommandDto
        {
            TwitchUserId = "heist-user-1",
            Username = "renamedviewer",
            DisplayName = "RenamedViewer",
            StakeAmount = 100m,
            OccurredAtUtc = new DateTimeOffset(2026, 4, 25, 16, 0, 0, TimeSpan.Zero)
        });

        Assert.True(result.Success);
        Assert.Equal("renamedviewer started a heist with 100 points. Starting in 2 minutes. Use !join <points> to join the crew.", result.Message);
    }

    [Fact]
    public void JoinHeistAction_ReservesStakeImmediatelyAndTracksJoinedBet()
    {
        SeedBalance("starter", 500m);
        SeedBalance("backup", 500m);
        var repository = CreateRepository();
        var settings = CreateSettings();
        settings.MinimumJoinAmount = 100m;
        var startAction = CreateStartAction(repository, settings);
        var joinAction = CreateJoinAction(repository, settings: settings);
        var startedAt = new DateTimeOffset(2026, 4, 25, 16, 0, 0, TimeSpan.Zero);

        startAction.Execute(new HeistCommandDto
        {
            Username = "starter",
            DisplayName = "Starter",
            StakeAmount = 100m,
            OccurredAtUtc = startedAt
        });

        var joinResult = joinAction.Execute(new HeistCommandDto
        {
            Username = "backup",
            DisplayName = "Backup",
            StakeAmount = 100m,
            OccurredAtUtc = startedAt.AddSeconds(10)
        });

        Assert.True(joinResult.Success);
        Assert.Equal(400m, repository.GetViewerBalance("backup"));

        var round = repository.GetOpenRound();
        Assert.NotNull(round);
        Assert.Equal(200m, round!.OriginalPot);

        var joinedParticipant = repository.GetParticipants(round.RoundId).Single(participant => participant.Identity.NormalizedUsername == "backup");
        Assert.Equal(100m, joinedParticipant.StakeAmount);
    }

    [Fact]
    public void ResolveDueHeistsAction_UsesProvidedMessageTemplates()
    {
        SeedBalance("starter", 500m);
        var repository = CreateRepository();
        var settings = CreateSettings();
        settings.MinimumPlayers = 1;
        var composer = CreateMessageComposer(new HeistMessageTemplates
        {
            StartMessages = new List<string> { "ACTION START {starter} {stake} {joinWindow}" },
            CooldownMessages = new List<string> { "ACTION COOLDOWN {cooldownRemaining}" },
            ReminderMessages = new List<string> { "ACTION REMINDER {countdown} {pot} {participantCount}" },
            SuccessHeadlines = new List<string> { "ACTION SUCCESS HEADLINE" },
            FailureHeadlines = new List<string> { "ACTION FAILURE HEADLINE" },
            SuccessCallouts = new List<string> { "ACTION WINNER {winner} {payout}" },
            FailureCallouts = new List<string> { "ACTION LOSER {loser}" },
            SacrificeCallouts = new List<string> { "ACTION SACRIFICE {loser} {winner}" },
            ResultSummaries = new List<string> { "ACTION SUMMARY {winnerCount} {loserCount} {resolvedPot} {successChancePercent}" }
        }, settings);
        var startAction = CreateStartAction(repository, settings, composer);
        var resolveAction = CreateResolveAction(repository, settings, composer);
        var startedAt = new DateTimeOffset(2026, 4, 25, 16, 0, 0, TimeSpan.Zero);

        startAction.Execute(new HeistCommandDto
        {
            Username = "starter",
            DisplayName = "Starter",
            StakeAmount = 100m,
            OccurredAtUtc = startedAt
        });

        var reminder = resolveAction.Execute(startedAt.AddMinutes(1));
        var result = resolveAction.Execute(startedAt.AddMinutes(2));

        Assert.Equal("ACTION REMINDER 1 minute 100 1", reminder.Message);
        Assert.Equal("ACTION SUCCESS HEADLINE ACTION WINNER starter 200 ACTION SUMMARY 1 0 200 74.59%", result.Message);
    }

    [Fact]
    public void ResolveDueHeistsAction_UsesProvidedTemplateForInsufficientCrewResults()
    {
        SeedBalance("starter", 500m);
        var repository = CreateRepository();
        var settings = CreateSettings();
        var composer = CreateMessageComposer(new HeistMessageTemplates
        {
            StartMessages = new List<string> { "ACTION START {starter} {stake} {joinWindow}" },
            CooldownMessages = new List<string> { "ACTION COOLDOWN {cooldownRemaining}" },
            ReminderMessages = new List<string> { "ACTION REMINDER {countdown} {pot} {participantCount}" },
            SuccessHeadlines = new List<string> { "ACTION SUCCESS HEADLINE" },
            FailureHeadlines = new List<string> { "ACTION FAILURE HEADLINE" },
            SuccessCallouts = new List<string> { "ACTION WINNER {winner} {payout}" },
            FailureCallouts = new List<string> { "ACTION LOSER {loser}" },
            SacrificeCallouts = new List<string> { "ACTION SACRIFICE {loser} {winner}" },
            ResultSummaries = new List<string> { "ACTION SUMMARY {winnerCount} {loserCount} {resolvedPot} {successChancePercent}" },
            InsufficientCrewMessages = new List<string> { "ACTION INSUFFICIENT {participantCount} {resolvedPot}" }
        }, settings);
        var startAction = CreateStartAction(repository, settings, composer);
        var resolveAction = CreateResolveAction(repository, settings, composer);
        var startedAt = new DateTimeOffset(2026, 4, 25, 16, 0, 0, TimeSpan.Zero);

        startAction.Execute(new HeistCommandDto
        {
            Username = "starter",
            DisplayName = "Starter",
            StakeAmount = 100m,
            OccurredAtUtc = startedAt
        });

        var result = resolveAction.Execute(startedAt.AddMinutes(2));

        Assert.True(result.Success);
        Assert.Equal("ACTION INSUFFICIENT 1 100", result.Message);
    }

    [Fact]
    public void JoinHeistAction_UsesTemplateForInsufficientBalanceWhenJoining()
    {
        SeedBalance("starter", 500m);
        SeedBalance("backup", 50m);
        var repository = CreateRepository();
        var settings = CreateSettings();
        settings.MinimumJoinAmount = 100m;
        var startAction = CreateStartAction(repository, settings);
        var joinAction = CreateJoinAction(repository, settings: settings);
        var startedAt = new DateTimeOffset(2026, 4, 25, 16, 0, 0, TimeSpan.Zero);

        startAction.Execute(new HeistCommandDto
        {
            Username = "starter",
            DisplayName = "Starter",
            StakeAmount = 100m,
            OccurredAtUtc = startedAt
        });

        var result = joinAction.Execute(new HeistCommandDto
        {
            Username = "backup",
            DisplayName = "Backup",
            StakeAmount = 100m,
            OccurredAtUtc = startedAt.AddSeconds(10)
        });

        Assert.False(result.Success);
        Assert.Equal("@backup you need at least 100 points to join this heist.", result.Message);
    }

    [Fact]
    public void JoinHeistAction_UsesTemplateForAlreadyJoinedWhenJoiningTwice()
    {
        SeedBalance("starter", 500m);
        SeedBalance("backup", 500m);
        var repository = CreateRepository();
        var settings = CreateSettings();
        settings.MinimumJoinAmount = 100m;
        var startAction = CreateStartAction(repository, settings);
        var joinAction = CreateJoinAction(repository, settings: settings);
        var startedAt = new DateTimeOffset(2026, 4, 25, 16, 0, 0, TimeSpan.Zero);

        startAction.Execute(new HeistCommandDto
        {
            Username = "starter",
            DisplayName = "Starter",
            StakeAmount = 100m,
            OccurredAtUtc = startedAt
        });

        var firstJoin = joinAction.Execute(new HeistCommandDto
        {
            Username = "backup",
            DisplayName = "Backup",
            StakeAmount = 100m,
            OccurredAtUtc = startedAt.AddSeconds(10)
        });

        var secondJoin = joinAction.Execute(new HeistCommandDto
        {
            Username = "backup",
            DisplayName = "Backup",
            StakeAmount = 100m,
            OccurredAtUtc = startedAt.AddSeconds(20)
        });

        Assert.True(firstJoin.Success);
        Assert.False(secondJoin.Success);
        Assert.Equal("@backup Viewer has already joined the open heist.", secondJoin.Message);
    }

    [Fact]
    public void JoinHeistAction_UsesTemplateForMinimumJoinAmountWhenStakeIsTooLow()
    {
        SeedBalance("starter", 500m);
        SeedBalance("backup", 5_000m);
        var repository = CreateRepository();
        var composer = CreateMessageComposer(new HeistMessageTemplates
        {
            StartMessages = new List<string> { "ACTION START {starter} {stake} {joinWindow}" },
            CooldownMessages = new List<string> { "ACTION COOLDOWN {cooldownRemaining}" },
            ReminderMessages = new List<string> { "ACTION REMINDER {countdown} {pot} {participantCount}" },
            SuccessHeadlines = new List<string> { "ACTION SUCCESS HEADLINE" },
            FailureHeadlines = new List<string> { "ACTION FAILURE HEADLINE" },
            SuccessCallouts = new List<string> { "ACTION WINNER {winner} {payout}" },
            FailureCallouts = new List<string> { "ACTION LOSER {loser}" },
            SacrificeCallouts = new List<string> { "ACTION SACRIFICE {loser} {winner}" },
            ResultSummaries = new List<string> { "ACTION SUMMARY {winnerCount} {loserCount} {resolvedPot} {successChancePercent}" },
            MinimumJoinAmountMessages = new List<string> { "ACTION MINIMUM {viewer} {minimumJoinAmount}" }
        });
        var startAction = CreateStartAction(repository, messageComposer: composer);
        var joinAction = CreateJoinAction(repository, messageComposer: composer);
        var startedAt = new DateTimeOffset(2026, 4, 25, 16, 0, 0, TimeSpan.Zero);

        startAction.Execute(new HeistCommandDto
        {
            Username = "starter",
            DisplayName = "Starter",
            StakeAmount = 100m,
            OccurredAtUtc = startedAt
        });

        var result = joinAction.Execute(new HeistCommandDto
        {
            Username = "backup",
            DisplayName = "Backup",
            StakeAmount = 100m,
            OccurredAtUtc = startedAt.AddSeconds(10)
        });

        Assert.False(result.Success);
        Assert.Equal("ACTION MINIMUM backup 1000", result.Message);
    }

    [Fact]
    public void ResolveDueHeistsAction_KeepsLargeSuccessfulMessagesToOneNamedCallout()
    {
        SeedBalance("starter", 500m);
        SeedBalance("viewer2", 500m);
        SeedBalance("viewer3", 500m);
        SeedBalance("viewer4", 500m);
        SeedBalance("viewer5", 500m);
        SeedBalance("viewer6", 500m);
        var repository = CreateRepository();
        var settings = CreateSettings();
        settings.MinimumPlayers = 2;
        settings.MinimumJoinAmount = 100m;
        settings.MaximumNamedResolutionCallouts = 1;

        var composer = CreateMessageComposer(new HeistMessageTemplates
        {
            StartMessages = new List<string> { "ACTION START {starter} {stake} {joinWindow}" },
            CooldownMessages = new List<string> { "ACTION COOLDOWN {cooldownRemaining}" },
            ReminderMessages = new List<string> { "ACTION REMINDER {countdown} {pot} {participantCount}" },
            SuccessHeadlines = new List<string> { "ACTION SUCCESS HEADLINE" },
            FailureHeadlines = new List<string> { "ACTION FAILURE HEADLINE" },
            SuccessCallouts = new List<string> { "ACTION CALLOUT {winner} {payout}" },
            FailureCallouts = new List<string> { "ACTION LOSER {loser}" },
            SacrificeCallouts = new List<string> { "ACTION CALLOUT {loser} {winner}" },
            ResultSummaries = new List<string> { "ACTION SUMMARY {winnerCount} {loserCount} {resolvedPot} {successChancePercent}" }
        }, settings);
        var startAction = CreateStartAction(repository, settings, composer);
        var joinAction = CreateJoinAction(repository, settings: settings);
        var resolveAction = CreateResolveAction(repository, settings, composer);
        var startedAt = new DateTimeOffset(2026, 4, 25, 16, 0, 0, TimeSpan.Zero);

        startAction.Execute(new HeistCommandDto
        {
            Username = "starter",
            DisplayName = "Starter",
            StakeAmount = 100m,
            OccurredAtUtc = startedAt
        });

        foreach (var viewer in new[] { "viewer2", "viewer3", "viewer4", "viewer5", "viewer6" })
        {
            var joinResult = joinAction.Execute(new HeistCommandDto
            {
                Username = viewer,
                DisplayName = viewer,
                StakeAmount = 100m,
                OccurredAtUtc = startedAt.AddSeconds(15)
            });

            Assert.True(joinResult.Success);
        }

        var result = resolveAction.Execute(startedAt.AddMinutes(2));

        Assert.True(result.Success);
        Assert.Equal(1, CountOccurrences(result.Message, "ACTION CALLOUT"));
        Assert.Contains("ACTION SUMMARY", result.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void HeistMessageComposer_DoesNotRepeatTheSameFailureCalloutTemplate()
    {
        var settings = CreateSettings();
        settings.MaximumNamedResolutionCallouts = 2;
        var composer = CreateMessageComposer(new HeistMessageTemplates
        {
            StartMessages = new List<string> { "ACTION START {starter} {stake} {joinWindow}" },
            CooldownMessages = new List<string> { "ACTION COOLDOWN {cooldownRemaining}" },
            ReminderMessages = new List<string> { "ACTION REMINDER {countdown} {pot} {participantCount}" },
            SuccessHeadlines = new List<string> { "ACTION SUCCESS HEADLINE" },
            FailureHeadlines = new List<string> { "ACTION FAILURE HEADLINE" },
            SuccessCallouts = new List<string> { "ACTION WINNER {winner} {payout}" },
            FailureCallouts = new List<string> { "ACTION LOSER {loser}" },
            SacrificeCallouts = new List<string> { "ACTION SACRIFICE {loser} {winner}" },
            ResultSummaries = new List<string> { "ACTION SUMMARY {winnerCount} {loserCount} {resolvedPot} {successChancePercent}" }
        }, settings);

        var result = composer.ComposeResolution(new HeistResolutionResult
        {
            RoundId = Guid.NewGuid(),
            FinalState = HeistRoundState.ResolvedFailure,
            SuccessChance = 0.5219m,
            OriginalPot = 47000m,
            ResolvedPot = 47000m,
            Winners = Array.Empty<HeistParticipant>(),
            Losers = new[]
            {
                new HeistParticipant
                {
                    Identity = new ViewerIdentity { Username = "firstloser", NormalizedUsername = "firstloser", DisplayName = "FirstLoser" },
                    StakeAmount = 100m
                },
                new HeistParticipant
                {
                    Identity = new ViewerIdentity { Username = "secondloser", NormalizedUsername = "secondloser", DisplayName = "SecondLoser" },
                    StakeAmount = 100m
                }
            },
            RefundedParticipants = Array.Empty<HeistParticipant>(),
            ResolvedAtUtc = new DateTimeOffset(2026, 4, 25, 16, 2, 0, TimeSpan.Zero)
        });

        Assert.Equal(1, CountOccurrences(result, "ACTION LOSER"));
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

    private StartHeistAction CreateStartAction(
        HeistRepository? repository = null,
        HeistSettings? settings = null,
        HeistMessageComposer? messageComposer = null)
    {
        return new StartHeistAction(
            repository ?? CreateRepository(),
            settings ?? CreateSettings(),
            messageComposer ?? CreateMessageComposer(settings: settings ?? CreateSettings()));
    }

    private ResolveDueHeistsAction CreateResolveAction(
        HeistRepository? repository = null,
        HeistSettings? settings = null,
        HeistMessageComposer? messageComposer = null)
    {
        var resolvedSettings = settings ?? CreateSettings();

        return new ResolveDueHeistsAction(
            repository ?? CreateRepository(),
            new HeistChanceCalculator(resolvedSettings),
            new HeistResolver(resolvedSettings, () => 0.01),
            resolvedSettings,
            messageComposer ?? CreateMessageComposer(settings: resolvedSettings));
    }

    private JoinHeistAction CreateJoinAction(
        HeistRepository? repository = null,
        HeistMessageComposer? messageComposer = null,
        HeistSettings? settings = null)
    {
        var resolvedSettings = settings ?? CreateSettings();
        return new JoinHeistAction(
            repository ?? CreateRepository(),
            resolvedSettings,
            messageComposer ?? CreateMessageComposer(settings: resolvedSettings));
    }

    private static HeistSettings CreateSettings()
    {
        return new HeistSettings
        {
            JoinWindow = TimeSpan.FromMinutes(2),
            MinimumJoinAmount = 1000m,
            MaximumWinnerCount = 5,
            SuccessfulPotMultiplier = 2.0m
        };
    }

    private static HeistMessageComposer CreateMessageComposer(HeistMessageTemplates? templates = null, HeistSettings? settings = null)
    {
        return new HeistMessageComposer(templates ?? HeistMessageTemplates.CreateDefault(), settings ?? CreateSettings(), _ => 0);
    }

    private static int CountOccurrences(string value, string token)
    {
        var count = 0;
        var startIndex = 0;

        while (true)
        {
            var index = value.IndexOf(token, startIndex, StringComparison.Ordinal);
            if (index < 0)
            {
                return count;
            }

            count++;
            startIndex = index + token.Length;
        }
    }

    private void SeedBalance(string normalizedUsername, decimal pointsBalance)
    {
        SeedBalance(normalizedUsername, twitchUserId: null, pointsBalance);
    }

    private void SeedBalance(string normalizedUsername, string? twitchUserId, decimal pointsBalance)
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
                $twitchUserId,
                $pointsBalance,
                0,
                $updatedAtUtc
            )
            ON CONFLICT(normalized_username) DO UPDATE SET
                twitch_user_id = excluded.twitch_user_id,
                points_balance = excluded.points_balance,
                updated_at_utc = excluded.updated_at_utc;
            """;
        command.Parameters.AddWithValue("$normalizedUsername", normalizedUsername);
        command.Parameters.AddWithValue("$twitchUserId", (object?)twitchUserId ?? DBNull.Value);
        command.Parameters.AddWithValue("$pointsBalance", pointsBalance.ToString(CultureInfo.InvariantCulture));
        command.Parameters.AddWithValue("$updatedAtUtc", DateTimeOffset.UtcNow.UtcDateTime.ToString("O", CultureInfo.InvariantCulture));
        command.ExecuteNonQuery();
    }
}
