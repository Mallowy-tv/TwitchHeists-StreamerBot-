namespace TwitchHeists.StreamerBot.Bridge.Models;

public sealed class BridgeRaffleCommand
{
    public string? SourceTwitchUserId { get; set; }

    public string SourceUsername { get; set; } = string.Empty;

    public string? SourceDisplayName { get; set; }

    public bool IsBroadcaster { get; set; }

    public decimal? WinnerPoints { get; set; }

    public DateTimeOffset OccurredAtUtc { get; set; }
}
