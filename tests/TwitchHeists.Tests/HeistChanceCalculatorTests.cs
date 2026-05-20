using TwitchHeists.Core.Options;
using TwitchHeists.Core.Services;

namespace TwitchHeists.Tests;

public sealed class HeistChanceCalculatorTests
{
    [Fact]
    public void CalculateSuccessChance_IncreasesWithMoreParticipantsUntilItHitsTheMaximum()
    {
        var calculator = new HeistChanceCalculator(
            new HeistSettings
            {
                MinimumSuccessChance = 0.40m,
                MaximumSuccessChance = 0.75m
            });

        var soloChance = calculator.CalculateSuccessChance(1_000m, 1);
        var duoChance = calculator.CalculateSuccessChance(1_000m, 2);
        var largeCrewChance = calculator.CalculateSuccessChance(1_000m, 10);

        Assert.True(duoChance > soloChance);
        Assert.Equal(0.75m, largeCrewChance);
    }

    [Fact]
    public void CalculateSuccessChance_DecreasesWhenTheTotalStakeGetsLarger()
    {
        var calculator = new HeistChanceCalculator(
            new HeistSettings
            {
                MinimumSuccessChance = 0.40m,
                MaximumSuccessChance = 0.75m
            });

        var lowerStakeChance = calculator.CalculateSuccessChance(1_000m, 3);
        var higherStakeChance = calculator.CalculateSuccessChance(50_000m, 3);

        Assert.True(higherStakeChance < lowerStakeChance);
    }
}
