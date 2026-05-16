using System.Globalization;
using Microsoft.Data.Sqlite;
using TwitchHeists.Data.Sqlite.Repositories;
using TwitchHeists.Data.Sqlite.Schema;
using TwitchHeists.StreamerBot.Bridge.Services;
using TwitchHeists.StreamerBot.Services;

namespace TwitchHeists.Tests;

public sealed class LeaderboardActionsTests : IDisposable
{
    private readonly string databasePath;
    private readonly string installDirectory;

    public LeaderboardActionsTests()
    {
        var suffix = Guid.NewGuid().ToString("N");
        databasePath = Path.Combine(AppContext.BaseDirectory, $"{suffix}.leaderboard.db");
        installDirectory = Path.Combine(AppContext.BaseDirectory, "leaderboard-install", suffix);
    }

    [Fact]
    public void GetLeaderboardAction_ReturnsTopFiveBalancesInDescendingOrder()
    {
        SeedBalance("alpha", 100m);
        SeedBalance("beta", 5_000m);
        SeedBalance("delta", 5_000m);
        SeedBalance("gamma", 2_500m);
        SeedBalance("zeta", 1_000m);
        SeedBalance("epsilon", 50m);
        var action = new GetLeaderboardAction(CreateRepository());

        var result = action.Execute();

        Assert.True(result.Success);
        Assert.Equal(
            string.Join(
                Environment.NewLine,
                new[]
                {
                    "1. beta (5000)",
                    "2. delta (5000)",
                    "3. gamma (2500)",
                    "4. zeta (1000)",
                    "5. alpha (100)"
                }),
            result.Message);
    }

    [Fact]
    public void GetLeaderboardAction_ReturnsFriendlyMessageWhenNoBalancesExist()
    {
        var action = new GetLeaderboardAction(CreateRepository());

        var result = action.Execute();

        Assert.True(result.Success);
        Assert.Equal("No point balances found.", result.Message);
    }

    [Fact]
    public void BridgeActions_GetLeaderboard_ReturnsTopFiveBalances()
    {
        EnsureInstallDirectory();
        var installDatabasePath = GetInstallDatabasePath();
        SeedBalance(installDatabasePath, "alpha", 100m);
        SeedBalance(installDatabasePath, "beta", 5_000m);
        SeedBalance(installDatabasePath, "delta", 5_000m);
        SeedBalance(installDatabasePath, "gamma", 2_500m);
        SeedBalance(installDatabasePath, "zeta", 1_000m);
        SeedBalance(installDatabasePath, "epsilon", 50m);
        var actions = new BridgeActions();

        var result = actions.GetLeaderboard(installDirectory);

        Assert.True(result.Success);
        Assert.Equal(
            string.Join(
                Environment.NewLine,
                new[]
                {
                    "1. beta (5000)",
                    "2. delta (5000)",
                    "3. gamma (2500)",
                    "4. zeta (1000)",
                    "5. alpha (100)"
                }),
            result.Message);
    }

    public void Dispose()
    {
        if (File.Exists(databasePath))
        {
            File.Delete(databasePath);
        }

        if (Directory.Exists(installDirectory))
        {
            SqliteConnection.ClearAllPools();
            Directory.Delete(installDirectory, recursive: true);
        }
    }

    private ViewerRepository CreateRepository()
    {
        return new ViewerRepository($"Data Source={databasePath};Pooling=False", new SchemaBootstrapper());
    }

    private void EnsureInstallDirectory()
    {
        Directory.CreateDirectory(Path.Combine(installDirectory, "data"));
        File.WriteAllText(Path.Combine(installDirectory, "appsettings.json"), "{}");
    }

    private void SeedBalance(string normalizedUsername, decimal pointsBalance)
    {
        SeedBalance(databasePath, normalizedUsername, pointsBalance);
    }

    private static void SeedBalance(string databaseFilePath, string normalizedUsername, decimal pointsBalance)
    {
        using var connection = new SqliteConnection($"Data Source={databaseFilePath};Pooling=False");
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
        command.Parameters.AddWithValue("$twitchUserId", DBNull.Value);
        command.Parameters.AddWithValue("$pointsBalance", pointsBalance.ToString(CultureInfo.InvariantCulture));
        command.Parameters.AddWithValue("$updatedAtUtc", DateTimeOffset.UtcNow.UtcDateTime.ToString("O", CultureInfo.InvariantCulture));
        command.ExecuteNonQuery();
    }

    private string GetInstallDatabasePath()
    {
        return Path.Combine(installDirectory, "data", "twitch-heists.db");
    }
}
