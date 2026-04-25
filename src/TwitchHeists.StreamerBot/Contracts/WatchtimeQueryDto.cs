namespace TwitchHeists.StreamerBot.Contracts;

public sealed class WatchtimeQueryDto
{
    public string RequesterUsername { get; set; } = string.Empty;

    public string? RequesterDisplayName { get; set; }

    public string? TargetUsername { get; set; }

    public string? TargetDisplayName { get; set; }

    public DateTimeOffset OccurredAtUtc { get; set; }
}
