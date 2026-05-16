namespace TwitchHeists.Core.Models;

public sealed class RaffleRoundRecord
{
    public Guid RoundId { get; set; }

    public RaffleRoundState State { get; set; }

    public DateTimeOffset StartedAtUtc { get; set; }

    public DateTimeOffset ResolveAtUtc { get; set; }

    public bool IsBroadcaster { get; set; }

    public bool SingleWinner { get; set; }

    public decimal WinnerPoints { get; set; }

    public DateTimeOffset? OneMinuteReminderSentAtUtc { get; set; }

    public DateTimeOffset? ThirtySecondReminderSentAtUtc { get; set; }

    public DateTimeOffset? TenSecondReminderSentAtUtc { get; set; }
}
