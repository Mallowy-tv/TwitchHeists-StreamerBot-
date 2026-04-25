using System.Globalization;
using TwitchHeists.Data.Sqlite.Repositories;
using TwitchHeists.StreamerBot.Contracts;

namespace TwitchHeists.StreamerBot.Services;

public sealed class RemovePointsAction
{
    private readonly ViewerRepository viewerRepository;

    public RemovePointsAction(ViewerRepository viewerRepository)
    {
        this.viewerRepository = viewerRepository;
    }

    public ActionResponseDto Execute(PointsCommandDto command)
    {
        if (command.Amount <= 0)
        {
            return Failure("Point amount must be greater than zero.");
        }

        var normalizedTarget = NormalizeUsername(command.TargetUsername);
        if (string.Equals(normalizedTarget, "all", StringComparison.Ordinal))
        {
            var activeViewers = viewerRepository.GetActiveNormalizedUsernames();
            if (activeViewers.Count == 0)
            {
                return Failure("There are no active viewers to remove points from.");
            }

            var updatedCount = viewerRepository.RemovePoints(activeViewers, command.Amount, command.OccurredAtUtc);
            return new ActionResponseDto
            {
                Success = true,
                Message = $"{updatedCount} active viewers each lost {command.Amount.ToString("0.##", CultureInfo.InvariantCulture)} points."
            };
        }

        viewerRepository.RemovePoints(normalizedTarget, command.Amount, command.OccurredAtUtc);
        var updatedBalance = viewerRepository.GetViewerBalance(normalizedTarget);
        var targetName = string.IsNullOrWhiteSpace(command.TargetDisplayName) ? command.TargetUsername : command.TargetDisplayName;

        return new ActionResponseDto
        {
            Success = true,
            Message = $"{targetName} lost {command.Amount.ToString("0.##", CultureInfo.InvariantCulture)} points. Balance is now {updatedBalance.ToString("0.##", CultureInfo.InvariantCulture)}."
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
