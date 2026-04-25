namespace TwitchHeists.StreamerBot.Contracts;

public sealed class PointsCommandDto
{
    public string SourceUsername { get; set; } = string.Empty;

    public string? SourceDisplayName { get; set; }

    public string TargetUsername { get; set; } = string.Empty;

    public string? TargetDisplayName { get; set; }

    public decimal Amount { get; set; }

    public DateTimeOffset OccurredAtUtc { get; set; }
}
