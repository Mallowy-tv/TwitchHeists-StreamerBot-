using System.Globalization;
using TwitchHeists.Core.Services;
using TwitchHeists.Data.Sqlite.Repositories;
using TwitchHeists.StreamerBot.Contracts;

namespace TwitchHeists.StreamerBot.Services;

public sealed class ResolveDueHeistsAction
{
    private readonly HeistRepository heistRepository;
    private readonly HeistChanceCalculator heistChanceCalculator;
    private readonly HeistResolver heistResolver;

    public ResolveDueHeistsAction(
        HeistRepository heistRepository,
        HeistChanceCalculator heistChanceCalculator,
        HeistResolver heistResolver)
    {
        this.heistRepository = heistRepository;
        this.heistChanceCalculator = heistChanceCalculator;
        this.heistResolver = heistResolver;
    }

    public ActionResponseDto Execute(DateTimeOffset nowUtc)
    {
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
            heistRepository.ApplyResolution(resolution);

            rewardedViewerCount += resolution.Winners.Count;
            totalPointsAwarded += resolution.Winners.Sum(winner => winner.PayoutAmount);
            messages.Add(
                resolution.FinalState == Core.Models.HeistRoundState.ResolvedSuccess
                    ? $"Heist {round.RoundId:D} succeeded at {(successChance * 100m).ToString("0.##", CultureInfo.InvariantCulture)}%."
                    : $"Heist {round.RoundId:D} failed at {(successChance * 100m).ToString("0.##", CultureInfo.InvariantCulture)}%.");
        }

        return new ActionResponseDto
        {
            Success = true,
            Message = string.Join(" ", messages),
            RewardedViewerCount = rewardedViewerCount,
            TotalPointsAwarded = totalPointsAwarded
        };
    }
}
