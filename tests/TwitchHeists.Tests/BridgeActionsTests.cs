using System.Globalization;
using Microsoft.Data.Sqlite;
using TwitchHeists.Core.Models;
using TwitchHeists.Data.Sqlite.Schema;
using TwitchHeists.StreamerBot.Bridge.Models;
using TwitchHeists.StreamerBot.Bridge.Services;

namespace TwitchHeists.Tests;

public sealed class BridgeActionsTests : IDisposable
{
    private readonly string installDirectory;

    public BridgeActionsTests()
    {
        installDirectory = Path.Combine(AppContext.BaseDirectory, "bridge-install", Guid.NewGuid().ToString("N"));
    }

    [Fact]
    public void RefreshCommunityViewers_ReturnsFriendlyFailureWhenInstallDirectoryIsMissing()
    {
        var actions = new BridgeActions();

        var result = actions.RefreshCommunityViewers(
            Path.Combine(installDirectory, "missing"),
            new DateTimeOffset(2026, 4, 23, 20, 0, 0, TimeSpan.Zero),
            Array.Empty<BridgeCommunityViewer>());

        Assert.False(result.Success);
        Assert.Contains("install directory", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void CreateRefreshCommunityViewersAction_ReturnsRuntimeForAValidInstallDirectory()
    {
        Directory.CreateDirectory(Path.Combine(installDirectory, "data"));
        File.WriteAllText(Path.Combine(installDirectory, "appsettings.json"), "{}");

        var factory = new BridgeRuntimeFactory();

        var action = factory.CreateRefreshCommunityViewersAction(installDirectory);

        Assert.NotNull(action);
    }

    [Fact]
    public void RefreshCommunityViewers_MapsToASimpleBridgeResult()
    {
        Directory.CreateDirectory(Path.Combine(installDirectory, "data"));
        File.WriteAllText(Path.Combine(installDirectory, "appsettings.json"), "{}");
        var actions = new BridgeActions();

        var result = actions.RefreshCommunityViewers(
            installDirectory,
            new DateTimeOffset(2026, 4, 23, 20, 0, 0, TimeSpan.Zero),
            new[]
            {
                new BridgeCommunityViewer { Username = "viewerone", DisplayName = "ViewerOne" }
            });

        Assert.True(result.Success);
        Assert.Equal("Refresh cycle applied.", result.Message);
        Assert.Equal(1, result.RewardedViewerCount);
        Assert.Equal(10m, result.TotalPointsAwarded);
    }

    [Fact]
    public void StartStreamAndEndStream_ReturnFriendlyLifecycleMessages()
    {
        EnsureInstallDirectory();
        var actions = new BridgeActions();
        var startedAt = new DateTimeOffset(2026, 4, 25, 18, 0, 0, TimeSpan.Zero);

        var startResult = actions.StartStream(
            installDirectory,
            new BridgeStreamLifecycleCommand
            {
                OccurredAtUtc = startedAt
            });
        var endResult = actions.EndStream(
            installDirectory,
            new BridgeStreamLifecycleCommand
            {
                OccurredAtUtc = startedAt.AddHours(4)
            });

        Assert.True(startResult.Success);
        Assert.True(endResult.Success);
        Assert.Contains("marked live", startResult.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("marked offline", endResult.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void RecordChatPresence_DuringAnActiveStreamAwardsSilentWatchStreakPoints()
    {
        EnsureInstallDirectory();
        var actions = new BridgeActions();
        var startedAt = new DateTimeOffset(2026, 4, 25, 18, 0, 0, TimeSpan.Zero);

        var startResult = actions.StartStream(
            installDirectory,
            new BridgeStreamLifecycleCommand
            {
                OccurredAtUtc = startedAt
            });
        var presenceResult = actions.RecordChatPresence(
            installDirectory,
            new BridgeChatPresence
            {
                TwitchUserId = "bridge-streak-1",
                Username = "viewerone",
                DisplayName = "ViewerOne",
                MessageReceivedAtUtc = startedAt.AddMinutes(1)
            },
            startedAt.AddMinutes(5));
        var repeatedPresenceResult = actions.RecordChatPresence(
            installDirectory,
            new BridgeChatPresence
            {
                TwitchUserId = "bridge-streak-1",
                Username = "viewerone",
                DisplayName = "ViewerOne",
                MessageReceivedAtUtc = startedAt.AddMinutes(2)
            },
            startedAt.AddMinutes(5));

        Assert.True(startResult.Success);
        Assert.True(presenceResult.Success);
        Assert.True(repeatedPresenceResult.Success);
        Assert.Equal("viewerone recorded from chat activity.", presenceResult.Message);
        Assert.Equal(100m, GetBalance("viewerone"));
    }

    [Fact]
    public void StartHeist_ReturnsFriendlyFailureMessages()
    {
        Directory.CreateDirectory(Path.Combine(installDirectory, "data"));
        File.WriteAllText(Path.Combine(installDirectory, "appsettings.json"), "{}");
        var actions = new BridgeActions();

        var result = actions.StartHeist(
            installDirectory,
            new BridgeHeistCommand
            {
                Username = "viewerone",
                DisplayName = "ViewerOne",
                StakeAmount = 0,
                OccurredAtUtc = new DateTimeOffset(2026, 4, 23, 20, 0, 0, TimeSpan.Zero)
            });

        Assert.False(result.Success);
        Assert.Contains("greater than zero", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void StartHeist_ReturnsStartingSoonMessage()
    {
        EnsureInstallDirectory();
        SeedBalance("starter", 500m, 0);
        var actions = new BridgeActions();

        var result = actions.StartHeist(
            installDirectory,
            new BridgeHeistCommand
            {
                Username = "starter",
                DisplayName = "Starter",
                StakeAmount = 100m,
                OccurredAtUtc = new DateTimeOffset(2026, 4, 25, 16, 0, 0, TimeSpan.Zero)
            });

        Assert.True(result.Success);
        Assert.Equal("starter started a heist with 100 points. Starting in 2 minutes.", result.Message);
    }

    [Fact]
    public void StartHeist_UsesCustomTemplateFileForStartMessage()
    {
        EnsureInstallDirectory(
            messageTemplatesJson:
            """
            {
              "startMessages": [
                "CUSTOM START {starter} {stake} {joinWindow}"
              ],
              "cooldownMessages": [
                "CUSTOM COOLDOWN {cooldownRemaining}"
              ],
              "reminderMessages": [
                "CUSTOM REMINDER {countdown} {pot} {participantCount}"
              ],
              "successHeadlines": [
                "CUSTOM SUCCESS HEADLINE"
              ],
              "failureHeadlines": [
                "CUSTOM FAILURE HEADLINE"
              ],
              "successCallouts": [
                "CUSTOM WINNER {winner} {payout}"
              ],
              "failureCallouts": [
                "CUSTOM LOSER {loser}"
              ],
              "sacrificeCallouts": [
                "CUSTOM SACRIFICE {loser} {winner}"
              ],
              "resultSummaries": [
                "CUSTOM SUMMARY {winnerCount} {loserCount} {resolvedPot} {successChancePercent}"
              ]
            }
            """);
        SeedBalance("starter", 500m, 0);
        var actions = new BridgeActions();

        var result = actions.StartHeist(
            installDirectory,
            new BridgeHeistCommand
            {
                Username = "starter",
                DisplayName = "Starter",
                StakeAmount = 100m,
                OccurredAtUtc = new DateTimeOffset(2026, 4, 25, 16, 0, 0, TimeSpan.Zero)
            });

        Assert.True(result.Success);
        Assert.Equal("CUSTOM START starter 100 2 minutes", result.Message);
    }

    [Fact]
    public void StartHeist_FailsDuringCooldownWithRemainingTime()
    {
        EnsureInstallDirectory();
        SeedBalance("starter", 1_000m, 0);
        SeedBalance("secondstarter", 1_000m, 0);
        var actions = new BridgeActions();
        var startedAt = new DateTimeOffset(2026, 4, 25, 16, 0, 0, TimeSpan.Zero);

        var startResult = actions.StartHeist(
            installDirectory,
            new BridgeHeistCommand
            {
                Username = "starter",
                DisplayName = "Starter",
                StakeAmount = 100m,
                OccurredAtUtc = startedAt
            });

        Assert.True(startResult.Success);

        var resolveResult = actions.ResolveDueHeists(installDirectory, startedAt.AddMinutes(2));

        Assert.True(resolveResult.Success);

        var cooldownResult = actions.StartHeist(
            installDirectory,
            new BridgeHeistCommand
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

    [Fact]
    public void StartHeist_UsesCustomTemplateFileForCooldownMessage()
    {
        EnsureInstallDirectory(
            configurationJson: BuildHeistConfigurationJson(1.0m, 1.0m),
            messageTemplatesJson:
            """
            {
              "startMessages": [
                "CUSTOM START {starter} {stake} {joinWindow}"
              ],
              "cooldownMessages": [
                "CUSTOM COOLDOWN {cooldownRemaining}"
              ],
              "reminderMessages": [
                "CUSTOM REMINDER {countdown} {pot} {participantCount}"
              ],
              "successHeadlines": [
                "CUSTOM SUCCESS HEADLINE"
              ],
              "failureHeadlines": [
                "CUSTOM FAILURE HEADLINE"
              ],
              "successCallouts": [
                "CUSTOM WINNER {winner} {payout}"
              ],
              "failureCallouts": [
                "CUSTOM LOSER {loser}"
              ],
              "sacrificeCallouts": [
                "CUSTOM SACRIFICE {loser} {winner}"
              ],
              "resultSummaries": [
                "CUSTOM SUMMARY {winnerCount} {loserCount} {resolvedPot} {successChancePercent}"
              ]
            }
            """);
        SeedBalance("starter", 1_000m, 0);
        SeedBalance("secondstarter", 1_000m, 0);
        var actions = new BridgeActions();
        var startedAt = new DateTimeOffset(2026, 4, 25, 16, 0, 0, TimeSpan.Zero);

        var startResult = actions.StartHeist(
            installDirectory,
            new BridgeHeistCommand
            {
                Username = "starter",
                DisplayName = "Starter",
                StakeAmount = 100m,
                OccurredAtUtc = startedAt
            });

        Assert.True(startResult.Success);

        var resolveResult = actions.ResolveDueHeists(installDirectory, startedAt.AddMinutes(2));

        Assert.True(resolveResult.Success);

        var cooldownResult = actions.StartHeist(
            installDirectory,
            new BridgeHeistCommand
            {
                Username = "secondstarter",
                DisplayName = "SecondStarter",
                StakeAmount = 100m,
                OccurredAtUtc = startedAt.AddMinutes(5)
            });

        Assert.False(cooldownResult.Success);
        Assert.Equal("CUSTOM COOLDOWN 2m 0s", cooldownResult.Message);
    }

    [Fact]
    public void ResolveDueHeists_ReturnsCountdownReminderMessage()
    {
        EnsureInstallDirectory();
        SeedBalance("starter", 500m, 0);
        var actions = new BridgeActions();
        var startedAt = new DateTimeOffset(2026, 4, 25, 16, 0, 0, TimeSpan.Zero);

        actions.StartHeist(
            installDirectory,
            new BridgeHeistCommand
            {
                Username = "starter",
                DisplayName = "Starter",
                StakeAmount = 100m,
                OccurredAtUtc = startedAt
            });

        var reminder = actions.ResolveDueHeists(installDirectory, startedAt.AddMinutes(1));

        Assert.True(reminder.Success);
        Assert.Contains("1 minute", reminder.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ResolveDueHeists_UsesCustomTemplateFileForReminderMessage()
    {
        EnsureInstallDirectory(
            messageTemplatesJson:
            """
            {
              "startMessages": [
                "CUSTOM START {starter} {stake} {joinWindow}"
              ],
              "cooldownMessages": [
                "CUSTOM COOLDOWN {cooldownRemaining}"
              ],
              "reminderMessages": [
                "CUSTOM REMINDER {countdown} {pot} {participantCount}"
              ],
              "successHeadlines": [
                "CUSTOM SUCCESS HEADLINE"
              ],
              "failureHeadlines": [
                "CUSTOM FAILURE HEADLINE"
              ],
              "successCallouts": [
                "CUSTOM WINNER {winner} {payout}"
              ],
              "failureCallouts": [
                "CUSTOM LOSER {loser}"
              ],
              "sacrificeCallouts": [
                "CUSTOM SACRIFICE {loser} {winner}"
              ],
              "resultSummaries": [
                "CUSTOM SUMMARY {winnerCount} {loserCount} {resolvedPot} {successChancePercent}"
              ]
            }
            """);
        SeedBalance("starter", 500m, 0);
        var actions = new BridgeActions();
        var startedAt = new DateTimeOffset(2026, 4, 25, 16, 0, 0, TimeSpan.Zero);

        actions.StartHeist(
            installDirectory,
            new BridgeHeistCommand
            {
                Username = "starter",
                DisplayName = "Starter",
                StakeAmount = 100m,
                OccurredAtUtc = startedAt
            });

        var reminder = actions.ResolveDueHeists(installDirectory, startedAt.AddMinutes(1));

        Assert.True(reminder.Success);
        Assert.Equal("CUSTOM REMINDER 1 minute 100 1", reminder.Message);
    }

    [Fact]
    public void ResolveDueHeists_UsesCustomTemplateFileForSuccessResult()
    {
        EnsureInstallDirectory(
            configurationJson: BuildHeistConfigurationJson(1.0m, 1.0m),
            messageTemplatesJson:
            """
            {
              "startMessages": [
                "CUSTOM START {starter} {stake} {joinWindow}"
              ],
              "cooldownMessages": [
                "CUSTOM COOLDOWN {cooldownRemaining}"
              ],
              "reminderMessages": [
                "CUSTOM REMINDER {countdown} {pot} {participantCount}"
              ],
              "successHeadlines": [
                "CUSTOM SUCCESS HEADLINE"
              ],
              "failureHeadlines": [
                "CUSTOM FAILURE HEADLINE"
              ],
              "successCallouts": [
                "CUSTOM WINNER {winner} {payout}"
              ],
              "failureCallouts": [
                "CUSTOM LOSER {loser}"
              ],
              "sacrificeCallouts": [
                "CUSTOM SACRIFICE {loser} {winner}"
              ],
              "resultSummaries": [
                "CUSTOM SUMMARY {winnerCount} {loserCount} {resolvedPot} {successChancePercent}"
              ]
            }
            """);
        SeedBalance("starter", 500m, 0);
        var actions = new BridgeActions();
        var startedAt = new DateTimeOffset(2026, 4, 25, 16, 0, 0, TimeSpan.Zero);

        actions.StartHeist(
            installDirectory,
            new BridgeHeistCommand
            {
                Username = "starter",
                DisplayName = "Starter",
                StakeAmount = 100m,
                OccurredAtUtc = startedAt
            });

        var result = actions.ResolveDueHeists(installDirectory, startedAt.AddMinutes(2));

        Assert.True(result.Success);
        Assert.Equal("CUSTOM SUCCESS HEADLINE CUSTOM WINNER starter 200 CUSTOM SUMMARY 1 0 200 100%", result.Message);
    }

    [Fact]
    public void ResolveDueHeists_UsesCustomTemplateFileForFailureResult()
    {
        EnsureInstallDirectory(
            configurationJson: BuildHeistConfigurationJson(0.0m, 0.0m),
            messageTemplatesJson:
            """
            {
              "startMessages": [
                "CUSTOM START {starter} {stake} {joinWindow}"
              ],
              "cooldownMessages": [
                "CUSTOM COOLDOWN {cooldownRemaining}"
              ],
              "reminderMessages": [
                "CUSTOM REMINDER {countdown} {pot} {participantCount}"
              ],
              "successHeadlines": [
                "CUSTOM SUCCESS HEADLINE"
              ],
              "failureHeadlines": [
                "CUSTOM FAILURE HEADLINE"
              ],
              "successCallouts": [
                "CUSTOM WINNER {winner} {payout}"
              ],
              "failureCallouts": [
                "CUSTOM LOSER {loser}"
              ],
              "sacrificeCallouts": [
                "CUSTOM SACRIFICE {loser} {winner}"
              ],
              "resultSummaries": [
                "CUSTOM SUMMARY {winnerCount} {loserCount} {resolvedPot} {successChancePercent}"
              ]
            }
            """);
        SeedBalance("starter", 500m, 0);
        var actions = new BridgeActions();
        var startedAt = new DateTimeOffset(2026, 4, 25, 16, 0, 0, TimeSpan.Zero);

        actions.StartHeist(
            installDirectory,
            new BridgeHeistCommand
            {
                Username = "starter",
                DisplayName = "Starter",
                StakeAmount = 100m,
                OccurredAtUtc = startedAt
            });

        var result = actions.ResolveDueHeists(installDirectory, startedAt.AddMinutes(2));

        Assert.True(result.Success);
        Assert.Equal("CUSTOM FAILURE HEADLINE CUSTOM LOSER starter CUSTOM SUMMARY 0 1 100 0%", result.Message);
    }

    [Fact]
    public void AddPoints_MapsToASimpleBridgeResult()
    {
        EnsureInstallDirectory();
        var actions = new BridgeActions();

        var result = actions.AddPoints(
            installDirectory,
            new BridgePointsCommand
            {
                TargetUsername = "viewerone",
                TargetDisplayName = "ViewerOne",
                Amount = 250m,
                OccurredAtUtc = new DateTimeOffset(2026, 4, 25, 16, 0, 0, TimeSpan.Zero)
            });

        Assert.True(result.Success);
        Assert.Contains("250", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void AddPoints_UsesTargetTwitchIdWhenUsernameChanged()
    {
        EnsureInstallDirectory();
        SeedBalance("legacyname", "bridge-user-1", 100m, 0);
        var actions = new BridgeActions();

        var result = actions.AddPoints(
            installDirectory,
            new BridgePointsCommand
            {
                TargetTwitchUserId = "bridge-user-1",
                TargetUsername = "renamedviewer",
                TargetDisplayName = "RenamedViewer",
                Amount = 25m,
                OccurredAtUtc = new DateTimeOffset(2026, 4, 25, 16, 0, 0, TimeSpan.Zero)
            });

        Assert.True(result.Success);

        var databasePath = Path.Combine(installDirectory, "data", "twitch-heists.db");
        using var connection = new SqliteConnection($@"Data Source={databasePath};Pooling=False");
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT points_balance
            FROM viewer_balances
            WHERE normalized_username = 'renamedviewer';
            """;

        Assert.Equal("125", Convert.ToString(command.ExecuteScalar(), CultureInfo.InvariantCulture));
    }

    [Fact]
    public void RemovePoints_ClampsBalanceToZero()
    {
        EnsureInstallDirectory();
        SeedBalance("viewerone", 30m, 0);
        var actions = new BridgeActions();

        var result = actions.RemovePoints(
            installDirectory,
            new BridgePointsCommand
            {
                TargetUsername = "viewerone",
                TargetDisplayName = "ViewerOne",
                Amount = 100m,
                OccurredAtUtc = new DateTimeOffset(2026, 4, 25, 16, 0, 0, TimeSpan.Zero)
            });

        Assert.True(result.Success);
        Assert.Contains("0", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void AddPoints_AllReturnsActiveViewerSummary()
    {
        EnsureInstallDirectory();
        SeedActivePresence("viewerone", "ViewerOne");
        SeedActivePresence("viewertwo", "ViewerTwo");
        var actions = new BridgeActions();

        var result = actions.AddPoints(
            installDirectory,
            new BridgePointsCommand
            {
                TargetUsername = "all",
                Amount = 500m,
                OccurredAtUtc = new DateTimeOffset(2026, 4, 25, 16, 0, 0, TimeSpan.Zero)
            });

        Assert.True(result.Success);
        Assert.Contains("2 active viewers", result.Message, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Balance is now", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void RemovePoints_AllReturnsActiveViewerSummary()
    {
        EnsureInstallDirectory();
        SeedBalance("viewerone", 700m, 0);
        SeedBalance("viewertwo", 300m, 0);
        SeedActivePresence("viewerone", "ViewerOne");
        SeedActivePresence("viewertwo", "ViewerTwo");
        var actions = new BridgeActions();

        var result = actions.RemovePoints(
            installDirectory,
            new BridgePointsCommand
            {
                TargetUsername = "all",
                Amount = 500m,
                OccurredAtUtc = new DateTimeOffset(2026, 4, 25, 16, 0, 0, TimeSpan.Zero)
            });

        Assert.True(result.Success);
        Assert.Contains("2 active viewers", result.Message, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Balance is now", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void GivePoints_ReturnsFriendlyFailureWhenSenderLacksFunds()
    {
        EnsureInstallDirectory();
        SeedBalance("giver", 50m, 0);
        var actions = new BridgeActions();

        var result = actions.GivePoints(
            installDirectory,
            new BridgePointsCommand
            {
                SourceUsername = "giver",
                SourceDisplayName = "Giver",
                TargetUsername = "friend",
                TargetDisplayName = "Friend",
                Amount = 100m,
                OccurredAtUtc = new DateTimeOffset(2026, 4, 25, 16, 0, 0, TimeSpan.Zero)
            });

        Assert.False(result.Success);
        Assert.Contains("enough points", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void GetWatchtime_ReturnsASimpleSuccessMessageForStoredLifetimeMinutes()
    {
        EnsureInstallDirectory();
        SeedBalance("viewerone", 0m, 135);
        var actions = new BridgeActions();

        var result = actions.GetWatchtime(
            installDirectory,
            new BridgeWatchtimeQuery
            {
                RequesterUsername = "viewerone",
                RequesterDisplayName = "ViewerOne",
                OccurredAtUtc = new DateTimeOffset(2026, 4, 25, 16, 0, 0, TimeSpan.Zero)
            });

        Assert.True(result.Success);
        Assert.Contains("2h 15m", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void GetWatchtime_UsesTargetTwitchIdWhenUsernameChanged()
    {
        EnsureInstallDirectory();
        SeedBalance("legacyname", "bridge-watchtime-1", 0m, 135);
        var actions = new BridgeActions();

        var result = actions.GetWatchtime(
            installDirectory,
            new BridgeWatchtimeQuery
            {
                RequesterTwitchUserId = "requester-1",
                RequesterUsername = "viewerone",
                RequesterDisplayName = "ViewerOne",
                TargetTwitchUserId = "bridge-watchtime-1",
                TargetUsername = "renamedviewer",
                TargetDisplayName = "RenamedViewer",
                OccurredAtUtc = new DateTimeOffset(2026, 4, 25, 16, 0, 0, TimeSpan.Zero)
            });

        Assert.True(result.Success);
        Assert.Contains("2h 15m", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    public void Dispose()
    {
        if (Directory.Exists(installDirectory))
        {
            SqliteConnection.ClearAllPools();

            for (var attempt = 0; attempt < 3; attempt++)
            {
                try
                {
                    Directory.Delete(installDirectory, true);
                    break;
                }
                catch (IOException) when (attempt < 2)
                {
                    Thread.Sleep(50);
                }
                catch (UnauthorizedAccessException) when (attempt < 2)
                {
                    Thread.Sleep(50);
                }
            }
        }
    }

    private void EnsureInstallDirectory(string? configurationJson = null, string? messageTemplatesJson = null)
    {
        Directory.CreateDirectory(Path.Combine(installDirectory, "data"));
        File.WriteAllText(Path.Combine(installDirectory, "appsettings.json"), configurationJson ?? "{}");

        if (messageTemplatesJson is not null)
        {
            File.WriteAllText(Path.Combine(installDirectory, "heist-messages.json"), messageTemplatesJson);
        }
    }

    private static string BuildHeistConfigurationJson(decimal minimumSuccessChance, decimal maximumSuccessChance)
    {
        return
            $$"""
            {
              "Heist": {
                "JoinWindow": "00:02:00",
                "CooldownWindow": "00:05:00",
                "OneMinuteReminderThreshold": "00:01:00",
                "ThirtySecondReminderThreshold": "00:00:30",
                "TenSecondReminderThreshold": "00:00:10",
                "MinimumSuccessChance": {{minimumSuccessChance.ToString(CultureInfo.InvariantCulture)}},
                "MaximumSuccessChance": {{maximumSuccessChance.ToString(CultureInfo.InvariantCulture)}},
                "MaximumWinnerCount": 5,
                "SuccessfulPotMultiplier": 2.0
              }
            }
            """;
    }

    private void SeedBalance(string normalizedUsername, decimal pointsBalance, int totalWatchMinutes)
    {
        SeedBalance(normalizedUsername, twitchUserId: null, pointsBalance, totalWatchMinutes);
    }

    private void SeedBalance(string normalizedUsername, string? twitchUserId, decimal pointsBalance, int totalWatchMinutes)
    {
        var databasePath = Path.Combine(installDirectory, "data", "twitch-heists.db");
        using var connection = new SqliteConnection($@"Data Source={databasePath};Pooling=False");
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
                $totalWatchMinutes,
                $updatedAtUtc
            )
            ON CONFLICT(normalized_username) DO UPDATE SET
                twitch_user_id = excluded.twitch_user_id,
                points_balance = excluded.points_balance,
                total_watch_minutes = excluded.total_watch_minutes,
                updated_at_utc = excluded.updated_at_utc;
            """;
        command.Parameters.AddWithValue("$normalizedUsername", normalizedUsername);
        command.Parameters.AddWithValue("$twitchUserId", (object?)twitchUserId ?? DBNull.Value);
        command.Parameters.AddWithValue("$pointsBalance", pointsBalance.ToString(CultureInfo.InvariantCulture));
        command.Parameters.AddWithValue("$totalWatchMinutes", totalWatchMinutes);
        command.Parameters.AddWithValue("$updatedAtUtc", DateTimeOffset.UtcNow.UtcDateTime.ToString("O", CultureInfo.InvariantCulture));
        command.ExecuteNonQuery();
    }

    private void SeedActivePresence(string normalizedUsername, string displayName)
    {
        var databasePath = Path.Combine(installDirectory, "data", "twitch-heists.db");
        using var connection = new SqliteConnection($@"Data Source={databasePath};Pooling=False");
        connection.Open();
        new SchemaBootstrapper().EnsureCreated(connection);

        using var command = connection.CreateCommand();
        command.CommandText =
            """
            INSERT INTO viewer_presence (
                normalized_username,
                twitch_user_id,
                username,
                display_name,
                presence_source,
                subscriber_tier,
                is_active,
                active_since_utc,
                last_seen_utc,
                last_confirmed_refresh_utc,
                last_rewarded_cycle_utc,
                presence_expires_at_utc
            )
            VALUES (
                $normalizedUsername,
                NULL,
                $username,
                $displayName,
                $presenceSource,
                $subscriberTier,
                1,
                $activeSinceUtc,
                $lastSeenUtc,
                NULL,
                NULL,
                $presenceExpiresAtUtc
            )
            ON CONFLICT(normalized_username) DO UPDATE SET
                username = excluded.username,
                display_name = excluded.display_name,
                presence_source = excluded.presence_source,
                subscriber_tier = excluded.subscriber_tier,
                is_active = 1,
                active_since_utc = excluded.active_since_utc,
                last_seen_utc = excluded.last_seen_utc,
                presence_expires_at_utc = excluded.presence_expires_at_utc;
            """;
        command.Parameters.AddWithValue("$normalizedUsername", normalizedUsername);
        command.Parameters.AddWithValue("$username", normalizedUsername);
        command.Parameters.AddWithValue("$displayName", displayName);
        command.Parameters.AddWithValue("$presenceSource", (int)PresenceSource.ChatFallback);
        command.Parameters.AddWithValue("$subscriberTier", (int)TwitchSubscriberTier.None);
        command.Parameters.AddWithValue("$activeSinceUtc", DateTimeOffset.UtcNow.AddMinutes(-5).UtcDateTime.ToString("O", CultureInfo.InvariantCulture));
        command.Parameters.AddWithValue("$lastSeenUtc", DateTimeOffset.UtcNow.UtcDateTime.ToString("O", CultureInfo.InvariantCulture));
        command.Parameters.AddWithValue("$presenceExpiresAtUtc", DateTimeOffset.UtcNow.AddMinutes(5).UtcDateTime.ToString("O", CultureInfo.InvariantCulture));
        command.ExecuteNonQuery();
    }

    private decimal GetBalance(string normalizedUsername)
    {
        var databasePath = Path.Combine(installDirectory, "data", "twitch-heists.db");
        using var connection = new SqliteConnection($@"Data Source={databasePath};Pooling=False");
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT points_balance
            FROM viewer_balances
            WHERE normalized_username = $normalizedUsername;
            """;
        command.Parameters.AddWithValue("$normalizedUsername", normalizedUsername);
        var scalar = command.ExecuteScalar();

        return scalar is null || scalar is DBNull
            ? 0m
            : decimal.Parse((string)scalar, CultureInfo.InvariantCulture);
    }
}
