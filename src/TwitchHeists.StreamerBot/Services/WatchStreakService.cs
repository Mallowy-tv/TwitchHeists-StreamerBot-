using TwitchHeists.Core.Models;
using TwitchHeists.Data.Sqlite.Repositories;

namespace TwitchHeists.StreamerBot.Services;

public sealed class WatchStreakService
{
    private const decimal PointsPerStreakStep = 100m;
    private readonly WatchStreakRepository watchStreakRepository;
    private readonly ViewerRepository viewerRepository;

    public WatchStreakService(WatchStreakRepository watchStreakRepository, ViewerRepository viewerRepository)
    {
        this.watchStreakRepository = watchStreakRepository;
        this.viewerRepository = viewerRepository;
    }

    public WatchStreakResult ApplySighting(ViewerIdentity viewer, DateTimeOffset sightingAtUtc)
    {
        var streamState = watchStreakRepository.GetStreamState();
        if (!streamState.IsStreamActive || !streamState.CurrentStreamStartedAtUtc.HasValue)
        {
            return WatchStreakResult.NoOp(streamIsActive: false);
        }

        if (sightingAtUtc < streamState.CurrentStreamStartedAtUtc.Value)
        {
            return WatchStreakResult.NoOp(streamIsActive: true);
        }

        var streakRecord = watchStreakRepository.GetViewerStreak(viewer);
        if (streakRecord?.LastAwardedStreamStartedAtUtc == streamState.CurrentStreamStartedAtUtc.Value)
        {
            return WatchStreakResult.NoOp(streamIsActive: true);
        }

        var nextStreak = 1;
        if (streakRecord?.LastAwardedStreamStartedAtUtc.HasValue == true &&
            streamState.LastCompletedStreamStartedAtUtc.HasValue &&
            streakRecord.LastAwardedStreamStartedAtUtc.Value == streamState.LastCompletedStreamStartedAtUtc.Value)
        {
            nextStreak = streakRecord.CurrentStreak + 1;
        }

        var awardedPoints = PointsPerStreakStep * nextStreak;
        viewerRepository.AddPoints(viewer, awardedPoints, sightingAtUtc);
        watchStreakRepository.RecordAward(viewer, nextStreak, streamState.CurrentStreamStartedAtUtc.Value, sightingAtUtc);

        return new WatchStreakResult
        {
            StreamIsActive = true,
            Awarded = true,
            AwardedPoints = awardedPoints,
            CurrentStreak = nextStreak
        };
    }

    public IReadOnlyList<WatchStreakResult> ApplySightings(IEnumerable<ViewerIdentity> viewers, DateTimeOffset sightingAtUtc)
    {
        var streamState = watchStreakRepository.GetStreamState();
        if (!streamState.IsStreamActive || !streamState.CurrentStreamStartedAtUtc.HasValue)
        {
            return Array.Empty<WatchStreakResult>();
        }

        if (sightingAtUtc < streamState.CurrentStreamStartedAtUtc.Value)
        {
            return Array.Empty<WatchStreakResult>();
        }

        var resolvedViewers = viewers
            .Where(HasIdentity)
            .GroupBy(viewer => NormalizeUsername(viewer), StringComparer.OrdinalIgnoreCase)
            .Select(group => group.Last())
            .ToArray();
        var streaksByViewer = watchStreakRepository.GetViewerStreaks(resolvedViewers);
        var pendingAwards = new List<PendingAward>();
        var results = new List<WatchStreakResult>(resolvedViewers.Length);

        foreach (var viewer in resolvedViewers)
        {
            streaksByViewer.TryGetValue(NormalizeUsername(viewer), out var streakRecord);

            if (streakRecord?.LastAwardedStreamStartedAtUtc == streamState.CurrentStreamStartedAtUtc.Value)
            {
                results.Add(WatchStreakResult.NoOp(streamIsActive: true));
                continue;
            }

            var nextStreak = 1;
            if (streakRecord?.LastAwardedStreamStartedAtUtc.HasValue == true &&
                streamState.LastCompletedStreamStartedAtUtc.HasValue &&
                streakRecord.LastAwardedStreamStartedAtUtc.Value == streamState.LastCompletedStreamStartedAtUtc.Value)
            {
                nextStreak = streakRecord.CurrentStreak + 1;
            }

            var awardedPoints = PointsPerStreakStep * nextStreak;
            pendingAwards.Add(new PendingAward(viewer, nextStreak, awardedPoints));
            results.Add(new WatchStreakResult
            {
                StreamIsActive = true,
                Awarded = true,
                AwardedPoints = awardedPoints,
                CurrentStreak = nextStreak
            });
        }

        foreach (var group in pendingAwards.GroupBy(award => award.AwardedPoints))
        {
            viewerRepository.AddPoints(group.Select(award => award.Viewer), group.Key, sightingAtUtc);
        }

        watchStreakRepository.RecordAwards(
            pendingAwards.Select(award => new WatchStreakAwardRecord
            {
                Viewer = award.Viewer,
                CurrentStreak = award.NextStreak,
                StreamStartedAtUtc = streamState.CurrentStreamStartedAtUtc.Value,
                AwardedAtUtc = sightingAtUtc
            }));

        return results;
    }

    private static bool HasIdentity(ViewerIdentity viewer)
    {
        return !string.IsNullOrWhiteSpace(viewer.Username) || !string.IsNullOrWhiteSpace(viewer.NormalizedUsername);
    }

    private static string NormalizeUsername(ViewerIdentity viewer)
    {
        var username = string.IsNullOrWhiteSpace(viewer.NormalizedUsername)
            ? viewer.Username
            : viewer.NormalizedUsername;

        return username.Trim().ToLowerInvariant();
    }

    private readonly struct PendingAward
    {
        public PendingAward(ViewerIdentity viewer, int nextStreak, decimal awardedPoints)
        {
            Viewer = viewer;
            NextStreak = nextStreak;
            AwardedPoints = awardedPoints;
        }

        public ViewerIdentity Viewer { get; }

        public int NextStreak { get; }

        public decimal AwardedPoints { get; }
    }
}

public sealed class WatchStreakResult
{
    public bool StreamIsActive { get; set; }

    public bool Awarded { get; set; }

    public decimal AwardedPoints { get; set; }

    public int CurrentStreak { get; set; }

    public static WatchStreakResult NoOp(bool streamIsActive)
    {
        return new WatchStreakResult
        {
            StreamIsActive = streamIsActive,
            Awarded = false,
            AwardedPoints = 0m,
            CurrentStreak = 0
        };
    }
}
