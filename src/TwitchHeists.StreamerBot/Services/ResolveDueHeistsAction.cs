using TwitchHeists.Core.Models;
using TwitchHeists.Core.Options;
using TwitchHeists.Core.Services;
using TwitchHeists.Data.Sqlite.Repositories;
using TwitchHeists.StreamerBot.Contracts;

namespace TwitchHeists.StreamerBot.Services;

public sealed class ResolveDueHeistsAction
{
    private readonly HeistRepository heistRepository;
    private readonly HeistChanceCalculator heistChanceCalculator;
    private readonly HeistResolver heistResolver;
    private readonly HeistSettings heistSettings;
    private readonly HeistMessageComposer messageComposer;

    public ResolveDueHeistsAction(
        HeistRepository heistRepository,
        HeistChanceCalculator heistChanceCalculator,
        HeistResolver heistResolver,
        HeistSettings heistSettings,
        HeistMessageComposer messageComposer)
    {
        this.heistRepository = heistRepository;
        this.heistChanceCalculator = heistChanceCalculator;
        this.heistResolver = heistResolver;
        this.heistSettings = heistSettings;
        this.messageComposer = messageComposer;
    }

    public ActionResponseDto Execute(DateTimeOffset nowUtc)
    {
        var reminder = TryBuildReminder(nowUtc);
        if (reminder is not null)
        {
            return reminder;
        }

        var dueRounds = heistRepository.GetDueOpenRounds(nowUtc);
        if (dueRounds.Count == 0)
        {
            return new ActionResponseDto
            {
                Success = true,
                Message = "No due heists to resolve."
            };
        }

        var messages = new List<string>();
        var rewardedViewerCount = 0;
        var totalPointsAwarded = 0m;

        foreach (var round in dueRounds)
        {
            var participants = heistRepository.GetParticipants(round.RoundId);
            var totalStake = participants.Sum(participant => participant.StakeAmount);
            var successChance = heistChanceCalculator.CalculateSuccessChance(totalStake, participants.Count);
            var resolution = heistResolver.Resolve(round.RoundId, participants, successChance, nowUtc);
            heistRepository.ApplyResolution(resolution, nowUtc.Add(heistSettings.CooldownWindow));

            rewardedViewerCount += resolution.Winners.Count;
            totalPointsAwarded += resolution.Winners.Sum(winner => winner.PayoutAmount);
            messages.Add(messageComposer.ComposeResolution(resolution));
        }

        return new ActionResponseDto
        {
            Success = true,
            Message = string.Join(" ", messages),
            RewardedViewerCount = rewardedViewerCount,
            TotalPointsAwarded = totalPointsAwarded
        };
    }

    private ActionResponseDto? TryBuildReminder(DateTimeOffset nowUtc)
    {
        var round = heistRepository.GetOpenRound();
        if (round is null || nowUtc >= round.ResolveAtUtc)
        {
            return null;
        }

        var remaining = round.ResolveAtUtc - nowUtc;

        if (remaining <= heistSettings.TenSecondReminderThreshold && !round.TenSecondReminderSentAtUtc.HasValue)
        {
            heistRepository.MarkTenSecondReminderSent(round.RoundId, nowUtc);
            return Success(messageComposer.ComposeReminder(round.OriginalPot, heistRepository.GetParticipants(round.RoundId).Count, "10 seconds"));
        }

        if (remaining <= heistSettings.ThirtySecondReminderThreshold && !round.ThirtySecondReminderSentAtUtc.HasValue)
        {
            heistRepository.MarkThirtySecondReminderSent(round.RoundId, nowUtc);
            return Success(messageComposer.ComposeReminder(round.OriginalPot, heistRepository.GetParticipants(round.RoundId).Count, "30 seconds"));
        }

        if (remaining <= heistSettings.OneMinuteReminderThreshold && !round.OneMinuteReminderSentAtUtc.HasValue)
        {
            heistRepository.MarkOneMinuteReminderSent(round.RoundId, nowUtc);
            return Success(messageComposer.ComposeReminder(round.OriginalPot, heistRepository.GetParticipants(round.RoundId).Count, "1 minute"));
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
