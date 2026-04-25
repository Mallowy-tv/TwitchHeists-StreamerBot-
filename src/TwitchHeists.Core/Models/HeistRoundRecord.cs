using System;

namespace TwitchHeists.Core.Models;

public sealed class HeistRoundRecord
{
    public Guid RoundId { get; set; }

    public HeistRoundState State { get; set; }

    public DateTimeOffset StartedAtUtc { get; set; }

    public DateTimeOffset ResolveAtUtc { get; set; }

    public DateTimeOffset? CooldownEndsAtUtc { get; set; }

    public DateTimeOffset? OneMinuteReminderSentAtUtc { get; set; }

    public DateTimeOffset? ThirtySecondReminderSentAtUtc { get; set; }

    public DateTimeOffset? TenSecondReminderSentAtUtc { get; set; }

    public decimal OriginalPot { get; set; }

    public decimal? SuccessChance { get; set; }

    public decimal? ResolvedPot { get; set; }

    public DateTimeOffset? ResolvedAtUtc { get; set; }
}
