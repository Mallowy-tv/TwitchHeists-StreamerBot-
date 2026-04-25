using System.Globalization;
using Microsoft.Data.Sqlite;
using TwitchHeists.Data.Sqlite.Repositories;
using TwitchHeists.Data.Sqlite.Schema;
using TwitchHeists.StreamerBot.Contracts;
using TwitchHeists.StreamerBot.Services;

namespace TwitchHeists.Tests;

public sealed class PointsActionsTests : IDisposable
{
    private readonly string databasePath;

    public PointsActionsTests()
    {
        databasePath = Path.Combine(AppContext.BaseDirectory, $"{Guid.NewGuid():N}.db");
    }

    [Fact]
    public void GivePointsAction_TransfersPointsFromSenderToRecipient()
    {
        SeedBalance("giver", 500m);
        var repository = CreateRepository();
        var action = new GivePointsAction(repository);

        var result = action.Execute(new PointsCommandDto
        {
            SourceUsername = "giver",
            SourceDisplayName = "Giver",
            TargetUsername = "friend",
            TargetDisplayName = "Friend",
            Amount = 200m,
            OccurredAtUtc = new DateTimeOffset(2026, 4, 25, 16, 0, 0, TimeSpan.Zero)
        });

        Assert.True(result.Success);
        Assert.Equal(300m, repository.GetViewerBalance("giver"));
        Assert.Equal(200m, repository.GetViewerBalance("friend"));
    }

    [Fact]
    public void GivePointsAction_FailsWhenSenderLacksEnoughPoints()
    {
        SeedBalance("giver", 100m);
        var repository = CreateRepository();
        var action = new GivePointsAction(repository);

        var result = action.Execute(new PointsCommandDto
        {
            SourceUsername = "giver",
            SourceDisplayName = "Giver",
            TargetUsername = "friend",
            TargetDisplayName = "Friend",
            Amount = 200m,
            OccurredAtUtc = new DateTimeOffset(2026, 4, 25, 16, 0, 0, TimeSpan.Zero)
        });

        Assert.False(result.Success);
        Assert.Contains("enough points", result.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(100m, repository.GetViewerBalance("giver"));
        Assert.Equal(0m, repository.GetViewerBalance("friend"));
    }

    [Fact]
    public void RemovePointsAction_ClampsTargetBalanceAtZero()
    {
        SeedBalance("target", 30m);
        var repository = CreateRepository();
        var action = new RemovePointsAction(repository);

        var result = action.Execute(new PointsCommandDto
        {
            TargetUsername = "target",
            TargetDisplayName = "Target",
            Amount = 100m,
            OccurredAtUtc = new DateTimeOffset(2026, 4, 25, 16, 0, 0, TimeSpan.Zero)
        });

        Assert.True(result.Success);
        Assert.Equal(0m, repository.GetViewerBalance("target"));
    }

    [Fact]
    public void AddPointsAction_IncreasesTargetBalance()
    {
        var repository = CreateRepository();
        var action = new AddPointsAction(repository);

        var result = action.Execute(new PointsCommandDto
        {
            TargetUsername = "target",
            TargetDisplayName = "Target",
            Amount = 250m,
            OccurredAtUtc = new DateTimeOffset(2026, 4, 25, 16, 0, 0, TimeSpan.Zero)
        });

        Assert.True(result.Success);
        Assert.Equal(250m, repository.GetViewerBalance("target"));
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

    private void SeedBalance(string normalizedUsername, decimal pointsBalance, int totalWatchMinutes = 0)
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
