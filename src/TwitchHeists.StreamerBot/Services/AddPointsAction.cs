using System.Globalization;
using TwitchHeists.Core.Models;
using TwitchHeists.Core.Services;
using TwitchHeists.Data.Sqlite.Repositories;
using TwitchHeists.StreamerBot.Contracts;

namespace TwitchHeists.StreamerBot.Services;

public sealed class AddPointsAction
{
    private readonly ViewerRepository viewerRepository;

    public AddPointsAction(ViewerRepository viewerRepository)
    {
        this.viewerRepository = viewerRepository;
    }

    public ActionResponseDto Execute(PointsCommandDto command)
    {
        var amount = PointValueNormalizer.Normalize(command.Amount);
        if (amount <= 0)
        {
            return Failure("Point amount must be greater than zero.");
        }

        var normalizedTarget = NormalizeUsername(command.TargetUsername);
        if (string.Equals(normalizedTarget, "all", StringComparison.Ordinal))
        {
            var activeViewers = viewerRepository.GetActiveViewerIdentities();
            if (activeViewers.Count == 0)
            {
                return Failure("There are no active viewers to award points to.");
            }

            var updatedCount = viewerRepository.AddPoints(activeViewers, amount, command.OccurredAtUtc);
            return new ActionResponseDto
            {
                Success = true,
                Message = $"{updatedCount} active viewers each received {amount.ToString("0", CultureInfo.InvariantCulture)} points."
            };
        }

        var targetViewer = CreateViewerIdentity(command.TargetTwitchUserId, command.TargetUsername, command.TargetDisplayName);
        viewerRepository.AddPoints(targetViewer, amount, command.OccurredAtUtc);
        var updatedBalance = viewerRepository.GetViewerBalance(targetViewer);
        var targetName = string.IsNullOrWhiteSpace(command.TargetDisplayName) ? command.TargetUsername : command.TargetDisplayName;

        return new ActionResponseDto
        {
            Success = true,
            Message = $"{targetName} received {amount.ToString("0", CultureInfo.InvariantCulture)} points. Balance is now {updatedBalance.ToString("0", CultureInfo.InvariantCulture)}."
        };
    }

    private static ActionResponseDto Failure(string message)
    {
        return new ActionResponseDto
        {
            Success = false,
            Message = message
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
