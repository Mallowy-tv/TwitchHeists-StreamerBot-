using TwitchHeists.Core.Services;

namespace TwitchHeists.Tests;

public sealed class RaffleWinnerCalculatorTests
{
    private readonly RaffleWinnerCalculator calculator = new();

    [Theory]
    [InlineData(1, 1)]
    [InlineData(2, 1)]
    [InlineData(10, 5)]
    [InlineData(11, 2)]
    [InlineData(20, 5)]
    [InlineData(21, 4)]
    [InlineData(50, 10)]
    [InlineData(51, 6)]
    [InlineData(200, 25)]
    [InlineData(201, 10)]
    [InlineData(240, 12)]
    public void ResolveWinnerCount_UsesConfiguredParticipantBands(int participants, int expectedWinners)
    {
        var result = calculator.ResolveWinnerCount(participants);

        Assert.Equal(expectedWinners, result);
    }

    [Fact]
    public void ResolveWinnerCount_ThrowsWhenParticipantsAreZero()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => calculator.ResolveWinnerCount(0));
    }
}
