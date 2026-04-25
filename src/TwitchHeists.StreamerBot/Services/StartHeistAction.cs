using TwitchHeists.Core.Models;
using TwitchHeists.Core.Options;
using TwitchHeists.Data.Sqlite.Repositories;
using TwitchHeists.StreamerBot.Contracts;

namespace TwitchHeists.StreamerBot.Services;

public sealed class StartHeistAction
{
    private readonly HeistRepository heistRepository;
    private readonly HeistSettings heistSettings;
    private readonly HeistMessageComposer messageComposer;

    public StartHeistAction(HeistRepository heistRepository, HeistSettings heistSettings, HeistMessageComposer messageComposer)
    {
        this.heistRepository = heistRepository;
        this.heistSettings = heistSettings;
        this.messageComposer = messageComposer;
    }

    public ActionResponseDto Execute(HeistCommandDto command)
    {
        if (command.StakeAmount <= 0)
        {
            return Failure("Stake amount must be greater than zero.");
        }

        var cooldownEndsAtUtc = heistRepository.GetActiveCooldownEndsAtUtc(command.OccurredAtUtc);
        if (cooldownEndsAtUtc.HasValue)
        {
            return Failure(messageComposer.ComposeCooldown(cooldownEndsAtUtc.Value - command.OccurredAtUtc));
        }

        try
        {
            heistRepository.StartRound(
                CreateViewerIdentity(command),
                command.StakeAmount,
                command.OccurredAtUtc,
                command.OccurredAtUtc.Add(heistSettings.JoinWindow));

            return new ActionResponseDto
            {
                Success = true,
                Message = messageComposer.ComposeStart(command.Username, command.StakeAmount, heistSettings.JoinWindow)
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
