using System;

namespace TwitchHeists.Core.Models;

public sealed class ViewerRewardResult
{
    public ViewerIdentity Identity { get; set; } = new ViewerIdentity();

    public int WatchMinutesAwarded { get; set; }

    public decimal PointsAwarded { get; set; }

    public decimal MultiplierApplied { get; set; }

    public decimal UpdatedBalance { get; set; }

    public DateTimeOffset RewardedAtUtc { get; set; }
}
