namespace TwitchHeists.StreamerBot.Contracts;

public sealed class RaffleCommandDto
{
    public string? SourceTwitchUserId { get; set; }

    public string SourceUsername { get; set; } = string.Empty;

    public string? SourceDisplayName { get; set; }

    public bool IsBroadcaster { get; set; }

    public bool SingleWinner { get; set; }

    public decimal? WinnerPoints { get; set; }

    public DateTimeOffset OccurredAtUtc { get; set; }
}
