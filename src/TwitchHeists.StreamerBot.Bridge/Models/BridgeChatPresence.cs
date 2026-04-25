namespace TwitchHeists.StreamerBot.Bridge.Models;

public sealed class BridgeChatPresence
{
    public string? TwitchUserId { get; set; }

    public string Username { get; set; } = string.Empty;

    public string? DisplayName { get; set; }

    public int SubscriberTier { get; set; }

    public DateTimeOffset MessageReceivedAtUtc { get; set; }
}
