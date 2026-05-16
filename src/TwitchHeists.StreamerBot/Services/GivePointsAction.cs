using System.Globalization;
using TwitchHeists.Core.Models;
using TwitchHeists.Core.Services;
using TwitchHeists.Data.Sqlite.Repositories;
using TwitchHeists.StreamerBot.Contracts;

namespace TwitchHeists.StreamerBot.Services;

public sealed class GivePointsAction
{
    private readonly ViewerRepository viewerRepository;

    public GivePointsAction(ViewerRepository viewerRepository)
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

        var sourceViewer = CreateViewerIdentity(command.SourceTwitchUserId, command.SourceUsername, command.SourceDisplayName);
        var targetViewer = CreateViewerIdentity(command.TargetTwitchUserId, command.TargetUsername, command.TargetDisplayName);
        if (RepresentsSameViewer(sourceViewer, targetViewer))
        {
            return Failure("You cannot give points to yourself.");
        }

        try
        {
            viewerRepository.TransferPoints(sourceViewer, targetViewer, amount, command.OccurredAtUtc);
        }
        catch (InvalidOperationException exception)
        {
            return Failure(exception.Message);
        }

        var sourceName = string.IsNullOrWhiteSpace(command.SourceDisplayName) ? command.SourceUsername : command.SourceDisplayName;
        var targetName = string.IsNullOrWhiteSpace(command.TargetDisplayName) ? command.TargetUsername : command.TargetDisplayName;

        return new ActionResponseDto
        {
            Success = true,
            Message = $"{sourceName} gave {amount.ToString("0", CultureInfo.InvariantCulture)} points to {targetName}."
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

    private static bool RepresentsSameViewer(ViewerIdentity sourceViewer, ViewerIdentity targetViewer)
    {
        if (!string.IsNullOrWhiteSpace(sourceViewer.TwitchUserId) &&
            !string.IsNullOrWhiteSpace(targetViewer.TwitchUserId))
        {
            return string.Equals(sourceViewer.TwitchUserId, targetViewer.TwitchUserId, StringComparison.Ordinal);
        }

        return string.Equals(sourceViewer.NormalizedUsername, targetViewer.NormalizedUsername, StringComparison.Ordinal);
    }
}
