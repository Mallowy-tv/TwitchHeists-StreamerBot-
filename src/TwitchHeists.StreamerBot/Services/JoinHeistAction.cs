using System;
using System.Globalization;
using TwitchHeists.Core.Models;
using TwitchHeists.Core.Options;
using TwitchHeists.Core.Services;
using TwitchHeists.Data.Sqlite.Repositories;
using TwitchHeists.StreamerBot.Contracts;

namespace TwitchHeists.StreamerBot.Services;

public sealed class JoinHeistAction
{
    private readonly HeistRepository heistRepository;
    private readonly HeistSettings heistSettings;
    private readonly HeistMessageComposer messageComposer;

    public JoinHeistAction(HeistRepository heistRepository, HeistSettings heistSettings, HeistMessageComposer messageComposer)
    {
        this.heistRepository = heistRepository;
        this.heistSettings = heistSettings;
        this.messageComposer = messageComposer;
    }

    public ActionResponseDto Execute(HeistCommandDto command)
    {
        var stakeAmount = PointValueNormalizer.Normalize(command.StakeAmount);
        if (stakeAmount <= 0)
        {
            return Failure("Stake amount must be greater than zero.");
        }

        if (stakeAmount < heistSettings.MinimumJoinAmount)
        {
            return Failure(messageComposer.ComposeMinimumJoinAmount(command.Username, heistSettings.MinimumJoinAmount));
        }

        try
        {
            heistRepository.JoinOpenRound(CreateViewerIdentity(command), stakeAmount, command.OccurredAtUtc);
            var round = heistRepository.GetOpenRound();
            var participants = round is null ? 0 : heistRepository.GetParticipants(round.RoundId).Count;

            return new ActionResponseDto
            {
                Success = true,
                Message = $"{command.Username} joined the heist with {stakeAmount.ToString("0", CultureInfo.InvariantCulture)} points. Pot is now {round?.OriginalPot.ToString("0", CultureInfo.InvariantCulture)} across {participants} viewers."
            };
        }
        catch (InvalidOperationException exception)
        {
            if (string.Equals(
                    exception.Message,
                    "Viewer has already joined the open heist.",
                    StringComparison.Ordinal))
            {
                return Failure(messageComposer.ComposeAlreadyJoined(command.Username));
            }

            if (string.Equals(
                    exception.Message,
                    HeistRepository.InsufficientBalanceForStakeMessage,
                    StringComparison.Ordinal))
            {
                return Failure(messageComposer.ComposeInsufficientBalance(command.Username, stakeAmount));
            }

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
