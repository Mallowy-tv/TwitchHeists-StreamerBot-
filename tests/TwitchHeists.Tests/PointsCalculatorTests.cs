using TwitchHeists.Core.Models;
using TwitchHeists.Core.Options;
using TwitchHeists.Core.Services;

namespace TwitchHeists.Tests;

public sealed class PointsCalculatorTests
{
    [Theory]
    [InlineData(TwitchSubscriberTier.None, 500)]
    [InlineData(TwitchSubscriberTier.Tier1, 750)]
    [InlineData(TwitchSubscriberTier.Tier2, 1000)]
    [InlineData(TwitchSubscriberTier.Tier3, 1500)]
    public void CalculateAward_AppliesConfiguredTierMultiplier(TwitchSubscriberTier subscriberTier, decimal expectedPoints)
    {
        var calculator = new PointsCalculator(new RewardSettings());

        var awardedPoints = calculator.CalculateAward(subscriberTier);

        Assert.Equal(expectedPoints, awardedPoints);
    }
}
