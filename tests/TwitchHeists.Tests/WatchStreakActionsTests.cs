using TwitchHeists.Core.Models;
using TwitchHeists.Core.Options;
using TwitchHeists.Core.Services;
using TwitchHeists.Data.Sqlite.Repositories;
using TwitchHeists.Data.Sqlite.Schema;
using TwitchHeists.StreamerBot.Contracts;
using TwitchHeists.StreamerBot.Services;

namespace TwitchHeists.Tests;

public sealed class WatchStreakActionsTests : IDisposable
{
    private readonly string databasePath;

    public WatchStreakActionsTests()
    {
        databasePath = Path.Combine(AppContext.BaseDirectory, $"{Guid.NewGuid():N}.watchstreak.db");
    }

    [Fact]
    public void StartStreamAndEndStream_GateWhetherChatPresenceCanAwardAStreak()
    {
        var viewerRepository = CreateViewerRepository();
        var watchStreakRepository = CreateWatchStreakRepository();
        var watchStreakService = new WatchStreakService(watchStreakRepository, viewerRepository);
        var startAction = new StartStreamAction(watchStreakRepository);
        var endAction = new EndStreamAction(watchStreakRepository);
        var recordAction = new RecordChatPresenceAction(viewerRepository, watchStreakService);
        var startedAt = new DateTimeOffset(2026, 4, 25, 18, 0, 0, TimeSpan.Zero);
        var viewer = CreateViewerIdentity("viewer-id-1", "viewerone", "ViewerOne");

        var startResult = startAction.Execute(new StreamLifecycleCommandDto { OccurredAtUtc = startedAt });

        Assert.True(startResult.Success);
        Assert.True(watchStreakRepository.GetStreamState().IsStreamActive);

        recordAction.Execute(
            new ChatPresenceDto
            {
                TwitchUserId = viewer.TwitchUserId,
                Username = viewer.Username,
                DisplayName = viewer.DisplayName,
                MessageReceivedAtUtc = startedAt.AddMinutes(1)
            },
            startedAt.AddMinutes(5));

        Assert.Equal(100m, viewerRepository.GetViewerBalance(viewer));

        var endResult = endAction.Execute(new StreamLifecycleCommandDto { OccurredAtUtc = startedAt.AddHours(4) });

        Assert.True(endResult.Success);
        Assert.False(watchStreakRepository.GetStreamState().IsStreamActive);

        recordAction.Execute(
            new ChatPresenceDto
            {
                TwitchUserId = viewer.TwitchUserId,
                Username = viewer.Username,
                DisplayName = viewer.DisplayName,
                MessageReceivedAtUtc = startedAt.AddHours(5)
            },
            startedAt.AddHours(5).AddMinutes(5));

        Assert.Equal(100m, viewerRepository.GetViewerBalance(viewer));
    }

    [Fact]
    public void RecordChatPresenceAction_AwardsOnlyTheFirstSightingPerStream()
    {
        var viewerRepository = CreateViewerRepository();
        var watchStreakRepository = CreateWatchStreakRepository();
        var watchStreakService = new WatchStreakService(watchStreakRepository, viewerRepository);
        var startAction = new StartStreamAction(watchStreakRepository);
        var recordAction = new RecordChatPresenceAction(viewerRepository, watchStreakService);
        var startedAt = new DateTimeOffset(2026, 4, 25, 18, 0, 0, TimeSpan.Zero);
        var viewer = CreateViewerIdentity("viewer-id-2", "viewertwo", "ViewerTwo");

        startAction.Execute(new StreamLifecycleCommandDto { OccurredAtUtc = startedAt });

        var firstResult = recordAction.Execute(
            new ChatPresenceDto
            {
                TwitchUserId = viewer.TwitchUserId,
                Username = viewer.Username,
                DisplayName = viewer.DisplayName,
                MessageReceivedAtUtc = startedAt.AddMinutes(1)
            },
            startedAt.AddMinutes(5));
        var secondResult = recordAction.Execute(
            new ChatPresenceDto
            {
                TwitchUserId = viewer.TwitchUserId,
                Username = viewer.Username,
                DisplayName = viewer.DisplayName,
                MessageReceivedAtUtc = startedAt.AddMinutes(2)
            },
            startedAt.AddMinutes(5));

        Assert.True(firstResult.Success);
        Assert.True(secondResult.Success);
        Assert.Equal(100m, viewerRepository.GetViewerBalance(viewer));
        Assert.Equal(1, watchStreakRepository.GetViewerStreak(viewer)?.CurrentStreak);
    }

