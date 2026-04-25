using TwitchHeists.Core.Models;
using TwitchHeists.Core.Options;
using TwitchHeists.Core.Services;
using TwitchHeists.Data.Sqlite.Repositories;
using TwitchHeists.Data.Sqlite.Schema;
using TwitchHeists.StreamerBot.Contracts;
using TwitchHeists.StreamerBot.Services;

namespace TwitchHeists.Tests;

public sealed class RefreshCommunityViewersActionTests : IDisposable
{
    private readonly string databasePath;

    public RefreshCommunityViewersActionTests()
    {
        databasePath = Path.Combine(AppContext.BaseDirectory, $"{Guid.NewGuid():N}.db");
    }

    [Fact]
    public void Execute_AwardsConfirmedViewersOncePerCycle()
    {
        var repository = CreateRepository();
        var action = new RefreshCommunityViewersAction(repository, new WatchtimeCalculator(new RewardSettings()));
        var snapshot = new[]
        {
            new CommunityViewerDto { Username = "viewerone", DisplayName = "ViewerOne" },
            new CommunityViewerDto { Username = "viewertwo", DisplayName = "ViewerTwo" }
        };
        var refreshTimestamp = new DateTimeOffset(2026, 4, 23, 20, 0, 0, TimeSpan.Zero);

        var firstResult = action.Execute(refreshTimestamp, snapshot);
        var secondResult = action.Execute(refreshTimestamp, snapshot);

        Assert.Equal(2, firstResult.RewardedViewerCount);
        Assert.Equal(0, secondResult.RewardedViewerCount);
        Assert.Equal(10m, repository.GetViewerBalance("viewerone"));
        Assert.Equal(10m, repository.GetViewerBalance("viewertwo"));
    }

    [Fact]
    public void Execute_AwardsChatOnlyViewerForBoundaryThenExpiresItWhenStillMissing()
    {
        var repository = CreateRepository();
        repository.StoreChatPresence(new ViewerPresenceRecord
        {
            Identity = new ViewerIdentity
            {
                Username = "lateviewer",
                NormalizedUsername = "lateviewer",
                DisplayName = "LateViewer"
            },
            ActiveSinceUtc = new DateTimeOffset(2026, 4, 23, 19, 58, 0, TimeSpan.Zero),
            LastSeenUtc = new DateTimeOffset(2026, 4, 23, 19, 59, 0, TimeSpan.Zero),
            PresenceSource = PresenceSource.ChatFallback,
            SubscriberTier = TwitchSubscriberTier.None,
            PresenceExpiresAtUtc = new DateTimeOffset(2026, 4, 23, 20, 0, 0, TimeSpan.Zero)
        });
        var action = new RefreshCommunityViewersAction(repository, new WatchtimeCalculator(new RewardSettings()));

        var result = action.Execute(new DateTimeOffset(2026, 4, 23, 20, 0, 0, TimeSpan.Zero), Array.Empty<CommunityViewerDto>());

        Assert.Equal(1, result.RewardedViewerCount);
        Assert.Equal(1, result.ExpiredViewerCount);
        Assert.Empty(repository.GetActivePresence());
        Assert.Equal(10m, repository.GetViewerBalance("lateviewer"));
    }

    [Fact]
    public void Execute_CollapsesDuplicateUsernamesWithinOneSnapshot()
    {
        var repository = CreateRepository();
        var action = new RefreshCommunityViewersAction(repository, new WatchtimeCalculator(new RewardSettings()));

        var result = action.Execute(
            new DateTimeOffset(2026, 4, 23, 20, 0, 0, TimeSpan.Zero),
            new[]
            {
                new CommunityViewerDto { Username = "duplicated" },
                new CommunityViewerDto { Username = "Duplicated" }
            });

        Assert.Equal(1, result.RewardedViewerCount);
        Assert.Equal(10m, repository.GetViewerBalance("duplicated"));
    }

    [Fact]
    public void Execute_AppliesSubscriberTierMultipliersFromTheSnapshot()
    {
        var repository = CreateRepository();
        var action = new RefreshCommunityViewersAction(repository, new WatchtimeCalculator(new RewardSettings()));

        var result = action.Execute(
            new DateTimeOffset(2026, 4, 23, 20, 0, 0, TimeSpan.Zero),
            new[]
            {
                new CommunityViewerDto { Username = "tierone", SubscriberTier = TwitchSubscriberTier.Tier1 },
                new CommunityViewerDto { Username = "tierthree", SubscriberTier = TwitchSubscriberTier.Tier3 }
            });

        Assert.Equal(2, result.RewardedViewerCount);
        Assert.Equal(15m, repository.GetViewerBalance("tierone"));
        Assert.Equal(30m, repository.GetViewerBalance("tierthree"));
    }

