namespace TwitchHeists.Core.Models;

public enum HeistRoundState
{
    Open = 0,
    Locked = 1,
    ResolvedSuccess = 2,
    ResolvedFailure = 3,
    Cancelled = 4,
    InsufficientCrew = 5
}
