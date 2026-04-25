namespace TwitchHeists.Tests.Performance;

public sealed class MixedLoadScenarioResult
{
    public MixedLoadScenarioResult(
        int viewerCount,
        int refreshRewardedViewerCount,
        int chatEventCount,
        int heistJoinCount,
        int activePresenceCount,
        int heistParticipantCount,
        decimal totalTrackedBalance,
        TimeSpan refreshElapsed,
        TimeSpan chatBurstElapsed,
        TimeSpan heistStartElapsed,
        TimeSpan heistJoinElapsed,
        TimeSpan heistResolveElapsed,
        TimeSpan totalElapsed)
    {
        ViewerCount = viewerCount;
        RefreshRewardedViewerCount = refreshRewardedViewerCount;
        ChatEventCount = chatEventCount;
        HeistJoinCount = heistJoinCount;
        ActivePresenceCount = activePresenceCount;
        HeistParticipantCount = heistParticipantCount;
        TotalTrackedBalance = totalTrackedBalance;
        RefreshElapsed = refreshElapsed;
        ChatBurstElapsed = chatBurstElapsed;
        HeistStartElapsed = heistStartElapsed;
        HeistJoinElapsed = heistJoinElapsed;
        HeistResolveElapsed = heistResolveElapsed;
        TotalElapsed = totalElapsed;
    }

    public int ViewerCount { get; }

    public int RefreshRewardedViewerCount { get; }

    public int ChatEventCount { get; }

    public int HeistJoinCount { get; }

    public int ActivePresenceCount { get; }

    public int HeistParticipantCount { get; }

    public decimal TotalTrackedBalance { get; }

    public TimeSpan RefreshElapsed { get; }

    public TimeSpan ChatBurstElapsed { get; }

    public TimeSpan HeistStartElapsed { get; }

    public TimeSpan HeistJoinElapsed { get; }

    public TimeSpan HeistResolveElapsed { get; }

    public TimeSpan TotalElapsed { get; }
}
