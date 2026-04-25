using TwitchHeists.Core.Options;

namespace TwitchHeists.Core.Services;

public sealed class HeistChanceCalculator
{
    private readonly HeistSettings heistSettings;

    public HeistChanceCalculator(HeistSettings heistSettings)
    {
        this.heistSettings = heistSettings;
    }

    public decimal CalculateSuccessChance(decimal totalStake, int participantCount)
    {
        var participantPressure = Math.Max(0, participantCount - 1) * 0.03m;
        var stakePressure = totalStake <= 0
            ? 0m
            : Math.Min(0.20m, (decimal)Math.Log10((double)(totalStake / 1_000m) + 1d) * 0.10m);
        var chance = heistSettings.MaximumSuccessChance - participantPressure - stakePressure;

        return Clamp(chance, heistSettings.MinimumSuccessChance, heistSettings.MaximumSuccessChance);
    }

    private static decimal Clamp(decimal value, decimal minimum, decimal maximum)
    {
        if (value < minimum)
        {
            return minimum;
        }

        if (value > maximum)
        {
            return maximum;
        }

        return value;
    }
}
