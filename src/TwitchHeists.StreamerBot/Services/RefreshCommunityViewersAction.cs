using TwitchHeists.Core.Models;
using TwitchHeists.Core.Services;
using TwitchHeists.Data.Sqlite.Repositories;
using TwitchHeists.StreamerBot.Contracts;

namespace TwitchHeists.StreamerBot.Services;

public sealed class RefreshCommunityViewersAction
{
    private readonly ViewerRepository viewerRepository;
    private readonly WatchtimeCalculator watchtimeCalculator;
    private readonly WatchStreakService? watchStreakService;

    public RefreshCommunityViewersAction(
        ViewerRepository viewerRepository,
        WatchtimeCalculator watchtimeCalculator,
        WatchStreakService? watchStreakService = null)
    {
        this.viewerRepository = viewerRepository;
        this.watchtimeCalculator = watchtimeCalculator;
        this.watchStreakService = watchStreakService;
    }

    public ActionResponseDto Execute(DateTimeOffset refreshTimestampUtc, IEnumerable<CommunityViewerDto> snapshot)
    {
        var viewerSnapshot = snapshot.ToArray();
        var placeholderUsernames = viewerSnapshot
            .Where(viewer => LooksLikePlaceholder(viewer.Username))
            .Select(viewer => viewer.Username.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var placeholderWarning = placeholderUsernames.Length > 0
            ? $"Community refresh snapshot contains placeholder username '{placeholderUsernames[0]}'. Replace placeholder values with real Community viewer data."
            : null;

        var confirmedPresence = viewerSnapshot
            .Where(viewer => !string.IsNullOrWhiteSpace(viewer.Username))
            .Where(viewer => !LooksLikePlaceholder(viewer.Username))
            .GroupBy(viewer => NormalizeUsername(viewer.Username), StringComparer.OrdinalIgnoreCase)
            .Select(group =>
            {
                var viewer = group.Last();
                return new ViewerPresenceRecord
                {
                    Identity = new ViewerIdentity
                    {
                        TwitchUserId = viewer.TwitchUserId,
                        Username = viewer.Username,
                        NormalizedUsername = NormalizeUsername(viewer.Username),
                        DisplayName = viewer.DisplayName ?? viewer.Username
                    },
                    ActiveSinceUtc = refreshTimestampUtc,
                    LastSeenUtc = refreshTimestampUtc,
                    PresenceSource = PresenceSource.CommunityRefresh,
                    SubscriberTier = viewer.SubscriberTier
                };
            })
            .ToArray();

        var knownPresence = viewerRepository.GetActivePresence();
        var cycleResult = watchtimeCalculator.CalculateCycle(refreshTimestampUtc, confirmedPresence, knownPresence);
        var applied = viewerRepository.ApplyRewardCycle(
            refreshTimestampUtc,
            cycleResult.ActivePresence,
            cycleResult.ExpiredPresence,
            cycleResult.Rewards);

        if (watchStreakService is not null)
        {
            watchStreakService.ApplySightings(confirmedPresence.Select(record => record.Identity), refreshTimestampUtc);
        }

        if (!applied)
        {
            return new ActionResponseDto
            {
                Success = true,
                Message = BuildMessage("Refresh cycle was already applied.", placeholderWarning),
                RewardedViewerCount = 0,
                ExpiredViewerCount = 0,
                TotalPointsAwarded = 0m
            };
        }

        return new ActionResponseDto
        {
            Success = true,
            Message = BuildMessage("Refresh cycle applied.", placeholderWarning),
            RewardedViewerCount = cycleResult.Rewards.Count,
            ExpiredViewerCount = cycleResult.ExpiredPresence.Count,
            TotalPointsAwarded = cycleResult.Rewards.Sum(reward => reward.PointsAwarded)
        };
    }

    private static string NormalizeUsername(string username)
    {
        return username.Trim().ToLowerInvariant();
    }

    private static bool LooksLikePlaceholder(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var trimmed = value!.Trim();
        return trimmed.Length > 2 && trimmed.StartsWith("<", StringComparison.Ordinal) && trimmed.EndsWith(">", StringComparison.Ordinal);
    }

    private static string BuildMessage(string baseMessage, string? warning)
    {
        return string.IsNullOrWhiteSpace(warning)
            ? baseMessage
            : $"{warning} {baseMessage}";
    }
}
