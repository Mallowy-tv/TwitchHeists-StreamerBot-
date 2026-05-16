using TwitchHeists.Core.Models;
using TwitchHeists.Core.Options;
using TwitchHeists.Core.Services;
using TwitchHeists.Data.Sqlite.Repositories;
using TwitchHeists.StreamerBot.Contracts;

namespace TwitchHeists.StreamerBot.Services;

public sealed class RunRaffleAction
{
    private static readonly Random DefaultRandom = new();
    private readonly ViewerRepository viewerRepository;
    private readonly RaffleWinnerCalculator raffleWinnerCalculator;
    private readonly RaffleSettings raffleSettings;
    private readonly Func<int, int> chooseIndex;

    public RunRaffleAction(
        ViewerRepository viewerRepository,
        RaffleWinnerCalculator raffleWinnerCalculator,
        RaffleSettings raffleSettings,
        Func<int, int>? chooseIndex = null)
    {
        this.viewerRepository = viewerRepository;
        this.raffleWinnerCalculator = raffleWinnerCalculator;
        this.raffleSettings = raffleSettings;
        this.chooseIndex = chooseIndex ?? DefaultRandom.Next;
    }

    public ActionResponseDto Execute(RaffleCommandDto command, IReadOnlyList<ViewerIdentity>? entrants = null)
    {
        var resolvedEntrants = ResolveEntrants(entrants);
        if (resolvedEntrants.Count == 0)
        {
            if (entrants is not null)
            {
                return Failure("No one joined this raffle.");
            }

            if (command.IsBroadcaster)
            {
                return Failure("There are no active participants for the raffle.");
            }

            return Failure("There are no active participants for the raffle.");
        }

        var winnerCount = command.SingleWinner
            ? 1
            : raffleWinnerCalculator.ResolveWinnerCount(resolvedEntrants.Count);

        var winners = DrawWinners(resolvedEntrants, winnerCount);
        var winnerPoints = ResolveWinnerPoints(command);
        if (winnerPoints > 0)
        {
            viewerRepository.AddPoints(winners, winnerPoints, command.OccurredAtUtc);
        }

        var totalPointsAwarded = winnerPoints > 0
            ? PointValueNormalizer.Normalize(winnerPoints * winners.Count)
            : 0m;
        var winnerNames = winners.Select(ResolveDisplayName).ToArray();
        var entrantsLabel = resolvedEntrants.Count == 1 ? "entrant" : "entrants";
        var pointsLabel = PointValueNormalizer.Format(winnerPoints);

        if (winnerNames.Length == 1)
        {
            return new ActionResponseDto
            {
                Success = true,
                Message = $"{winnerNames[0]} won the raffle and {pointsLabel} points (1/{resolvedEntrants.Count} {entrantsLabel}).",
                RewardedViewerCount = 1,
                TotalPointsAwarded = totalPointsAwarded
            };
        }

        return new ActionResponseDto
        {
            Success = true,
            Message = $"Raffle winners ({winnerNames.Length}/{resolvedEntrants.Count} {entrantsLabel}): {string.Join(", ", winnerNames)}. Each winner received {pointsLabel} points.",
            RewardedViewerCount = winnerNames.Length,
            TotalPointsAwarded = totalPointsAwarded
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

    public int GetEligibleEntrantCount(bool isBroadcaster, IReadOnlyList<ViewerIdentity>? entrants = null)
    {
        _ = isBroadcaster;
        return ResolveEntrants(entrants).Count;
    }

    private IReadOnlyList<ViewerIdentity> ResolveEntrants(IReadOnlyList<ViewerIdentity>? entrants)
    {
        var sourceEntrants = entrants ?? viewerRepository
            .GetActivePresence()
            .Select(presence => presence.Identity)
            .ToArray();
        var resolvedEntrants = new List<ViewerIdentity>(sourceEntrants.Count);
        var seenIdentities = new HashSet<string>(StringComparer.Ordinal);

        foreach (var identity in sourceEntrants)
        {
            if (!HasIdentity(identity))
            {
                continue;
            }

            if (!seenIdentities.Add(GetIdentityKey(identity)))
            {
                continue;
            }

            resolvedEntrants.Add(identity);
        }

        return resolvedEntrants;
    }

    private IReadOnlyList<ViewerIdentity> DrawWinners(IReadOnlyList<ViewerIdentity> entrants, int winnerCount)
    {
        var remainingEntrants = entrants.ToList();
        var winners = new List<ViewerIdentity>(winnerCount);

        for (var index = 0; index < winnerCount; index++)
        {
            var randomIndex = chooseIndex(remainingEntrants.Count);
            if (randomIndex < 0 || randomIndex >= remainingEntrants.Count)
            {
                throw new InvalidOperationException("Raffle random selector returned an invalid index.");
            }

            winners.Add(remainingEntrants[randomIndex]);
            remainingEntrants.RemoveAt(randomIndex);
        }

        return winners;
    }

    private static string ResolveDisplayName(ViewerIdentity identity)
    {
        return string.IsNullOrWhiteSpace(identity.DisplayName)
            ? identity.Username
            : identity.DisplayName!;
    }

    private static bool HasIdentity(ViewerIdentity identity)
    {
        return !string.IsNullOrWhiteSpace(identity.NormalizedUsername) ||
               !string.IsNullOrWhiteSpace(identity.TwitchUserId);
    }

    private static string GetIdentityKey(ViewerIdentity identity)
    {
        if (!string.IsNullOrWhiteSpace(identity.TwitchUserId))
        {
            return $"twitch:{identity.TwitchUserId}";
        }

        return $"username:{identity.NormalizedUsername}";
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
