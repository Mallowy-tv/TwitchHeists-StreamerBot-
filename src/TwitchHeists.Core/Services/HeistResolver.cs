using TwitchHeists.Core.Models;
using TwitchHeists.Core.Options;

namespace TwitchHeists.Core.Services;

public sealed class HeistResolver
{
    private static readonly Random DefaultRandom = new();
    private readonly HeistSettings heistSettings;
    private readonly Func<double> nextRandomValue;

    public HeistResolver(HeistSettings heistSettings, Func<double>? nextRandomValue = null)
    {
        this.heistSettings = heistSettings;
        this.nextRandomValue = nextRandomValue ?? (() => DefaultRandom.NextDouble());
    }

    public HeistResolutionResult Resolve(
        Guid roundId,
        IReadOnlyList<HeistParticipant> participants,
        decimal successChance,
        DateTimeOffset resolvedAtUtc)
    {
        if (participants.Count == 0)
        {
            throw new InvalidOperationException("Cannot resolve a heist without participants.");
        }

        var originalPot = PointValueNormalizer.Normalize(participants.Sum(participant => participant.StakeAmount));
        if (participants.Count < heistSettings.MinimumPlayers)
        {
            return new HeistResolutionResult
            {
                RoundId = roundId,
                FinalState = HeistRoundState.InsufficientCrew,
                SuccessChance = successChance,
                OriginalPot = originalPot,
                ResolvedPot = originalPot,
                Winners = Array.Empty<HeistParticipant>(),
                Losers = Array.Empty<HeistParticipant>(),
                RefundedParticipants = participants.Select(CloneParticipant).ToArray(),
                ResolvedAtUtc = resolvedAtUtc
            };
        }

        var wasSuccessful = (decimal)nextRandomValue() <= successChance;

        if (!wasSuccessful)
        {
            return new HeistResolutionResult
            {
                RoundId = roundId,
                FinalState = HeistRoundState.ResolvedFailure,
                SuccessChance = successChance,
                OriginalPot = originalPot,
                ResolvedPot = originalPot,
                Winners = Array.Empty<HeistParticipant>(),
                Losers = participants.Select(CloneParticipant).ToArray(),
                ResolvedAtUtc = resolvedAtUtc
            };
        }

        var remainingParticipants = participants.Select(CloneParticipant).ToList();
        var winnerCount = ResolveWinnerCount(remainingParticipants.Count);
        var winners = new List<HeistParticipant>(winnerCount);

        for (var index = 0; index < winnerCount; index++)
        {
            var randomIndex = (int)Math.Floor(nextRandomValue() * remainingParticipants.Count);
            if (randomIndex >= remainingParticipants.Count)
            {
                randomIndex = remainingParticipants.Count - 1;
            }

            var winner = remainingParticipants[randomIndex];
            winner.IsWinner = true;
            winners.Add(winner);
            remainingParticipants.RemoveAt(randomIndex);
        }

        var resolvedPot = PointValueNormalizer.Normalize(originalPot * heistSettings.SuccessfulPotMultiplier);
        AllocateWinnerPayouts(winners, resolvedPot);

        return new HeistResolutionResult
        {
            RoundId = roundId,
            FinalState = HeistRoundState.ResolvedSuccess,
            SuccessChance = successChance,
            OriginalPot = originalPot,
            ResolvedPot = resolvedPot,
            Winners = winners,
            Losers = remainingParticipants,
            ResolvedAtUtc = resolvedAtUtc
        };
    }

    private int ResolveWinnerCount(int participantCount)
    {
        var band = ResolveWinnerBand(participantCount);
        if (band is null)
        {
            return Math.Min(heistSettings.MaximumWinnerCount, participantCount);
        }

        var winnerRange = band.GetClampedWinnerRange(participantCount);
        if (winnerRange.MinimumWinners == winnerRange.MaximumWinners)
        {
            return winnerRange.MinimumWinners;
        }

        var rangeSize = winnerRange.MaximumWinners - winnerRange.MinimumWinners + 1;
        var randomOffset = (int)Math.Floor(nextRandomValue() * rangeSize);
        if (randomOffset >= rangeSize)
        {
            randomOffset = rangeSize - 1;
        }

        return winnerRange.MinimumWinners + randomOffset;
    }

    private HeistWinnerBand? ResolveWinnerBand(int participantCount)
    {
        var winnerBands = heistSettings.WinnerBands
            .OrderBy(band => band.MinimumParticipants)
            .ToList();

        if (winnerBands.Count == 0)
        {
            return null;
        }

        var exactBand = winnerBands.FirstOrDefault(band => band.ContainsParticipants(participantCount));
        if (exactBand is not null)
        {
            return exactBand;
        }

        var lastBand = winnerBands[winnerBands.Count - 1];
        return participantCount > lastBand.MaximumParticipants
            ? lastBand
            : null;
    }

    private static void AllocateWinnerPayouts(IReadOnlyList<HeistParticipant> winners, decimal resolvedPot)
    {
        var totalWinnerStake = winners.Sum(winner => winner.StakeAmount);
        var remainingPot = resolvedPot;

        for (var index = 0; index < winners.Count; index++)
        {
            var winner = winners[index];
            if (index == winners.Count - 1)
            {
                winner.PayoutAmount = remainingPot;
                continue;
            }

            var payout = PointValueNormalizer.Normalize(resolvedPot * (winner.StakeAmount / totalWinnerStake));
            if (payout > remainingPot)
            {
                payout = remainingPot;
            }

            winner.PayoutAmount = payout;
            remainingPot -= payout;
        }
    }

    private static HeistParticipant CloneParticipant(HeistParticipant participant)
    {
        return new HeistParticipant
        {
            Identity = new ViewerIdentity
            {
                TwitchUserId = participant.Identity.TwitchUserId,
                Username = participant.Identity.Username,
                NormalizedUsername = participant.Identity.NormalizedUsername,
                DisplayName = participant.Identity.DisplayName
            },
            StakeAmount = participant.StakeAmount,
            JoinedAtUtc = participant.JoinedAtUtc,
            IsWinner = participant.IsWinner,
            PayoutAmount = participant.PayoutAmount
        };
    }
}