    [Fact]
    public void Execute_AwardsWatchStreakDuringActiveStreamWithoutChangingTheRefreshMessage()
    {
        var viewerRepository = CreateRepository();
        var watchStreakRepository = CreateWatchStreakRepository();
        var startAction = new StartStreamAction(watchStreakRepository);
        var action = new RefreshCommunityViewersAction(
            viewerRepository,
            new WatchtimeCalculator(new RewardSettings { BasePointsPerInterval = 0m }),
            new WatchStreakService(watchStreakRepository, viewerRepository));
        var viewer = CreateViewerIdentity("viewer-id-refresh", "viewerrefresh", "ViewerRefresh");
        var startedAt = new DateTimeOffset(2026, 4, 25, 18, 0, 0, TimeSpan.Zero);

        startAction.Execute(new StreamLifecycleCommandDto { OccurredAtUtc = startedAt });

        var result = action.Execute(
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

        Assert.Equal("Refresh cycle applied.", result.Message);
        Assert.Equal(100m, viewerRepository.GetViewerBalance(viewer));
        Assert.Equal(1, watchStreakRepository.GetViewerStreak(viewer)?.CurrentStreak);
    }

    [Fact]
    public void Execute_DoesNotClearExistingTwitchUserIdWhenCommunitySnapshotOmitsIt()
    {
        var repository = CreateRepository();
        repository.StoreChatPresence(new ViewerPresenceRecord
        {
            Identity = new ViewerIdentity
            {
                TwitchUserId = "viewer-id-keep",
                Username = "viewerkeep",
                NormalizedUsername = "viewerkeep",
                DisplayName = "ViewerKeep"
            },
            ActiveSinceUtc = new DateTimeOffset(2026, 4, 25, 17, 58, 0, TimeSpan.Zero),
            LastSeenUtc = new DateTimeOffset(2026, 4, 25, 17, 59, 0, TimeSpan.Zero),
            PresenceSource = PresenceSource.ChatFallback,
            SubscriberTier = TwitchSubscriberTier.None,
            PresenceExpiresAtUtc = new DateTimeOffset(2026, 4, 25, 18, 0, 0, TimeSpan.Zero)
        });
        var action = new RefreshCommunityViewersAction(repository, new WatchtimeCalculator(new RewardSettings()));
        var refreshTimestamp = new DateTimeOffset(2026, 4, 25, 18, 0, 0, TimeSpan.Zero);

        var result = action.Execute(
            refreshTimestamp,
            new[]
            {
                new CommunityViewerDto
                {
                    Username = "viewerkeep",
                    DisplayName = "ViewerKeep"
                }
            });

        Assert.True(result.Success);
        Assert.Equal("viewer-id-keep", repository.GetActivePresence().Single().Identity.TwitchUserId);
        Assert.Equal(10m, repository.GetViewerBalance(new ViewerIdentity
        {
            TwitchUserId = "viewer-id-keep",
            Username = "viewerkeep",
            NormalizedUsername = "viewerkeep",
            DisplayName = "ViewerKeep"
        }));
    }

    [Fact]
    public void Execute_DuringActiveStreamKeepsSubscriberMultipliersAndAwardsOnlyOneStreakPerViewer()
    {
        var viewerRepository = CreateRepository();
        var watchStreakRepository = CreateWatchStreakRepository();
        var startAction = new StartStreamAction(watchStreakRepository);
        var action = new RefreshCommunityViewersAction(
            viewerRepository,
            new WatchtimeCalculator(new RewardSettings()),
            new WatchStreakService(watchStreakRepository, viewerRepository));
        var tierOneViewer = CreateViewerIdentity("viewer-id-tier1", "tieroneactive", "TierOneActive");
        var tierThreeViewer = CreateViewerIdentity("viewer-id-tier3", "tierthreeactive", "TierThreeActive");
        var startedAt = new DateTimeOffset(2026, 4, 25, 18, 0, 0, TimeSpan.Zero);
        var snapshot = new[]
        {
            new CommunityViewerDto
            {
                TwitchUserId = tierOneViewer.TwitchUserId,
                Username = tierOneViewer.Username,
                DisplayName = tierOneViewer.DisplayName,
                SubscriberTier = TwitchSubscriberTier.Tier1
            },
            new CommunityViewerDto
            {
                TwitchUserId = tierThreeViewer.TwitchUserId,
                Username = tierThreeViewer.Username,
                DisplayName = tierThreeViewer.DisplayName,
                SubscriberTier = TwitchSubscriberTier.Tier3
            }
        };

        startAction.Execute(new StreamLifecycleCommandDto { OccurredAtUtc = startedAt });

        var firstResult = action.Execute(startedAt.AddMinutes(5), snapshot);
        var secondResult = action.Execute(startedAt.AddMinutes(10), snapshot);

        Assert.Equal("Refresh cycle applied.", firstResult.Message);
        Assert.Equal("Refresh cycle applied.", secondResult.Message);
        Assert.Equal(2, firstResult.RewardedViewerCount);
        Assert.Equal(45m, firstResult.TotalPointsAwarded);
        Assert.Equal(2, secondResult.RewardedViewerCount);
        Assert.Equal(45m, secondResult.TotalPointsAwarded);
        Assert.Equal(130m, viewerRepository.GetViewerBalance(tierOneViewer));
        Assert.Equal(160m, viewerRepository.GetViewerBalance(tierThreeViewer));
        Assert.Equal(1, watchStreakRepository.GetViewerStreak(tierOneViewer)?.CurrentStreak);
        Assert.Equal(1, watchStreakRepository.GetViewerStreak(tierThreeViewer)?.CurrentStreak);
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
