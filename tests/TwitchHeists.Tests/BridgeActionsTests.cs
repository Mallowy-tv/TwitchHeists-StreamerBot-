using System.Globalization;
using Microsoft.Data.Sqlite;
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

    public void Dispose()
    {
        if (Directory.Exists(installDirectory))
        {
            Directory.Delete(installDirectory, true);
        }
    }

    private void EnsureInstallDirectory()
    {
        Directory.CreateDirectory(Path.Combine(installDirectory, "data"));
        File.WriteAllText(Path.Combine(installDirectory, "appsettings.json"), "{}");
    }

    private void SeedBalance(string normalizedUsername, decimal pointsBalance, int totalWatchMinutes)
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
                points_balance,
                total_watch_minutes,
                updated_at_utc
            )
            VALUES (
                $normalizedUsername,
                $pointsBalance,
                $totalWatchMinutes,
                $updatedAtUtc
            )
            ON CONFLICT(normalized_username) DO UPDATE SET
                points_balance = excluded.points_balance,
                total_watch_minutes = excluded.total_watch_minutes,
                updated_at_utc = excluded.updated_at_utc;
            """;
        command.Parameters.AddWithValue("$normalizedUsername", normalizedUsername);
        command.Parameters.AddWithValue("$pointsBalance", pointsBalance.ToString(CultureInfo.InvariantCulture));
        command.Parameters.AddWithValue("$totalWatchMinutes", totalWatchMinutes);
        command.Parameters.AddWithValue("$updatedAtUtc", DateTimeOffset.UtcNow.UtcDateTime.ToString("O", CultureInfo.InvariantCulture));
        command.ExecuteNonQuery();
    }
}
