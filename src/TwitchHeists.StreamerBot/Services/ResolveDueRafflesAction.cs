using TwitchHeists.Core.Options;
using TwitchHeists.Data.Sqlite.Repositories;
using TwitchHeists.StreamerBot.Contracts;

namespace TwitchHeists.StreamerBot.Services;

public sealed class ResolveDueRafflesAction
{
    private readonly RaffleRepository raffleRepository;
    private readonly RunRaffleAction runRaffleAction;
    private readonly RaffleSettings raffleSettings;

    public ResolveDueRafflesAction(
        RaffleRepository raffleRepository,
        RunRaffleAction runRaffleAction,
        RaffleSettings raffleSettings)
    {
        this.raffleRepository = raffleRepository;
        this.runRaffleAction = runRaffleAction;
        this.raffleSettings = raffleSettings;
    }

    public ActionResponseDto Execute(DateTimeOffset nowUtc)
    {
        var reminder = TryBuildReminder(nowUtc);
        if (reminder is not null)
        {
            return reminder;
        }

        var dueRounds = raffleRepository.GetDueOpenRounds(nowUtc);
        if (dueRounds.Count == 0)
        {
            return Success("No due raffles to resolve.");
        }

        var messages = new List<string>();
        var rewardedViewerCount = 0;

        foreach (var round in dueRounds)
        {
            var participants = raffleRepository.GetParticipants(round.RoundId);
            var drawResult = runRaffleAction.Execute(new RaffleCommandDto
            {
                IsBroadcaster = round.IsBroadcaster,
                SingleWinner = round.SingleWinner,
                WinnerPoints = round.WinnerPoints,
                OccurredAtUtc = nowUtc
            }, participants);

            var entrantCount = runRaffleAction.GetEligibleEntrantCount(round.IsBroadcaster, participants);
            raffleRepository.CompleteRound(round.RoundId, nowUtc, entrantCount, drawResult.RewardedViewerCount);
            rewardedViewerCount += drawResult.RewardedViewerCount;
            messages.Add(drawResult.Message);
        }

        return new ActionResponseDto
        {
            Success = true,
            Message = string.Join(" ", messages),
            RewardedViewerCount = rewardedViewerCount
        };
    }

    private ActionResponseDto? TryBuildReminder(DateTimeOffset nowUtc)
    {
        var round = raffleRepository.GetOpenRound();
        if (round is null || nowUtc >= round.ResolveAtUtc)
        {
            return null;
        }

        var remaining = round.ResolveAtUtc - nowUtc;
        var participants = raffleRepository.GetParticipants(round.RoundId);
        var entrants = runRaffleAction.GetEligibleEntrantCount(round.IsBroadcaster, participants);

        if (remaining <= raffleSettings.TenSecondReminderThreshold && !round.TenSecondReminderSentAtUtc.HasValue)
        {
            raffleRepository.MarkTenSecondReminderSent(round.RoundId, nowUtc);
            return Success($"Raffle drawing in 10 seconds. Current eligible entrants: {entrants}.");
        }

        if (remaining <= raffleSettings.ThirtySecondReminderThreshold && !round.ThirtySecondReminderSentAtUtc.HasValue)
        {
            raffleRepository.MarkThirtySecondReminderSent(round.RoundId, nowUtc);
            return Success($"Raffle drawing in 30 seconds. Current eligible entrants: {entrants}.");
        }

        if (remaining <= raffleSettings.OneMinuteReminderThreshold && !round.OneMinuteReminderSentAtUtc.HasValue)
        {
            raffleRepository.MarkOneMinuteReminderSent(round.RoundId, nowUtc);
            return Success($"Raffle drawing in 1 minute. Current eligible entrants: {entrants}.");
        }

        return null;
    }

    private static ActionResponseDto Success(string message)
    {
        return new ActionResponseDto
        {
            Success = true,
            Message = message
        };
    }
}
