using System.Globalization;
using Microsoft.Data.Sqlite;
using TwitchHeists.Data.Sqlite.Repositories;
using TwitchHeists.Data.Sqlite.Schema;
using TwitchHeists.StreamerBot.Contracts;
using TwitchHeists.StreamerBot.Services;

namespace TwitchHeists.Tests;

public sealed class PointsLookupActionsTests : IDisposable
{
    private readonly string databasePath;

    public PointsLookupActionsTests()
    {
        databasePath = Path.Combine(AppContext.BaseDirectory, $"{Guid.NewGuid():N}.points-lookup.db");
    }

    [Fact]
    public void GetPointsAction_ReturnsRequesterBalanceWhenNoTargetIsSupplied()
    {
        SeedBalance("viewerone", 250m);
        var repository = CreateRepository();
        var action = new GetPointsAction(repository);

        var result = action.Execute(new PointsQueryDto
        {
            RequesterUsername = "viewerone",
            RequesterDisplayName = "ViewerOne",
            OccurredAtUtc = new DateTimeOffset(2026, 5, 16, 16, 0, 0, TimeSpan.Zero)
        });

        Assert.True(result.Success);
        Assert.Contains("250", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void GetPointsAction_ReturnsTargetBalanceWhenUsernameIsSupplied()
    {
        SeedBalance("viewertwo", 750m);
        var repository = CreateRepository();
        var action = new GetPointsAction(repository);

        var result = action.Execute(new PointsQueryDto
        {
            RequesterUsername = "viewerone",
            RequesterDisplayName = "ViewerOne",
            TargetUsername = "viewertwo",
            TargetDisplayName = "ViewerTwo",
            OccurredAtUtc = new DateTimeOffset(2026, 5, 16, 16, 0, 0, TimeSpan.Zero)
        });

        Assert.True(result.Success);
        Assert.Contains("750", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void GetPointsAction_UsesTargetTwitchIdWhenUsernameChanged()
    {
        SeedBalance("legacyname", "points-123", 1200m);
        var repository = CreateRepository();
        var action = new GetPointsAction(repository);

        var result = action.Execute(new PointsQueryDto
        {
            RequesterTwitchUserId = "requester-1",
            RequesterUsername = "viewerone",
            RequesterDisplayName = "ViewerOne",
            TargetTwitchUserId = "points-123",
            TargetUsername = "renamedviewer",
            TargetDisplayName = "RenamedViewer",
            OccurredAtUtc = new DateTimeOffset(2026, 5, 16, 16, 0, 0, TimeSpan.Zero)
        });

        Assert.True(result.Success);
        Assert.Contains("1200", result.Message, StringComparison.OrdinalIgnoreCase);
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
