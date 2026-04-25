using System.Globalization;
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
        if (command.Amount <= 0)
        {
            return Failure("Point amount must be greater than zero.");
        }

        var normalizedSource = NormalizeUsername(command.SourceUsername);
        var normalizedTarget = NormalizeUsername(command.TargetUsername);
        if (string.Equals(normalizedSource, normalizedTarget, StringComparison.Ordinal))
        {
            return Failure("You cannot give points to yourself.");
        }

        try
        {
            viewerRepository.TransferPoints(normalizedSource, normalizedTarget, command.Amount, command.OccurredAtUtc);
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
            Message = $"{sourceName} gave {command.Amount.ToString("0.##", CultureInfo.InvariantCulture)} points to {targetName}."
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
}
