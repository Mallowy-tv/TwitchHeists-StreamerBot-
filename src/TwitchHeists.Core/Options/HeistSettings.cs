using System;
using System.Collections.Generic;

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

    public int MinimumPlayers { get; set; } = 2;

    public int MaximumWinnerCount { get; set; } = 5;

    public List<HeistWinnerBand> WinnerBands { get; set; } = CreateDefaultWinnerBands();

    public int MaximumNamedResolutionCallouts { get; set; } = 2;

    public decimal SuccessfulPotMultiplier { get; set; } = 2.0m;

    private static List<HeistWinnerBand> CreateDefaultWinnerBands()
    {
        return new List<HeistWinnerBand>
        {
            new(2, 2, 1, 2),
            new(3, 3, 1, 3),
            new(4, 4, 1, 4),
            new(5, 5, 1, 5),
            new(6, 6, 3, 6),
            new(7, 10, 3, 7),
            new(11, 15, 4, 8),
            new(16, 20, 5, 9),
            new(21, 25, 6, 10),
            new(26, 30, 7, 11),
            new(31, 35, 8, 12),
            new(36, 40, 9, 13),
            new(41, 45, 10, 14),
            new(46, 50, 11, 15),
            new(51, 60, 12, 16),
            new(61, 70, 13, 18),
            new(71, 80, 14, 20),
            new(81, 90, 15, 22),
            new(91, 100, 16, 24),
            new(101, 110, 17, 26),
            new(111, 120, 18, 28),
            new(121, 130, 19, 30),
            new(131, 140, 20, 32),
            new(141, 150, 21, 34)
        };
    }
}