    [Fact]
    public void RecordChatPresenceAction_ContinuesTheStreakAcrossConsecutiveCompletedStreams()
    {
        var viewerRepository = CreateViewerRepository();
        var watchStreakRepository = CreateWatchStreakRepository();
        var watchStreakService = new WatchStreakService(watchStreakRepository, viewerRepository);
        var startAction = new StartStreamAction(watchStreakRepository);
        var endAction = new EndStreamAction(watchStreakRepository);
        var recordAction = new RecordChatPresenceAction(viewerRepository, watchStreakService);
        var firstStreamStartedAt = new DateTimeOffset(2026, 4, 25, 18, 0, 0, TimeSpan.Zero);
        var secondStreamStartedAt = firstStreamStartedAt.AddDays(1);

        startAction.Execute(new StreamLifecycleCommandDto { OccurredAtUtc = firstStreamStartedAt });
        recordAction.Execute(
            new ChatPresenceDto
            {
                TwitchUserId = "viewer-id-3",
                Username = "legacyname",
                DisplayName = "LegacyName",
                MessageReceivedAtUtc = firstStreamStartedAt.AddMinutes(1)
            },
            firstStreamStartedAt.AddMinutes(5));
        endAction.Execute(new StreamLifecycleCommandDto { OccurredAtUtc = firstStreamStartedAt.AddHours(4) });

        startAction.Execute(new StreamLifecycleCommandDto { OccurredAtUtc = secondStreamStartedAt });
        recordAction.Execute(
            new ChatPresenceDto
            {
                TwitchUserId = "viewer-id-3",
                Username = "renamedviewer",
                DisplayName = "RenamedViewer",
                MessageReceivedAtUtc = secondStreamStartedAt.AddMinutes(1)
            },
            secondStreamStartedAt.AddMinutes(5));

        var renamedViewer = CreateViewerIdentity("viewer-id-3", "renamedviewer", "RenamedViewer");

        Assert.Equal(300m, viewerRepository.GetViewerBalance(renamedViewer));
        Assert.Equal(2, watchStreakRepository.GetViewerStreak(renamedViewer)?.CurrentStreak);
    }

