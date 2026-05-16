using TwitchHeists.Core.Models;
using TwitchHeists.Core.Services;
using TwitchHeists.Data.Sqlite.Repositories;
using TwitchHeists.StreamerBot.Contracts;

namespace TwitchHeists.StreamerBot.Services;

public sealed class GetPointsAction
{
    private readonly ViewerRepository viewerRepository;

    public GetPointsAction(ViewerRepository viewerRepository)
    {
        this.viewerRepository = viewerRepository;
    }

    public ActionResponseDto Execute(PointsQueryDto query)
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
        var points = viewerRepository.GetViewerBalance(targetViewer);

        return new ActionResponseDto
        {
            Success = true,
            Message = $"{targetDisplayName} has {PointValueNormalizer.Format(points)} points."
        };
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
