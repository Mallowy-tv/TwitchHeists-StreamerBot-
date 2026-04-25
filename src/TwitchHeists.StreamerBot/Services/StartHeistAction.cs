using System.Globalization;
using TwitchHeists.Core.Models;
using TwitchHeists.Core.Options;
using TwitchHeists.Data.Sqlite.Repositories;
using TwitchHeists.StreamerBot.Contracts;

namespace TwitchHeists.StreamerBot.Services;

public sealed class StartHeistAction
{
    private readonly HeistRepository heistRepository;
    private readonly HeistSettings heistSettings;

    public StartHeistAction(HeistRepository heistRepository, HeistSettings heistSettings)
    {
        this.heistRepository = heistRepository;
        this.heistSettings = heistSettings;
    }

    public ActionResponseDto Execute(HeistCommandDto command)
    {
        if (command.StakeAmount <= 0)
        {
            return Failure("Stake amount must be greater than zero.");
        }

        try
        {
            var roundId = heistRepository.StartRound(
                CreateViewerIdentity(command),
                command.StakeAmount,
                command.OccurredAtUtc,
                command.OccurredAtUtc.Add(heistSettings.JoinWindow));
            var round = heistRepository.GetOpenRound();

            return new ActionResponseDto
            {
                Success = true,
                Message = $"{command.Username} started a heist with {command.StakeAmount.ToString("0.##", CultureInfo.InvariantCulture)} points. Round {roundId:D} closes at {round?.ResolveAtUtc:HH:mm:ss} UTC."
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
