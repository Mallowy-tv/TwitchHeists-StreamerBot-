using TwitchHeists.Core.Models;
using TwitchHeists.Core.Options;
using TwitchHeists.Core.Services;

namespace TwitchHeists.Tests;

public sealed class WatchtimeCalculatorTests
{
    private static readonly DateTimeOffset CycleTimestampUtc = new(2026, 4, 23, 20, 0, 0, TimeSpan.Zero);

    [Fact]
    public void CalculateCycle_AwardsFiveMinutesToConfirmedViewer()
    {
        var calculator = new WatchtimeCalculator(new RewardSettings());
        var confirmedViewers = new[]
        {
            CreateViewer("viewerone")
        };

        var result = calculator.CalculateCycle(CycleTimestampUtc, confirmedViewers, Array.Empty<ViewerPresenceRecord>());

        var reward = Assert.Single(result.Rewards);
        Assert.Equal("viewerone", reward.Identity.NormalizedUsername);
        Assert.Equal(5, reward.WatchMinutesAwarded);
        Assert.Equal(500m, reward.PointsAwarded);
        Assert.Empty(result.ExpiredPresence);
    }

    [Fact]
    public void CalculateCycle_AwardsChatOnlyViewerOnceThenExpiresWhenStillMissing()
    {
        var calculator = new WatchtimeCalculator(new RewardSettings());
        var chatOnlyPresence = new[]
        {
            new ViewerPresenceRecord
            {
                Identity = CreateViewer("lateviewer"),
                ActiveSinceUtc = CycleTimestampUtc.AddMinutes(-2),
                LastSeenUtc = CycleTimestampUtc.AddMinutes(-1),
                PresenceSource = PresenceSource.ChatFallback,
                SubscriberTier = TwitchSubscriberTier.None,
                PresenceExpiresAtUtc = CycleTimestampUtc
            }
        };

        var result = calculator.CalculateCycle(CycleTimestampUtc, Array.Empty<ViewerIdentity>(), chatOnlyPresence);

        var reward = Assert.Single(result.Rewards);
        Assert.Equal("lateviewer", reward.Identity.NormalizedUsername);
        Assert.Equal(5, reward.WatchMinutesAwarded);
        Assert.Single(result.ExpiredPresence);
        Assert.Empty(result.ActivePresence);
    }

    [Fact]
    public void CalculateCycle_DoesNotDoubleAwardWhenCycleWasAlreadyProcessed()
    {
        var calculator = new WatchtimeCalculator(new RewardSettings());
        var confirmedViewers = new[]
        {
            CreateViewer("steadyviewer")
        };
        var knownPresence = new[]
        {
            new ViewerPresenceRecord
            {
                Identity = CreateViewer("steadyviewer"),
                ActiveSinceUtc = CycleTimestampUtc.AddMinutes(-10),
                LastSeenUtc = CycleTimestampUtc,
                PresenceSource = PresenceSource.CommunityRefresh,
                SubscriberTier = TwitchSubscriberTier.None,
                LastConfirmedRefreshUtc = CycleTimestampUtc,
                LastRewardedCycleUtc = CycleTimestampUtc
            }
        };

        var result = calculator.CalculateCycle(CycleTimestampUtc, confirmedViewers, knownPresence);

        Assert.Empty(result.Rewards);
        Assert.Single(result.ActivePresence);
        Assert.Empty(result.ExpiredPresence);
    }

    private static ViewerIdentity CreateViewer(string normalizedUsername)
    {
        return new ViewerIdentity
        {
            Username = normalizedUsername,
            NormalizedUsername = normalizedUsername,
            DisplayName = normalizedUsername
        };
    }
}
