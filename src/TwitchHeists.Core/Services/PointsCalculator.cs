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
        return rewardSettings.BasePointsPerInterval * rewardSettings.GetMultiplier(subscriberTier);
    }
}
