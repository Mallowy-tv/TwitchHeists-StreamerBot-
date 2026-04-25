using TwitchHeists.Core.Models;

namespace TwitchHeists.StreamerBot.Contracts;

public sealed class CommunityViewerDto
{
    public string? TwitchUserId { get; set; }

    public string Username { get; set; } = string.Empty;

    public string? DisplayName { get; set; }

    public TwitchSubscriberTier SubscriberTier { get; set; }
}
