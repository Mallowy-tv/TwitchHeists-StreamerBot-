namespace TwitchHeists.StreamerBot.Bridge.Models;

public sealed class BridgeWatchtimeQuery
{
    public string? RequesterTwitchUserId { get; set; }

    public string RequesterUsername { get; set; } = string.Empty;

    public string? RequesterDisplayName { get; set; }

    public string? TargetTwitchUserId { get; set; }

    public string? TargetUsername { get; set; }

    public string? TargetDisplayName { get; set; }

    public DateTimeOffset OccurredAtUtc { get; set; }
}
