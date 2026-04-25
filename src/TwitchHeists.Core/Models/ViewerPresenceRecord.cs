using System;

namespace TwitchHeists.Core.Models;

public sealed class ViewerPresenceRecord
{
    public ViewerIdentity Identity { get; set; } = new ViewerIdentity();

    public DateTimeOffset ActiveSinceUtc { get; set; }

    public DateTimeOffset LastSeenUtc { get; set; }

    public PresenceSource PresenceSource { get; set; }

    public TwitchSubscriberTier SubscriberTier { get; set; }

    public DateTimeOffset? LastConfirmedRefreshUtc { get; set; }

    public DateTimeOffset? LastRewardedCycleUtc { get; set; }

    public DateTimeOffset? PresenceExpiresAtUtc { get; set; }
}
