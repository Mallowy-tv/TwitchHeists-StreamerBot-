using System.Diagnostics;
using System.Globalization;
using Microsoft.Data.Sqlite;
using TwitchHeists.Data.Sqlite.Schema;
using TwitchHeists.StreamerBot.Bridge.Models;
using TwitchHeists.StreamerBot.Bridge.Services;

namespace TwitchHeists.Tests.Performance;

public sealed class MixedLoadScenarioRunner : IDisposable
{
    private const int DefaultViewerCount = 1_000;
    private const int DefaultHeistJoinCount = 250;
    private const int DefaultChatEventsPerViewer = 2;
    private const decimal SeedBalancePerViewer = 5_000m;
    private const decimal StarterStake = 100m;
    private const decimal JoinStake = 50m;
    private const decimal RefreshPointsPerViewer = 500m;
    private const decimal StreakPointsPerViewer = 100m;
    private const decimal SuccessfulPotMultiplier = 2m;

    private readonly string installDirectory;
    private readonly string databasePath;
    private readonly BridgeActions actions;

    public MixedLoadScenarioRunner()
    {
        installDirectory = Path.Combine(AppContext.BaseDirectory, "mixed-load", Guid.NewGuid().ToString("N"));
        databasePath = Path.Combine(installDirectory, "data", "twitch-heists.db");
        actions = new BridgeActions();
    }

    public MixedLoadScenarioResult Execute()
    {
        EnsureInstallDirectory();

        var viewers = CreateViewers(DefaultViewerCount);
        SeedBalances(viewers, SeedBalancePerViewer);

        var totalTimer = Stopwatch.StartNew();
        var streamStartedAt = new DateTimeOffset(2026, 4, 26, 18, 0, 0, TimeSpan.Zero);
        EnsureSuccess(
            actions.StartStream(
                installDirectory,
                new BridgeStreamLifecycleCommand
                {
                    OccurredAtUtc = streamStartedAt
                }),
            "start stream");

        var refreshTimestamp = streamStartedAt.AddMinutes(5);
        var refreshTimer = Stopwatch.StartNew();
        var refreshResult = actions.RefreshCommunityViewers(
            installDirectory,
            refreshTimestamp,
            viewers.Select(viewer => new BridgeCommunityViewer
            {
                TwitchUserId = viewer.TwitchUserId,
                Username = viewer.Username,
                DisplayName = viewer.DisplayName,
                SubscriberTier = 0
            }));
        refreshTimer.Stop();
        EnsureSuccess(refreshResult, "community refresh");

        var nextRefreshBoundaryUtc = refreshTimestamp.AddMinutes(5);
        var chatTimer = Stopwatch.StartNew();
        var chatEventCount = 0;
        for (var iteration = 0; iteration < DefaultChatEventsPerViewer; iteration++)
        {
            foreach (var viewer in viewers)
            {
                var chatResult = actions.RecordChatPresence(
                    installDirectory,
                    new BridgeChatPresence
                    {
                        TwitchUserId = viewer.TwitchUserId,
                        Username = viewer.Username,
                        DisplayName = viewer.DisplayName,
                        SubscriberTier = 0,
                        MessageReceivedAtUtc = refreshTimestamp.AddMinutes(1).AddSeconds(iteration).AddMilliseconds(chatEventCount % 1000)
                    },
                    nextRefreshBoundaryUtc);
                EnsureSuccess(chatResult, $"chat presence #{chatEventCount + 1}");
                chatEventCount++;
            }
        }
        chatTimer.Stop();

        var heistStartedAt = streamStartedAt.AddMinutes(10);
        var heistStartTimer = Stopwatch.StartNew();
        var startHeistResult = actions.StartHeist(
            installDirectory,
            new BridgeHeistCommand
            {
                TwitchUserId = viewers[0].TwitchUserId,
                Username = viewers[0].Username,
                DisplayName = viewers[0].DisplayName,
                StakeAmount = StarterStake,
                OccurredAtUtc = heistStartedAt
            });
        heistStartTimer.Stop();
        EnsureSuccess(startHeistResult, "heist start");

        var heistJoinTimer = Stopwatch.StartNew();
        for (var index = 1; index <= DefaultHeistJoinCount; index++)
        {
            var viewer = viewers[index];
            var joinResult = actions.JoinHeist(
                installDirectory,
                new BridgeHeistCommand
                {
                    TwitchUserId = viewer.TwitchUserId,
                    Username = viewer.Username,
                    DisplayName = viewer.DisplayName,
                    StakeAmount = JoinStake,
                    OccurredAtUtc = heistStartedAt.AddSeconds(10).AddMilliseconds(index)
                });
            EnsureSuccess(joinResult, $"heist join #{index}");
        }
        heistJoinTimer.Stop();

        var heistResolveTimer = Stopwatch.StartNew();
        var resolveResult = actions.ResolveDueHeists(installDirectory, heistStartedAt.AddMinutes(2));
        heistResolveTimer.Stop();
        EnsureSuccess(resolveResult, "heist resolve");

        EnsureSuccess(
            actions.EndStream(
                installDirectory,
                new BridgeStreamLifecycleCommand
                {
                    OccurredAtUtc = streamStartedAt.AddHours(4)
                }),
            "end stream");
        totalTimer.Stop();

        return new MixedLoadScenarioResult(
            viewers.Count,
            refreshResult.RewardedViewerCount,
            chatEventCount,
            DefaultHeistJoinCount,
            GetActivePresenceCount(),
            GetHeistParticipantCount(),
            GetTotalTrackedBalance(),
            refreshTimer.Elapsed,
            chatTimer.Elapsed,
            heistStartTimer.Elapsed,
            heistJoinTimer.Elapsed,
            heistResolveTimer.Elapsed,
            totalTimer.Elapsed);
    }

