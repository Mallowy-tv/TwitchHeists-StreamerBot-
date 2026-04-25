namespace TwitchHeists.StreamerBot.Bridge.Models;

public sealed class BridgeHeistCommand
{
    public string? TwitchUserId { get; set; }

    public string Username { get; set; } = string.Empty;

    public string? DisplayName { get; set; }

    public decimal StakeAmount { get; set; }

    public DateTimeOffset OccurredAtUtc { get; set; }
}
