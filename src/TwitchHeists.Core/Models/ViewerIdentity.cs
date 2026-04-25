namespace TwitchHeists.Core.Models;

public sealed class ViewerIdentity
{
    public string? TwitchUserId { get; set; }

    public string Username { get; set; } = string.Empty;

    public string NormalizedUsername { get; set; } = string.Empty;

    public string? DisplayName { get; set; }
}
