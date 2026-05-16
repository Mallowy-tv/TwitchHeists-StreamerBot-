using TwitchHeists.Core.Options;
using TwitchHeists.Core.Services;
using TwitchHeists.Data.Sqlite.Repositories;
using TwitchHeists.StreamerBot.Contracts;

namespace TwitchHeists.StreamerBot.Services;

public sealed class StartRaffleAction
{
    private readonly RaffleRepository raffleRepository;
    private readonly RaffleSettings raffleSettings;

    public StartRaffleAction(RaffleRepository raffleRepository, RaffleSettings raffleSettings)
    {
        this.raffleRepository = raffleRepository;
        this.raffleSettings = raffleSettings;
    }

    public ActionResponseDto Execute(RaffleCommandDto command)
    {
        try
        {
            var resolvedWinnerPoints = ResolveWinnerPoints(command);
            raffleRepository.StartRound(
                command.IsBroadcaster,
                command.SingleWinner,
                resolvedWinnerPoints,
                command.OccurredAtUtc,
                command.OccurredAtUtc.Add(raffleSettings.JoinWindow));
        }
        catch (InvalidOperationException exception)
        {
            return Failure(exception.Message);
        }

        var winnerPoints = ResolveWinnerPoints(command);
        var mode = command.SingleWinner ? "single-winner" : "multi-winner";
        return new ActionResponseDto
        {
            Success = true,
            Message = $"Started a {mode} raffle for {PointValueNormalizer.Format(winnerPoints)} points per winner. Drawing in {FormatDuration(raffleSettings.JoinWindow)}. Use !rjoin to enter for a chance to win {PointValueNormalizer.Format(winnerPoints)} points."
        };
    }

    private decimal ResolveWinnerPoints(RaffleCommandDto command)
    {
        if (command.WinnerPoints.HasValue)
        {
            var explicitWinnerPoints = PointValueNormalizer.Normalize(command.WinnerPoints.Value);
            if (explicitWinnerPoints > 0)
            {
                return explicitWinnerPoints;
            }
        }

        return PointValueNormalizer.NormalizeNonNegative(raffleSettings.WinnerPoints);
    }

    private static string FormatDuration(TimeSpan duration)
    {
        if (duration.TotalSeconds < 60)
        {
            return $"{(int)Math.Round(duration.TotalSeconds, MidpointRounding.AwayFromZero)} seconds";
        }

        if (duration.TotalMinutes == 1)
        {
            return "1 minute";
        }

        return $"{(int)Math.Round(duration.TotalMinutes, MidpointRounding.AwayFromZero)} minutes";
    }

    private static ActionResponseDto Failure(string message)
    {
        return new ActionResponseDto
        {
            Success = false,
            Message = message
        };
    }
}