    public void Dispose()
    {
        if (!Directory.Exists(installDirectory))
        {
            return;
        }

        SqliteConnection.ClearAllPools();

        for (var attempt = 0; attempt < 3; attempt++)
        {
            try
            {
                Directory.Delete(installDirectory, recursive: true);
                return;
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

    private void EnsureInstallDirectory()
    {
        Directory.CreateDirectory(Path.Combine(installDirectory, "data"));
        File.WriteAllText(Path.Combine(installDirectory, "appsettings.json"), BuildConfigurationJson());
    }

    private void SeedBalances(IReadOnlyList<LoadViewer> viewers, decimal seedBalance)
    {
        using var connection = new SqliteConnection($@"Data Source={databasePath};Pooling=False");
        connection.Open();
        new SchemaBootstrapper().EnsureCreated(connection);
        using var transaction = connection.BeginTransaction();
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
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
                total_watch_minutes = excluded.total_watch_minutes,
                updated_at_utc = excluded.updated_at_utc;
            """;
        var normalizedUsernameParameter = command.Parameters.Add("$normalizedUsername", SqliteType.Text);
        var twitchUserIdParameter = command.Parameters.Add("$twitchUserId", SqliteType.Text);
        var pointsBalanceParameter = command.Parameters.Add("$pointsBalance", SqliteType.Text);
        var updatedAtParameter = command.Parameters.Add("$updatedAtUtc", SqliteType.Text);
        var updatedAtUtc = DateTimeOffset.UtcNow.UtcDateTime.ToString("O", CultureInfo.InvariantCulture);

        foreach (var viewer in viewers)
        {
            normalizedUsernameParameter.Value = viewer.Username;
            twitchUserIdParameter.Value = viewer.TwitchUserId;
            pointsBalanceParameter.Value = seedBalance.ToString(CultureInfo.InvariantCulture);
            updatedAtParameter.Value = updatedAtUtc;
            command.ExecuteNonQuery();
        }

        transaction.Commit();
    }

    private int GetActivePresenceCount()
    {
        using var connection = OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT COUNT(1)
            FROM viewer_presence
            WHERE is_active = 1;
            """;
        return Convert.ToInt32(command.ExecuteScalar(), CultureInfo.InvariantCulture);
    }

    private int GetHeistParticipantCount()
    {
        using var connection = OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT COUNT(1)
            FROM heist_participants;
            """;
        return Convert.ToInt32(command.ExecuteScalar(), CultureInfo.InvariantCulture);
    }

    private decimal GetTotalTrackedBalance()
    {
        using var connection = OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT points_balance
            FROM viewer_balances;
            """;

        using var reader = command.ExecuteReader();
        var total = 0m;
        while (reader.Read())
        {
            total += decimal.Parse(reader.GetString(0), CultureInfo.InvariantCulture);
        }

        return total;
    }

    private SqliteConnection OpenConnection()
    {
        var connection = new SqliteConnection($@"Data Source={databasePath};Pooling=False");
        connection.Open();
        return connection;
    }

    private static List<LoadViewer> CreateViewers(int viewerCount)
    {
        var viewers = new List<LoadViewer>(viewerCount);
        for (var index = 1; index <= viewerCount; index++)
        {
            viewers.Add(new LoadViewer(
                $"load-viewer-{index:D4}",
                $"viewer{index:D4}",
                $"Viewer {index:D4}"));
        }

        return viewers;
    }

    private static void EnsureSuccess(BridgeResult result, string phase)
    {
        if (!result.Success)
        {
            throw new InvalidOperationException($"{phase} failed: {result.Message}");
        }
    }

    public static decimal CalculateExpectedTotalTrackedBalance(int viewerCount, int heistJoinCount)
    {
        var seededTotal = SeedBalancePerViewer * viewerCount;
        var refreshTotal = RefreshPointsPerViewer * viewerCount;
        var streakTotal = StreakPointsPerViewer * viewerCount;
        var successfulHeistPot = StarterStake + (JoinStake * heistJoinCount);
        var netHeistIncrease = successfulHeistPot * (SuccessfulPotMultiplier - 1m);

        return seededTotal + refreshTotal + streakTotal + netHeistIncrease;
    }

    private static string BuildConfigurationJson()
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
                "MinimumSuccessChance": 1.0,
                "MaximumSuccessChance": 1.0,
                "MinimumJoinAmount": 50.0,
                "MaximumWinnerCount": 5,
                "SuccessfulPotMultiplier": 2.0
              }
            }
            """;
    }

    private sealed record LoadViewer(string TwitchUserId, string Username, string DisplayName);
}
