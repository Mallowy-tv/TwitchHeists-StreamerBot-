using System;
using TwitchHeists.Core.Models;

namespace TwitchHeists.Core.Options;

public sealed class RewardSettings
{
    public TimeSpan RewardInterval { get; set; } = TimeSpan.FromMinutes(5);

    public decimal BasePointsPerInterval { get; set; } = 10m;

    public decimal Tier1Multiplier { get; set; } = 1.5m;

    public decimal Tier2Multiplier { get; set; } = 2.0m;

    public decimal Tier3Multiplier { get; set; } = 3.0m;

    public decimal GetMultiplier(TwitchSubscriberTier subscriberTier)
    {
        return subscriberTier switch
        {
            TwitchSubscriberTier.Tier1 => Tier1Multiplier,
            TwitchSubscriberTier.Tier2 => Tier2Multiplier,
            TwitchSubscriberTier.Tier3 => Tier3Multiplier,
            _ => 1.0m
        };
    }
}
