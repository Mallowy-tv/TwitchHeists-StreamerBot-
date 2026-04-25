using System.Globalization;
using Microsoft.Data.Sqlite;
using TwitchHeists.Data.Sqlite.Repositories;
using TwitchHeists.Data.Sqlite.Schema;
using TwitchHeists.StreamerBot.Contracts;
using TwitchHeists.StreamerBot.Services;

namespace TwitchHeists.Tests;

public sealed class WatchtimeActionsTests : IDisposable
{
    private readonly string databasePath;

    public WatchtimeActionsTests()
    {
        databasePath = Path.Combine(AppContext.BaseDirectory, $"{Guid.NewGuid():N}.db");
    }

    [Fact]
    public void GetWatchtimeAction_ReturnsRequesterLifetimeWhenNoTargetIsSupplied()
    {
        SeedBalance("viewerone", 0m, 125);
        var repository = CreateRepository();
        var action = new GetWatchtimeAction(repository);

        var result = action.Execute(new WatchtimeQueryDto
        {
            RequesterUsername = "viewerone",
            RequesterDisplayName = "ViewerOne",
            OccurredAtUtc = new DateTimeOffset(2026, 4, 25, 16, 0, 0, TimeSpan.Zero)
        });

        Assert.True(result.Success);
        Assert.Contains("2h 5m", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void GetWatchtimeAction_ReturnsTargetLifetimeWhenUsernameIsSupplied()
    {
        SeedBalance("viewertwo", 0m, 65);
        var repository = CreateRepository();
        var action = new GetWatchtimeAction(repository);

        var result = action.Execute(new WatchtimeQueryDto
        {
            RequesterUsername = "viewerone",
            RequesterDisplayName = "ViewerOne",
            TargetUsername = "viewertwo",
            TargetDisplayName = "ViewerTwo",
            OccurredAtUtc = new DateTimeOffset(2026, 4, 25, 16, 0, 0, TimeSpan.Zero)
        });

        Assert.True(result.Success);
        Assert.Contains("1h 5m", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void GetWatchtimeAction_ReturnsZeroForAUserWithNoStoredWatchtime()
    {
        var repository = CreateRepository();
        var action = new GetWatchtimeAction(repository);

        var result = action.Execute(new WatchtimeQueryDto
        {
            RequesterUsername = "newviewer",
            RequesterDisplayName = "NewViewer",
            OccurredAtUtc = new DateTimeOffset(2026, 4, 25, 16, 0, 0, TimeSpan.Zero)
        });

        Assert.True(result.Success);
        Assert.Contains("0m", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    public void Dispose()
    {
        if (File.Exists(databasePath))
        {
            File.Delete(databasePath);
        }
    }

    private ViewerRepository CreateRepository()
    {
        return new ViewerRepository($"Data Source={databasePath};Pooling=False", new SchemaBootstrapper());
    }

    private void SeedBalance(string normalizedUsername, decimal pointsBalance, int totalWatchMinutes)
    {
        using var connection = new SqliteConnection($"Data Source={databasePath};Pooling=False");
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
