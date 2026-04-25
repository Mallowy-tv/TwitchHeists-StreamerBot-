namespace TwitchHeists.StreamerBot.Contracts;

public sealed class ActionResponseDto
{
    public bool Success { get; set; }

    public string Message { get; set; } = string.Empty;

    public int RewardedViewerCount { get; set; }

    public int ExpiredViewerCount { get; set; }

    public decimal TotalPointsAwarded { get; set; }
}
