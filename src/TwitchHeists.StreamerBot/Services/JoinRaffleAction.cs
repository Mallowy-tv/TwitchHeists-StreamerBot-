using TwitchHeists.Core.Models;
using TwitchHeists.Data.Sqlite.Repositories;
using TwitchHeists.StreamerBot.Contracts;

namespace TwitchHeists.StreamerBot.Services;

public sealed class JoinRaffleAction
{
    private readonly RaffleRepository raffleRepository;

    public JoinRaffleAction(RaffleRepository raffleRepository)
    {
        this.raffleRepository = raffleRepository;
    }

    public ActionResponseDto Execute(RaffleCommandDto command)
    {
        var round = raffleRepository.GetOpenRound();
        if (round is null)
        {
            return Failure("There is no open raffle to join.");
        }

        var viewer = CreateViewerIdentity(command.SourceTwitchUserId, command.SourceUsername, command.SourceDisplayName);

        try
        {
            var participantCount = raffleRepository.JoinOpenRound(viewer, command.OccurredAtUtc);
            return new ActionResponseDto
            {
                Success = true,
                Message = $"{ResolveDisplayName(viewer)} joined the raffle. Current entrants: {participantCount}."
            };
        }
        catch (InvalidOperationException exception)
        {
            return Failure(exception.Message);
        }
    }

    private static string ResolveDisplayName(ViewerIdentity identity)
    {
        return string.IsNullOrWhiteSpace(identity.DisplayName)
            ? identity.Username
            : identity.DisplayName!;
    }

    private static ViewerIdentity CreateViewerIdentity(string? twitchUserId, string username, string? displayName)
    {
        var normalizedUsername = username.Trim().ToLowerInvariant();

        return new ViewerIdentity
        {
            TwitchUserId = twitchUserId,
            Username = username,
            NormalizedUsername = normalizedUsername,
            DisplayName = displayName ?? username
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
