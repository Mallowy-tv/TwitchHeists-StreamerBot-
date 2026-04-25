using TwitchHeists.Core.Models;
using TwitchHeists.Core.Options;
using TwitchHeists.Core.Services;

namespace TwitchHeists.Tests;

public sealed class PointsCalculatorTests
{
    [Theory]
    [InlineData(TwitchSubscriberTier.None, 10)]
    [InlineData(TwitchSubscriberTier.Tier1, 15)]
    [InlineData(TwitchSubscriberTier.Tier2, 20)]
    [InlineData(TwitchSubscriberTier.Tier3, 30)]
    public void CalculateAward_AppliesConfiguredTierMultiplier(TwitchSubscriberTier subscriberTier, decimal expectedPoints)
    {
        var calculator = new PointsCalculator(new RewardSettings());

        var awardedPoints = calculator.CalculateAward(subscriberTier);

        Assert.Equal(expectedPoints, awardedPoints);
    }
}
