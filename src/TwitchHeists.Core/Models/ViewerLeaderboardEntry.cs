namespace TwitchHeists.Core.Models;

public sealed class ViewerLeaderboardEntry
{
    public string Username { get; set; } = string.Empty;

    public decimal PointsBalance { get; set; }
}
