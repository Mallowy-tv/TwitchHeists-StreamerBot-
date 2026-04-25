using System;

namespace TwitchHeists.Core.Options;

public sealed class HeistSettings
{
    public TimeSpan JoinWindow { get; set; } = TimeSpan.FromMinutes(2);

    public TimeSpan CooldownWindow { get; set; } = TimeSpan.FromMinutes(5);

    public TimeSpan OneMinuteReminderThreshold { get; set; } = TimeSpan.FromMinutes(1);

    public TimeSpan ThirtySecondReminderThreshold { get; set; } = TimeSpan.FromSeconds(30);

    public TimeSpan TenSecondReminderThreshold { get; set; } = TimeSpan.FromSeconds(10);

    public decimal MinimumSuccessChance { get; set; } = 0.40m;

    public decimal MaximumSuccessChance { get; set; } = 0.75m;

    public int MaximumWinnerCount { get; set; } = 5;

    public decimal SuccessfulPotMultiplier { get; set; } = 2.0m;
}
