using System;

namespace TwitchHeists.Core.Options;

public sealed class HeistSettings
{
    public TimeSpan JoinWindow { get; set; } = TimeSpan.FromMinutes(2);

    public decimal MinimumSuccessChance { get; set; } = 0.40m;

    public decimal MaximumSuccessChance { get; set; } = 0.75m;

    public int MaximumWinnerCount { get; set; } = 5;

    public decimal SuccessfulPotMultiplier { get; set; } = 2.0m;
}
