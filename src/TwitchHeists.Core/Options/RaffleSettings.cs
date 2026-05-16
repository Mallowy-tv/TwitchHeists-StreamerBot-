namespace TwitchHeists.Core.Options;

public sealed class RaffleSettings
{
    public TimeSpan JoinWindow { get; set; } = TimeSpan.FromMinutes(2);

    public TimeSpan OneMinuteReminderThreshold { get; set; } = TimeSpan.FromMinutes(1);

    public TimeSpan ThirtySecondReminderThreshold { get; set; } = TimeSpan.FromSeconds(30);

    public TimeSpan TenSecondReminderThreshold { get; set; } = TimeSpan.FromSeconds(10);

    public decimal WinnerPoints { get; set; } = 5_000m;

    public decimal ModeratorPointsLimit { get; set; } = 5_000m;
}
