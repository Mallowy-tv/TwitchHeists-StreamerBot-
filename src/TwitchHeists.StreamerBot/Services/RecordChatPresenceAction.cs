using TwitchHeists.Core.Models;
using TwitchHeists.Data.Sqlite.Repositories;
using TwitchHeists.StreamerBot.Contracts;

namespace TwitchHeists.StreamerBot.Services;

public sealed class RecordChatPresenceAction
{
    private readonly ViewerRepository viewerRepository;

    public RecordChatPresenceAction(ViewerRepository viewerRepository)
    {
        this.viewerRepository = viewerRepository;
    }

    public ActionResponseDto Execute(ChatPresenceDto chatPresence, DateTimeOffset nextRefreshBoundaryUtc)
    {
        var normalizedUsername = NormalizeUsername(chatPresence.Username);
        var presenceRecord = new ViewerPresenceRecord
        {
            Identity = new ViewerIdentity
            {
                TwitchUserId = chatPresence.TwitchUserId,
                Username = chatPresence.Username,
                NormalizedUsername = normalizedUsername,
                DisplayName = chatPresence.DisplayName ?? chatPresence.Username
            },
            ActiveSinceUtc = chatPresence.MessageReceivedAtUtc,
            LastSeenUtc = chatPresence.MessageReceivedAtUtc,
            PresenceSource = PresenceSource.ChatFallback,
            SubscriberTier = chatPresence.SubscriberTier,
            PresenceExpiresAtUtc = nextRefreshBoundaryUtc
        };

        viewerRepository.StoreChatPresence(presenceRecord);

        return new ActionResponseDto
        {
            Success = true,
            Message = $"{chatPresence.Username} recorded from chat activity."
        };
    }

    private static string NormalizeUsername(string username)
    {
        return username.Trim().ToLowerInvariant();
    }
}
