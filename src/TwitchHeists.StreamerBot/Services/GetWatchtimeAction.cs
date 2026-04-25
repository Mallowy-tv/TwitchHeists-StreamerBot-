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
        var normalizedTarget = NormalizeUsername(targetUsername);
        var totalWatchMinutes = viewerRepository.GetLifetimeWatchMinutes(normalizedTarget);

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
}
