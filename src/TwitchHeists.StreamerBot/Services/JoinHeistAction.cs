using System.Globalization;
using TwitchHeists.Core.Models;
using TwitchHeists.Data.Sqlite.Repositories;
using TwitchHeists.StreamerBot.Contracts;

namespace TwitchHeists.StreamerBot.Services;

public sealed class JoinHeistAction
{
    private readonly HeistRepository heistRepository;

    public JoinHeistAction(HeistRepository heistRepository)
    {
        this.heistRepository = heistRepository;
    }

    public ActionResponseDto Execute(HeistCommandDto command)
    {
        if (command.StakeAmount <= 0)
        {
            return Failure("Stake amount must be greater than zero.");
        }

        try
        {
            heistRepository.JoinOpenRound(CreateViewerIdentity(command), command.StakeAmount, command.OccurredAtUtc);
            var round = heistRepository.GetOpenRound();
            var participants = round is null ? 0 : heistRepository.GetParticipants(round.RoundId).Count;

            return new ActionResponseDto
            {
                Success = true,
                Message = $"{command.Username} joined the heist with {command.StakeAmount.ToString("0.##", CultureInfo.InvariantCulture)} points. Pot is now {round?.OriginalPot.ToString("0.##", CultureInfo.InvariantCulture)} across {participants} viewers."
            };
        }
        catch (InvalidOperationException exception)
        {
            return Failure(exception.Message);
        }
    }

    private static ViewerIdentity CreateViewerIdentity(HeistCommandDto command)
    {
        var normalizedUsername = command.Username.Trim().ToLowerInvariant();

        return new ViewerIdentity
        {
            TwitchUserId = command.TwitchUserId,
            Username = command.Username,
            NormalizedUsername = normalizedUsername,
            DisplayName = command.DisplayName ?? command.Username
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
}
