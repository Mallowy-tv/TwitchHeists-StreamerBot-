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
        Assert.Equal(500m, result.TotalPointsAwarded);
    }

    [Fact]
    public void RefreshCommunityViewers_LoadsRewardsSettingsFromConfiguration()
    {
        EnsureInstallDirectory(
            configurationJson: BuildRewardsConfigurationJson(
                rewardInterval: "00:07:00",
                basePointsPerInterval: 1000m,
                tier1Multiplier: 1.25m,
                tier2Multiplier: 2.5m,
                tier3Multiplier: 4.0m));
        var actions = new BridgeActions();
        var refreshedAt = new DateTimeOffset(2026, 4, 23, 20, 0, 0, TimeSpan.Zero);

        var refreshResult = actions.RefreshCommunityViewers(
            installDirectory,
            refreshedAt,
            new[]
            {
                new BridgeCommunityViewer
                {
                    TwitchUserId = "tier-three-1",
                    Username = "viewerone",
                    DisplayName = "ViewerOne",
                    SubscriberTier = 3
                }
            });
        var watchtimeResult = actions.GetWatchtime(
            installDirectory,
            new BridgeWatchtimeQuery
            {
                RequesterTwitchUserId = "tier-three-1",
                RequesterUsername = "viewerone",
                RequesterDisplayName = "ViewerOne",
                OccurredAtUtc = refreshedAt
            });

        Assert.True(refreshResult.Success);
        Assert.Equal(4000m, refreshResult.TotalPointsAwarded);
        Assert.True(watchtimeResult.Success);
        Assert.Contains("7m", watchtimeResult.Message, StringComparison.OrdinalIgnoreCase);
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
        Assert.Equal("starter started a heist with 100 points. Starting in 2 minutes. Use !join <points> to join the crew.", result.Message);
    }

    [Fact]
    public void StartHeist_ReturnsTaggedInsufficientBalanceMessage()
    {
        EnsureInstallDirectory();
        SeedBalance("starter", 50m, 0);
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

        Assert.False(result.Success);
        Assert.Equal("@starter you need at least 100 points to join this heist.", result.Message);
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
            configurationJson: BuildHeistConfigurationJson(1.0m, 1.0m, minimumParticipants: 1),
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
            configurationJson: BuildHeistConfigurationJson(1.0m, 1.0m, minimumParticipants: 1),
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
            configurationJson: BuildHeistConfigurationJson(0.0m, 0.0m, minimumParticipants: 1),
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
    public void ResolveDueHeists_UsesCustomTemplateFileForInsufficientCrewResultsAndLoadsMinimumParticipants()
    {
        EnsureInstallDirectory(
            configurationJson: BuildHeistConfigurationJson(1.0m, 1.0m, minimumParticipants: 3, minimumJoinAmount: 100m),
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
              ],
              "insufficientCrewMessages": [
                "CUSTOM INSUFFICIENT {participantCount} {resolvedPot}"
              ]
            }
            """);
        SeedBalance("starter", 500m, 0);
        SeedBalance("backup", 500m, 0);
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
        var joinResult = actions.JoinHeist(
            installDirectory,
            new BridgeHeistCommand
            {
                Username = "backup",
                DisplayName = "Backup",
                StakeAmount = 100m,
                OccurredAtUtc = startedAt.AddSeconds(10)
            });

        Assert.True(joinResult.Success);

        var result = actions.ResolveDueHeists(installDirectory, startedAt.AddMinutes(2));

        Assert.True(result.Success);
        Assert.Equal("CUSTOM INSUFFICIENT 2 200", result.Message);
    }

    [Fact]
    public void ResolveDueHeists_UsesConfiguredMaximumNamedCalloutsForLargeCrews()
    {
        EnsureInstallDirectory(
            configurationJson: BuildHeistConfigurationJson(1.0m, 1.0m, minimumParticipants: 2, maximumNamedResolutionCallouts: 1),
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
                "CUSTOM CALLOUT {winner} {payout}"
              ],
              "failureCallouts": [
                "CUSTOM LOSER {loser}"
              ],
              "sacrificeCallouts": [
                "CUSTOM CALLOUT {loser} {winner}"
              ],
              "resultSummaries": [
                "CUSTOM SUMMARY {winnerCount} {loserCount} {resolvedPot} {successChancePercent}"
              ]
            }
            """);
        SeedBalance("starter", 500m, 0);
        SeedBalance("viewer2", 500m, 0);
        SeedBalance("viewer3", 500m, 0);
        SeedBalance("viewer4", 500m, 0);
        SeedBalance("viewer5", 500m, 0);
        SeedBalance("viewer6", 500m, 0);
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

        foreach (var viewer in new[] { "viewer2", "viewer3", "viewer4", "viewer5", "viewer6" })
        {
            var joinResult = actions.JoinHeist(
                installDirectory,
                new BridgeHeistCommand
                {
                    Username = viewer,
                    DisplayName = viewer,
                    StakeAmount = 100m,
                    OccurredAtUtc = startedAt.AddSeconds(15)
                });

            Assert.True(joinResult.Success);
        }

        var result = actions.ResolveDueHeists(installDirectory, startedAt.AddMinutes(2));

        Assert.True(result.Success);
        Assert.Equal(1, CountOccurrences(result.Message, "CUSTOM CALLOUT"));
        Assert.Contains("CUSTOM SUMMARY", result.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void RunRaffle_ModModeUsesConfiguredPointsLimit()
    {
        EnsureInstallDirectory();
        SeedBalance("viewerone", 1000m, 0);
        SeedBalance("viewertwo", 6500m, 0);
        SeedBalance("viewerthree", 3000m, 0);
        SeedActivePresence("viewerone", "ViewerOne");
        SeedActivePresence("viewertwo", "ViewerTwo");
        SeedActivePresence("viewerthree", "ViewerThree");
        var actions = new BridgeActions();

        var result = actions.RunRaffle(
            installDirectory,
            new BridgeRaffleCommand
            {
                SourceUsername = "moduser",
                IsBroadcaster = false,
                OccurredAtUtc = new DateTimeOffset(2026, 5, 16, 16, 0, 0, TimeSpan.Zero)
            });

        Assert.True(result.Success);
        Assert.Contains("drawing in", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void RaffleActions_LoadRaffleSettingsFromConfiguration()
    {
        EnsureInstallDirectory(
            configurationJson: BuildRaffleConfigurationJson(
                joinWindow: "00:03:00",
                oneMinuteReminderThreshold: "00:02:30",
                thirtySecondReminderThreshold: "00:01:45",
                tenSecondReminderThreshold: "00:01:15",
                winnerPoints: 4321m,
                moderatorPointsLimit: 5000m));
        SeedBalance("viewerone", 1000m, 0);
        SeedActivePresence("viewerone", "ViewerOne");
        var actions = new BridgeActions();
        var startedAt = new DateTimeOffset(2026, 5, 16, 16, 0, 0, TimeSpan.Zero);

        var startResult = actions.RunRaffle(
            installDirectory,
            new BridgeRaffleCommand
            {
                SourceUsername = "moduser",
                IsBroadcaster = false,
                OccurredAtUtc = startedAt
            });
        var joinResult = actions.JoinRaffle(
            installDirectory,
            new BridgeRaffleCommand
            {
                SourceUsername = "viewerone",
                SourceDisplayName = "ViewerOne",
                OccurredAtUtc = startedAt.AddSeconds(5)
            });
        var oneMinuteReminder = actions.ResolveDueRaffles(installDirectory, startedAt.AddSeconds(40));
        var thirtySecondReminder = actions.ResolveDueRaffles(installDirectory, startedAt.AddSeconds(80));
        var tenSecondReminder = actions.ResolveDueRaffles(installDirectory, startedAt.AddSeconds(110));
        var resolution = actions.ResolveDueRaffles(installDirectory, startedAt.AddMinutes(3));

        Assert.True(startResult.Success);
        Assert.True(joinResult.Success);
        Assert.Contains("4321 points", startResult.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("3 minutes", startResult.Message, StringComparison.OrdinalIgnoreCase);
        Assert.True(oneMinuteReminder.Success);
        Assert.Contains("1 minute", oneMinuteReminder.Message, StringComparison.OrdinalIgnoreCase);
        Assert.True(thirtySecondReminder.Success);
        Assert.Contains("30 seconds", thirtySecondReminder.Message, StringComparison.OrdinalIgnoreCase);
        Assert.True(tenSecondReminder.Success);
        Assert.Contains("10 seconds", tenSecondReminder.Message, StringComparison.OrdinalIgnoreCase);
        Assert.True(resolution.Success);
        Assert.Contains("4321 points", resolution.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void RunSingleWinnerRaffle_StartsScheduledRound()
    {
        EnsureInstallDirectory();
        SeedBalance("viewerone", 1000m, 0);
        SeedBalance("viewertwo", 2500m, 0);
        SeedBalance("viewerthree", 3000m, 0);
        SeedBalance("viewerfour", 500m, 0);
        SeedActivePresence("viewerone", "ViewerOne");
        SeedActivePresence("viewertwo", "ViewerTwo");
        SeedActivePresence("viewerthree", "ViewerThree");
        SeedActivePresence("viewerfour", "ViewerFour");
        var actions = new BridgeActions();

        var result = actions.RunSingleWinnerRaffle(
            installDirectory,
            new BridgeRaffleCommand
            {
                SourceUsername = "streamer",
                IsBroadcaster = true,
                OccurredAtUtc = new DateTimeOffset(2026, 5, 16, 16, 0, 0, TimeSpan.Zero)
            });

        Assert.True(result.Success);
        Assert.Contains("single-winner raffle", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ResolveDueRaffles_ReturnsCountdownAndThenResultMessage()
    {
        EnsureInstallDirectory();
        SeedBalance("viewerone", 1000m, 0);
        SeedBalance("viewertwo", 2500m, 0);
        var actions = new BridgeActions();
        var startedAt = new DateTimeOffset(2026, 5, 16, 16, 0, 0, TimeSpan.Zero);

        var start = actions.RunSingleWinnerRaffle(
            installDirectory,
            new BridgeRaffleCommand
            {
                SourceUsername = "streamer",
                IsBroadcaster = true,
                WinnerPoints = 10000m,
                OccurredAtUtc = startedAt
            });
        var joinOne = actions.JoinRaffle(
            installDirectory,
            new BridgeRaffleCommand
            {
                SourceUsername = "viewerone",
                SourceDisplayName = "ViewerOne",
                OccurredAtUtc = startedAt.AddSeconds(5)
            });
        var joinTwo = actions.JoinRaffle(
            installDirectory,
            new BridgeRaffleCommand
            {
                SourceUsername = "viewertwo",
                SourceDisplayName = "ViewerTwo",
                OccurredAtUtc = startedAt.AddSeconds(8)
            });
        var reminder = actions.ResolveDueRaffles(installDirectory, startedAt.AddMinutes(1));
        var resolution = actions.ResolveDueRaffles(installDirectory, startedAt.AddMinutes(2));

        Assert.True(start.Success);
        Assert.True(joinOne.Success);
        Assert.True(joinTwo.Success);
        Assert.True(reminder.Success);
        Assert.True(resolution.Success);
        Assert.Contains("drawing in 1 minute", reminder.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("raffle", resolution.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("10000 points", resolution.Message, StringComparison.OrdinalIgnoreCase);
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

    [Fact]
    public void GetPoints_ReturnsRequesterBalanceWhenNoTargetIsSupplied()
    {
        EnsureInstallDirectory();
        SeedBalance("viewerone", 425m, 0);
        var actions = new BridgeActions();

        var result = actions.GetPoints(
            installDirectory,
            new BridgePointsQuery
            {
                RequesterUsername = "viewerone",
                RequesterDisplayName = "ViewerOne",
                OccurredAtUtc = new DateTimeOffset(2026, 5, 16, 16, 0, 0, TimeSpan.Zero)
            });

        Assert.True(result.Success);
        Assert.Contains("425", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void GetPoints_ReturnsTargetBalanceWhenTargetIsProvided()
    {
        EnsureInstallDirectory();
        SeedBalance("targetviewer", 775m, 0);
        var actions = new BridgeActions();

        var result = actions.GetPoints(
            installDirectory,
            new BridgePointsQuery
            {
                RequesterUsername = "viewerone",
                RequesterDisplayName = "ViewerOne",
                TargetUsername = "targetviewer",
                TargetDisplayName = "TargetViewer",
                OccurredAtUtc = new DateTimeOffset(2026, 5, 16, 16, 0, 0, TimeSpan.Zero)
            });

        Assert.True(result.Success);
        Assert.Contains("775", result.Message, StringComparison.OrdinalIgnoreCase);
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

    private static string BuildHeistConfigurationJson(
        decimal minimumSuccessChance,
        decimal maximumSuccessChance,
        int minimumParticipants = 2,
        int maximumNamedResolutionCallouts = 2,
        decimal minimumJoinAmount = 100m)
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
                "MinimumParticipants": {{minimumParticipants}},
                "MinimumJoinAmount": {{minimumJoinAmount.ToString(CultureInfo.InvariantCulture)}},
                "MaximumWinnerCount": 5,
                "MaximumNamedResolutionCallouts": {{maximumNamedResolutionCallouts}},
                "SuccessfulPotMultiplier": 2.0
              }
            }
            """;
    }

    private static string BuildRewardsConfigurationJson(
        string rewardInterval,
        decimal basePointsPerInterval,
        decimal tier1Multiplier,
        decimal tier2Multiplier,
        decimal tier3Multiplier)
    {
        return
            $$"""
            {
              "Rewards": {
                "RewardInterval": "{{rewardInterval}}",
                "BasePointsPerInterval": {{basePointsPerInterval.ToString(CultureInfo.InvariantCulture)}},
                "Tier1Multiplier": {{tier1Multiplier.ToString(CultureInfo.InvariantCulture)}},
                "Tier2Multiplier": {{tier2Multiplier.ToString(CultureInfo.InvariantCulture)}},
                "Tier3Multiplier": {{tier3Multiplier.ToString(CultureInfo.InvariantCulture)}}
              }
            }
            """;
    }

    private static string BuildRaffleConfigurationJson(
        string joinWindow,
        string oneMinuteReminderThreshold,
        string thirtySecondReminderThreshold,
        string tenSecondReminderThreshold,
        decimal winnerPoints,
        decimal moderatorPointsLimit)
    {
        return
            $$"""
            {
              "Raffle": {
                "JoinWindow": "{{joinWindow}}",
                "OneMinuteReminderThreshold": "{{oneMinuteReminderThreshold}}",
                "ThirtySecondReminderThreshold": "{{thirtySecondReminderThreshold}}",
                "TenSecondReminderThreshold": "{{tenSecondReminderThreshold}}",
                "WinnerPoints": {{winnerPoints.ToString(CultureInfo.InvariantCulture)}},
                "ModeratorPointsLimit": {{moderatorPointsLimit.ToString(CultureInfo.InvariantCulture)}}
              }
            }
            """;
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