    [Fact]
    public void RecordChatPresenceAction_ResetsTheStreakAfterAMissedCompletedStream()
    {
        var viewerRepository = CreateViewerRepository();
        var watchStreakRepository = CreateWatchStreakRepository();
        var watchStreakService = new WatchStreakService(watchStreakRepository, viewerRepository);
        var startAction = new StartStreamAction(watchStreakRepository);
        var endAction = new EndStreamAction(watchStreakRepository);
        var recordAction = new RecordChatPresenceAction(viewerRepository, watchStreakService);
        var firstStreamStartedAt = new DateTimeOffset(2026, 4, 25, 18, 0, 0, TimeSpan.Zero);
        var secondStreamStartedAt = firstStreamStartedAt.AddDays(1);
        var thirdStreamStartedAt = secondStreamStartedAt.AddDays(1);
        var viewer = CreateViewerIdentity("viewer-id-4", "viewerfour", "ViewerFour");

        startAction.Execute(new StreamLifecycleCommandDto { OccurredAtUtc = firstStreamStartedAt });
        recordAction.Execute(
            new ChatPresenceDto
            {
                TwitchUserId = viewer.TwitchUserId,
                Username = viewer.Username,
                DisplayName = viewer.DisplayName,
                MessageReceivedAtUtc = firstStreamStartedAt.AddMinutes(1)
            },
            firstStreamStartedAt.AddMinutes(5));
        endAction.Execute(new StreamLifecycleCommandDto { OccurredAtUtc = firstStreamStartedAt.AddHours(4) });

        startAction.Execute(new StreamLifecycleCommandDto { OccurredAtUtc = secondStreamStartedAt });
        endAction.Execute(new StreamLifecycleCommandDto { OccurredAtUtc = secondStreamStartedAt.AddHours(4) });

        startAction.Execute(new StreamLifecycleCommandDto { OccurredAtUtc = thirdStreamStartedAt });
        recordAction.Execute(
            new ChatPresenceDto
            {
                TwitchUserId = viewer.TwitchUserId,
                Username = viewer.Username,
                DisplayName = viewer.DisplayName,
                MessageReceivedAtUtc = thirdStreamStartedAt.AddMinutes(1)
            },
            thirdStreamStartedAt.AddMinutes(5));

        Assert.Equal(200m, viewerRepository.GetViewerBalance(viewer));
        Assert.Equal(1, watchStreakRepository.GetViewerStreak(viewer)?.CurrentStreak);
    }

    [Fact]
    public void RefreshCommunityViewersAction_AwardsTheStreakSilentlyDuringAnActiveStream()
    {
        var viewerRepository = CreateViewerRepository();
        var watchStreakRepository = CreateWatchStreakRepository();
        var watchStreakService = new WatchStreakService(watchStreakRepository, viewerRepository);
        var startAction = new StartStreamAction(watchStreakRepository);
        var refreshAction = new RefreshCommunityViewersAction(
            viewerRepository,
            new WatchtimeCalculator(new RewardSettings { BasePointsPerInterval = 0m }),
            watchStreakService);
        var startedAt = new DateTimeOffset(2026, 4, 25, 18, 0, 0, TimeSpan.Zero);
        var viewer = CreateViewerIdentity("viewer-id-5", "viewerfive", "ViewerFive");

        startAction.Execute(new StreamLifecycleCommandDto { OccurredAtUtc = startedAt });

        var firstResult = refreshAction.Execute(
            startedAt.AddMinutes(5),
            new[]
            {
                new CommunityViewerDto
                {
                    TwitchUserId = viewer.TwitchUserId,
                    Username = viewer.Username,
                    DisplayName = viewer.DisplayName
                }
            });
        var secondResult = refreshAction.Execute(
            startedAt.AddMinutes(10),
            new[]
            {
                new CommunityViewerDto
                {
                    TwitchUserId = viewer.TwitchUserId,
                    Username = viewer.Username,
                    DisplayName = viewer.DisplayName
                }
            });

        Assert.Equal("Refresh cycle applied.", firstResult.Message);
        Assert.Equal("Refresh cycle applied.", secondResult.Message);
        Assert.Equal(100m, viewerRepository.GetViewerBalance(viewer));
        Assert.Equal(1, watchStreakRepository.GetViewerStreak(viewer)?.CurrentStreak);
    }

    public void Dispose()
    {
        if (File.Exists(databasePath))
        {
            File.Delete(databasePath);
        }
    }

    private ViewerRepository CreateViewerRepository()
    {
        return new ViewerRepository($"Data Source={databasePath};Pooling=False", new SchemaBootstrapper());
    }

    private WatchStreakRepository CreateWatchStreakRepository()
    {
        return new WatchStreakRepository($"Data Source={databasePath};Pooling=False", new SchemaBootstrapper());
    }

    private static ViewerIdentity CreateViewerIdentity(string? twitchUserId, string username, string displayName)
    {
        return new ViewerIdentity
        {
            TwitchUserId = twitchUserId,
            Username = username,
            NormalizedUsername = username.Trim().ToLowerInvariant(),
            DisplayName = displayName
        };
    }
}
