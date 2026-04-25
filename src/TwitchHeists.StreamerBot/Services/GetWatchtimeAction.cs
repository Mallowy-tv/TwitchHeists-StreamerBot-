using TwitchHeists.Core.Models;
using TwitchHeists.Data.Sqlite.Repositories;
using TwitchHeists.StreamerBot.Contracts;

namespace TwitchHeists.StreamerBot.Services;

public sealed class GetWatchtimeAction
{
    private readonly ViewerRepository viewerRepository;

    public GetWatchtimeAction(ViewerRepository viewerRepository)
    {
        this.viewerRepository = viewerRepository;
    }

    public ActionResponseDto Execute(WatchtimeQueryDto query)
    {
        var targetUsername = string.IsNullOrWhiteSpace(query.TargetUsername)
            ? query.RequesterUsername
            : query.TargetUsername!;
        var targetDisplayName = string.IsNullOrWhiteSpace(query.TargetUsername)
            ? (string.IsNullOrWhiteSpace(query.RequesterDisplayName) ? query.RequesterUsername : query.RequesterDisplayName)
            : (string.IsNullOrWhiteSpace(query.TargetDisplayName) ? query.TargetUsername : query.TargetDisplayName);
        var targetViewer = CreateViewerIdentity(
            string.IsNullOrWhiteSpace(query.TargetUsername) ? query.RequesterTwitchUserId : query.TargetTwitchUserId,
            targetUsername,
            targetDisplayName);
        var totalWatchMinutes = viewerRepository.GetLifetimeWatchMinutes(targetViewer);

        return new ActionResponseDto
        {
            Success = true,
            Message = $"{targetDisplayName} has watched for {FormatWatchtime(totalWatchMinutes)} total."
        };
    }

    private static string FormatWatchtime(int totalWatchMinutes)
    {
        var hours = totalWatchMinutes / 60;
        var minutes = totalWatchMinutes % 60;

        return hours > 0
            ? $"{hours}h {minutes}m"
            : $"{minutes}m";
    }

    private static string NormalizeUsername(string username)
    {
        return username.Trim().ToLowerInvariant();
    }

    private static ViewerIdentity CreateViewerIdentity(string? twitchUserId, string username, string? displayName)
    {
        var resolvedTwitchUserId = string.IsNullOrWhiteSpace(twitchUserId) ? null : twitchUserId!.Trim();

        return new ViewerIdentity
        {
            TwitchUserId = resolvedTwitchUserId,
            Username = username,
            NormalizedUsername = NormalizeUsername(username),
            DisplayName = displayName ?? username
        };
    }
}
