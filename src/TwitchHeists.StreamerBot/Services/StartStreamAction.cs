using TwitchHeists.Data.Sqlite.Repositories;
using TwitchHeists.StreamerBot.Contracts;

namespace TwitchHeists.StreamerBot.Services;

public sealed class StartStreamAction
{
    private readonly WatchStreakRepository watchStreakRepository;

    public StartStreamAction(WatchStreakRepository watchStreakRepository)
    {
        this.watchStreakRepository = watchStreakRepository;
    }

    public ActionResponseDto Execute(StreamLifecycleCommandDto command)
    {
        watchStreakRepository.StartStream(command.OccurredAtUtc);

        return new ActionResponseDto
        {
            Success = true,
            Message = $"Stream marked live at {command.OccurredAtUtc:O}."
        };
    }
}
