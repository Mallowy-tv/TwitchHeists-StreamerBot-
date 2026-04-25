namespace TwitchHeists.StreamerBot.Bridge.Models;

public sealed class BridgeCommunityViewer
{
    public string? TwitchUserId { get; set; }

    public string Username { get; set; } = string.Empty;

    public string? DisplayName { get; set; }

    public int SubscriberTier { get; set; }
}
