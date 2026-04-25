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
        Assert.Equal("starter started a heist with 100 points. Starting in 2 minutes.", result.Message);
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
    public void ResolveDueHeistsAction_ResolvesTheHeistWhenDue()
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
        Assert.Contains("crew", result.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Success chance was", result.Message, StringComparison.OrdinalIgnoreCase);
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
        Assert.Equal("renamedviewer started a heist with 100 points. Starting in 2 minutes.", result.Message);
    }

    [Fact]
    public void ResolveDueHeistsAction_UsesProvidedMessageTemplates()
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
            ResultSummaries = new List<string> { "ACTION SUMMARY {winnerCount} {loserCount} {resolvedPot} {successChancePercent}" }
        });
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
            messageComposer ?? CreateMessageComposer());
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
            messageComposer ?? CreateMessageComposer());
    }

    private static HeistSettings CreateSettings()
    {
        return new HeistSettings
        {
            JoinWindow = TimeSpan.FromMinutes(2),
            MaximumWinnerCount = 5,
            SuccessfulPotMultiplier = 2.0m
        };
    }

    private static HeistMessageComposer CreateMessageComposer(HeistMessageTemplates? templates = null)
    {
        return new HeistMessageComposer(templates ?? HeistMessageTemplates.CreateDefault(), _ => 0);
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
