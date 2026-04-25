using TwitchHeists.Tests.Performance;
using Xunit.Abstractions;

namespace TwitchHeists.Tests;

public sealed class MixedLoadPerformanceTests
{
    private readonly ITestOutputHelper output;

    public MixedLoadPerformanceTests(ITestOutputHelper output)
    {
        this.output = output;
    }

    [Fact]
    public void Execute_RecordsABaselineForAThousandViewerMixedStream()
    {
        using var runner = new MixedLoadScenarioRunner();

        var result = runner.Execute();

        output.WriteLine($"Mixed load viewers: {result.ViewerCount}");
        output.WriteLine($"Refresh rewarded viewers: {result.RefreshRewardedViewerCount}");
        output.WriteLine($"Chat events: {result.ChatEventCount}");
        output.WriteLine($"Heist joins: {result.HeistJoinCount}");
        output.WriteLine($"Active presence count: {result.ActivePresenceCount}");
        output.WriteLine($"Heist participant count: {result.HeistParticipantCount}");
        output.WriteLine($"Refresh elapsed: {result.RefreshElapsed.TotalMilliseconds:F2} ms");
        output.WriteLine($"Chat burst elapsed: {result.ChatBurstElapsed.TotalMilliseconds:F2} ms");
        output.WriteLine($"Heist start elapsed: {result.HeistStartElapsed.TotalMilliseconds:F2} ms");
        output.WriteLine($"Heist join elapsed: {result.HeistJoinElapsed.TotalMilliseconds:F2} ms");
        output.WriteLine($"Heist resolve elapsed: {result.HeistResolveElapsed.TotalMilliseconds:F2} ms");
        output.WriteLine($"Total elapsed: {result.TotalElapsed.TotalMilliseconds:F2} ms");
        output.WriteLine($"Total tracked balance: {result.TotalTrackedBalance}");

        Assert.Equal(1_000, result.ViewerCount);
        Assert.Equal(result.ViewerCount, result.RefreshRewardedViewerCount);
        Assert.Equal(result.ViewerCount * 2, result.ChatEventCount);
        Assert.Equal(result.ViewerCount, result.ActivePresenceCount);
        Assert.Equal(result.HeistJoinCount + 1, result.HeistParticipantCount);
        Assert.Equal(
            MixedLoadScenarioRunner.CalculateExpectedTotalTrackedBalance(result.ViewerCount, result.HeistJoinCount),
            result.TotalTrackedBalance);
    }
}
