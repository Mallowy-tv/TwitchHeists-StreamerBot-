using System.Globalization;
using Microsoft.Data.Sqlite;
using TwitchHeists.Core.Models;
using TwitchHeists.Data.Sqlite.Repositories;
using TwitchHeists.Data.Sqlite.Schema;

namespace TwitchHeists.Tests;

public sealed class ViewerRepositoryIdentityTests : IDisposable
{
    private readonly string databasePath;

    public ViewerRepositoryIdentityTests()
    {
        databasePath = Path.Combine(AppContext.BaseDirectory, $"{Guid.NewGuid():N}.identity.db");
    }

    [Fact]
    public void ApplyRewardCycle_AdoptsLegacyUsernameOnlyBalanceWhenTwitchIdArrives()
    {
        SeedBalance("viewerone", twitchUserId: null, 100m, 30);
        var repository = CreateRepository();
        var rewardedAt = new DateTimeOffset(2026, 4, 25, 16, 0, 0, TimeSpan.Zero);

        var applied = repository.ApplyRewardCycle(
            rewardedAt,
            Array.Empty<ViewerPresenceRecord>(),
            Array.Empty<ViewerPresenceRecord>(),
            new[]
            {
                new ViewerRewardResult
                {
                    Identity = new ViewerIdentity
                    {
                        TwitchUserId = "viewer-id-1",
                        Username = "viewerone",
                        NormalizedUsername = "viewerone",
                        DisplayName = "ViewerOne"
                    },
                    PointsAwarded = 10m,
                    WatchMinutesAwarded = 5,
                    RewardedAtUtc = rewardedAt
                }
            });

        Assert.True(applied);
        Assert.Equal(1, CountBalanceRows());
        Assert.Equal("viewer-id-1", GetBalanceTwitchUserId("viewerone"));
    }

    [Fact]
    public void ApplyRewardCycle_UsesTwitchIdToPreserveBalanceAcrossRename()
    {
        SeedBalance("legacyname", "viewer-id-2", 100m, 30);
        var repository = CreateRepository();
        var rewardedAt = new DateTimeOffset(2026, 4, 25, 16, 0, 0, TimeSpan.Zero);

        var applied = repository.ApplyRewardCycle(
            rewardedAt,
            Array.Empty<ViewerPresenceRecord>(),
            Array.Empty<ViewerPresenceRecord>(),
            new[]
            {
                new ViewerRewardResult
                {
                    Identity = new ViewerIdentity
                    {
                        TwitchUserId = "viewer-id-2",
                        Username = "renamedviewer",
                        NormalizedUsername = "renamedviewer",
                        DisplayName = "RenamedViewer"
                    },
                    PointsAwarded = 25m,
                    WatchMinutesAwarded = 15,
                    RewardedAtUtc = rewardedAt
                }
            });

        Assert.True(applied);
        Assert.Equal(1, CountBalanceRows());
        Assert.Equal("viewer-id-2", GetBalanceTwitchUserId("renamedviewer"));
        Assert.Equal(125m, GetBalanceValue("renamedviewer"));
        Assert.Equal(45, GetWatchMinutes("renamedviewer"));
    }

    [Fact]
    public void ApplyRewardCycle_DoesNotClearExistingTwitchIdWhenRewardIdentityOmitsIt()
    {
        SeedBalance("viewerkeep", "viewer-id-keep", 100m, 30);
        var repository = CreateRepository();
        var rewardedAt = new DateTimeOffset(2026, 4, 25, 16, 0, 0, TimeSpan.Zero);

        var applied = repository.ApplyRewardCycle(
            rewardedAt,
            Array.Empty<ViewerPresenceRecord>(),
            Array.Empty<ViewerPresenceRecord>(),
            new[]
            {
                new ViewerRewardResult
                {
                    Identity = new ViewerIdentity
                    {
                        Username = "viewerkeep",
                        NormalizedUsername = "viewerkeep",
                        DisplayName = "ViewerKeep"
                    },
                    PointsAwarded = 10m,
                    WatchMinutesAwarded = 5,
                    RewardedAtUtc = rewardedAt
                }
            });

        Assert.True(applied);
        Assert.Equal("viewer-id-keep", GetBalanceTwitchUserId("viewerkeep"));
        Assert.Equal(110m, GetBalanceValue("viewerkeep"));
        Assert.Equal(35, GetWatchMinutes("viewerkeep"));
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

    private void SeedBalance(string normalizedUsername, string? twitchUserId, decimal pointsBalance, int totalWatchMinutes)
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

    private int CountBalanceRows()
    {
        using var connection = new SqliteConnection($"Data Source={databasePath};Pooling=False");
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(1) FROM viewer_balances;";
        return Convert.ToInt32(command.ExecuteScalar(), CultureInfo.InvariantCulture);
    }

    private string? GetBalanceTwitchUserId(string normalizedUsername)
    {
        using var connection = new SqliteConnection($"Data Source={databasePath};Pooling=False");
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT twitch_user_id
            FROM viewer_balances
            WHERE normalized_username = $normalizedUsername;
            """;
        command.Parameters.AddWithValue("$normalizedUsername", normalizedUsername);
        return command.ExecuteScalar() as string;
    }

    private decimal GetBalanceValue(string normalizedUsername)
    {
        using var connection = new SqliteConnection($"Data Source={databasePath};Pooling=False");
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT points_balance
            FROM viewer_balances
            WHERE normalized_username = $normalizedUsername;
            """;
        command.Parameters.AddWithValue("$normalizedUsername", normalizedUsername);
        return decimal.Parse((string)command.ExecuteScalar()!, CultureInfo.InvariantCulture);
    }

    private int GetWatchMinutes(string normalizedUsername)
    {
        using var connection = new SqliteConnection($"Data Source={databasePath};Pooling=False");
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT total_watch_minutes
            FROM viewer_balances
            WHERE normalized_username = $normalizedUsername;
            """;
        command.Parameters.AddWithValue("$normalizedUsername", normalizedUsername);
        return Convert.ToInt32(command.ExecuteScalar(), CultureInfo.InvariantCulture);
    }
}
