using TwitchHeists.Core.Models;
using TwitchHeists.Core.Options;

namespace TwitchHeists.Core.Services;

public sealed class PointsCalculator
{
    private readonly RewardSettings rewardSettings;

    public PointsCalculator(RewardSettings rewardSettings)
    {
        this.rewardSettings = rewardSettings;
    }

    public decimal CalculateAward(TwitchSubscriberTier subscriberTier)
    {
        return PointValueNormalizer.Normalize(rewardSettings.BasePointsPerInterval * rewardSettings.GetMultiplier(subscriberTier));
    }
}
