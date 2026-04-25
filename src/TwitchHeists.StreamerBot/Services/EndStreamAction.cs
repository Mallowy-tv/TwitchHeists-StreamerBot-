using TwitchHeists.Data.Sqlite.Repositories;
using TwitchHeists.StreamerBot.Contracts;

namespace TwitchHeists.StreamerBot.Services;

public sealed class EndStreamAction
{
    private readonly WatchStreakRepository watchStreakRepository;

    public EndStreamAction(WatchStreakRepository watchStreakRepository)
    {
        this.watchStreakRepository = watchStreakRepository;
    }

    public ActionResponseDto Execute(StreamLifecycleCommandDto command)
    {
        watchStreakRepository.EndStream(command.OccurredAtUtc);

        return new ActionResponseDto
        {
            Success = true,
            Message = $"Stream marked offline at {command.OccurredAtUtc:O}."
        };
    }
}
