using TwitchHeists.Core.Models;
using TwitchHeists.Core.Options;

namespace TwitchHeists.Core.Services;

public sealed class WatchtimeCalculator
{
    private readonly RewardSettings rewardSettings;
    private readonly PointsCalculator pointsCalculator;

    public WatchtimeCalculator(RewardSettings rewardSettings)
    {
        this.rewardSettings = rewardSettings;
        pointsCalculator = new PointsCalculator(rewardSettings);
    }

    public WatchtimeCycleResult CalculateCycle(
        DateTimeOffset cycleTimestampUtc,
        IEnumerable<ViewerIdentity> confirmedViewers,
        IEnumerable<ViewerPresenceRecord> knownPresence)
    {
        var confirmedPresence = confirmedViewers.Select(
            viewer => new ViewerPresenceRecord
            {
                Identity = viewer,
                ActiveSinceUtc = cycleTimestampUtc,
                LastSeenUtc = cycleTimestampUtc,
                PresenceSource = PresenceSource.CommunityRefresh,
                SubscriberTier = TwitchSubscriberTier.None
            });

        return CalculateCycle(cycleTimestampUtc, confirmedPresence, knownPresence);
    }

    public WatchtimeCycleResult CalculateCycle(
        DateTimeOffset cycleTimestampUtc,
        IEnumerable<ViewerPresenceRecord> confirmedPresence,
        IEnumerable<ViewerPresenceRecord> knownPresence)
    {
        var confirmedPresenceMap = confirmedPresence
            .GroupBy(record => record.Identity.NormalizedUsername, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.Last(), StringComparer.OrdinalIgnoreCase);

        var knownPresenceMap = knownPresence
            .GroupBy(record => record.Identity.NormalizedUsername, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.Last(), StringComparer.OrdinalIgnoreCase);

        var rewards = new List<ViewerRewardResult>();
        var activePresence = new List<ViewerPresenceRecord>();
        var expiredPresence = new List<ViewerPresenceRecord>();

        foreach (var confirmedRecord in confirmedPresenceMap.Values)
        {
            knownPresenceMap.TryGetValue(confirmedRecord.Identity.NormalizedUsername, out var existingRecord);

            var resolvedPresence = existingRecord ?? new ViewerPresenceRecord
            {
                Identity = confirmedRecord.Identity,
                ActiveSinceUtc = confirmedRecord.ActiveSinceUtc == default ? cycleTimestampUtc : confirmedRecord.ActiveSinceUtc
            };

            resolvedPresence.Identity = confirmedRecord.Identity;
            resolvedPresence.ActiveSinceUtc = existingRecord?.ActiveSinceUtc ?? resolvedPresence.ActiveSinceUtc;
            resolvedPresence.LastSeenUtc = cycleTimestampUtc;
            resolvedPresence.LastConfirmedRefreshUtc = cycleTimestampUtc;
            resolvedPresence.PresenceSource = PresenceSource.CommunityRefresh;
            resolvedPresence.SubscriberTier = confirmedRecord.SubscriberTier;
            resolvedPresence.PresenceExpiresAtUtc = null;

            if (resolvedPresence.LastRewardedCycleUtc != cycleTimestampUtc)
            {
                rewards.Add(CreateReward(resolvedPresence, cycleTimestampUtc));
                resolvedPresence.LastRewardedCycleUtc = cycleTimestampUtc;
            }

            activePresence.Add(resolvedPresence);
        }

        foreach (var presenceRecord in knownPresenceMap.Values)
        {
            if (confirmedPresenceMap.ContainsKey(presenceRecord.Identity.NormalizedUsername))
            {
                continue;
            }

            var shouldRewardChatFallback =
                presenceRecord.PresenceSource == PresenceSource.ChatFallback &&
                presenceRecord.PresenceExpiresAtUtc.HasValue &&
                presenceRecord.PresenceExpiresAtUtc.Value <= cycleTimestampUtc &&
                presenceRecord.LastRewardedCycleUtc != cycleTimestampUtc;

            if (shouldRewardChatFallback)
            {
                rewards.Add(CreateReward(presenceRecord, cycleTimestampUtc));
                presenceRecord.LastRewardedCycleUtc = cycleTimestampUtc;
            }

            var isExpired =
                presenceRecord.PresenceSource == PresenceSource.ChatFallback
                    ? !presenceRecord.PresenceExpiresAtUtc.HasValue || presenceRecord.PresenceExpiresAtUtc.Value <= cycleTimestampUtc
                    : true;

            if (isExpired)
            {
                expiredPresence.Add(presenceRecord);
                continue;
            }

            activePresence.Add(presenceRecord);
        }

        return new WatchtimeCycleResult
        {
            Rewards = rewards,
            ActivePresence = activePresence,
            ExpiredPresence = expiredPresence
        };
    }

    private ViewerRewardResult CreateReward(ViewerPresenceRecord presenceRecord, DateTimeOffset cycleTimestampUtc)
    {
        return new ViewerRewardResult
        {
            Identity = presenceRecord.Identity,
            WatchMinutesAwarded = (int)rewardSettings.RewardInterval.TotalMinutes,
            PointsAwarded = pointsCalculator.CalculateAward(presenceRecord.SubscriberTier),
            MultiplierApplied = rewardSettings.GetMultiplier(presenceRecord.SubscriberTier),
            RewardedAtUtc = cycleTimestampUtc
        };
    }
}
