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
}
