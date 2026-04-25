using System.Text.Json;
using TwitchHeists.Core.Models;
using TwitchHeists.StreamerBot.Bridge.Models;
using TwitchHeists.StreamerBot.Contracts;

namespace TwitchHeists.StreamerBot.Bridge.Services;

public sealed class BridgeActions
{
    private readonly BridgeRuntimeFactory runtimeFactory;

    public BridgeActions()
        : this(new BridgeRuntimeFactory())
    {
    }

    public BridgeActions(BridgeRuntimeFactory runtimeFactory)
    {
        this.runtimeFactory = runtimeFactory;
    }

    public BridgeResult RefreshCommunityViewers(
        string installDirectory,
        DateTimeOffset refreshTimestampUtc,
        IEnumerable<BridgeCommunityViewer> snapshot)
    {
        return Execute(
            installDirectory,
            () => runtimeFactory.CreateRefreshCommunityViewersAction(installDirectory).Execute(
                refreshTimestampUtc,
                snapshot.Select(Map)));
    }

    public BridgeResult RecordChatPresence(
        string installDirectory,
        BridgeChatPresence chatPresence,
        DateTimeOffset nextRefreshBoundaryUtc)
    {
        return Execute(
            installDirectory,
            () => runtimeFactory.CreateRecordChatPresenceAction(installDirectory).Execute(
                Map(chatPresence),
                nextRefreshBoundaryUtc));
    }

    public BridgeResult StartStream(string installDirectory, BridgeStreamLifecycleCommand command)
    {
        return Execute(
            installDirectory,
            () => runtimeFactory.CreateStartStreamAction(installDirectory).Execute(Map(command)));
    }

    public BridgeResult EndStream(string installDirectory, BridgeStreamLifecycleCommand command)
    {
        return Execute(
            installDirectory,
            () => runtimeFactory.CreateEndStreamAction(installDirectory).Execute(Map(command)));
    }

    public BridgeResult AddPoints(string installDirectory, BridgePointsCommand command)
    {
        return Execute(
            installDirectory,
            () => runtimeFactory.CreateAddPointsAction(installDirectory).Execute(Map(command)));
    }

    public BridgeResult RemovePoints(string installDirectory, BridgePointsCommand command)
    {
        return Execute(
            installDirectory,
            () => runtimeFactory.CreateRemovePointsAction(installDirectory).Execute(Map(command)));
    }

    public BridgeResult GivePoints(string installDirectory, BridgePointsCommand command)
    {
        return Execute(
            installDirectory,
            () => runtimeFactory.CreateGivePointsAction(installDirectory).Execute(Map(command)));
    }

    public BridgeResult GetWatchtime(string installDirectory, BridgeWatchtimeQuery query)
    {
        return Execute(
            installDirectory,
            () => runtimeFactory.CreateGetWatchtimeAction(installDirectory).Execute(Map(query)));
    }

    public BridgeResult StartHeist(string installDirectory, BridgeHeistCommand command)
    {
        return Execute(
            installDirectory,
            () => runtimeFactory.CreateStartHeistAction(installDirectory).Execute(Map(command)));
    }

    public BridgeResult JoinHeist(string installDirectory, BridgeHeistCommand command)
    {
        return Execute(
            installDirectory,
            () => runtimeFactory.CreateJoinHeistAction(installDirectory).Execute(Map(command)));
    }

    public BridgeResult ResolveDueHeists(string installDirectory, DateTimeOffset nowUtc)
    {
        return Execute(
            installDirectory,
            () => runtimeFactory.CreateResolveDueHeistsAction(installDirectory).Execute(nowUtc));
    }

    private static BridgeResult Execute(string installDirectory, Func<ActionResponseDto> operation)
    {
        try
        {
            return Map(operation());
        }
        catch (ArgumentException exception) when (string.Equals(exception.ParamName, "installDirectory", StringComparison.Ordinal))
        {
            return Failure(exception.Message);
        }
        catch (DirectoryNotFoundException exception)
        {
            return Failure(exception.Message);
        }
        catch (FileNotFoundException exception)
        {
            return Failure(exception.Message);
        }
        catch (InvalidDataException exception)
        {
            return Failure(exception.Message);
        }
        catch (InvalidOperationException exception)
        {
            return Failure(exception.Message);
        }
        catch (IOException exception)
        {
            return Failure($"The TwitchHeists install at '{installDirectory}' could not be accessed. {exception.Message}");
        }
        catch (JsonException exception)
        {
            return Failure($"The TwitchHeists configuration file is invalid JSON. {exception.Message}");
        }
        catch (UnauthorizedAccessException exception)
        {
            return Failure($"The TwitchHeists install at '{installDirectory}' could not be accessed. {exception.Message}");
        }
    }

    private static BridgeResult Map(ActionResponseDto response)
    {
        return new BridgeResult
        {
            Success = response.Success,
            Message = response.Message,
            RewardedViewerCount = response.RewardedViewerCount,
            ExpiredViewerCount = response.ExpiredViewerCount,
            TotalPointsAwarded = response.TotalPointsAwarded
        };
    }

    private static CommunityViewerDto Map(BridgeCommunityViewer viewer)
    {
        return new CommunityViewerDto
        {
            TwitchUserId = viewer.TwitchUserId,
            Username = viewer.Username,
            DisplayName = viewer.DisplayName,
            SubscriberTier = MapSubscriberTier(viewer.SubscriberTier)
        };
    }

    private static ChatPresenceDto Map(BridgeChatPresence presence)
    {
        return new ChatPresenceDto
        {
            TwitchUserId = presence.TwitchUserId,
            Username = presence.Username,
            DisplayName = presence.DisplayName,
            SubscriberTier = MapSubscriberTier(presence.SubscriberTier),
            MessageReceivedAtUtc = presence.MessageReceivedAtUtc
        };
    }

    private static HeistCommandDto Map(BridgeHeistCommand command)
    {
        return new HeistCommandDto
        {
            TwitchUserId = command.TwitchUserId,
            Username = command.Username,
            DisplayName = command.DisplayName,
            StakeAmount = command.StakeAmount,
            OccurredAtUtc = command.OccurredAtUtc
        };
    }

    private static StreamLifecycleCommandDto Map(BridgeStreamLifecycleCommand command)
    {
        return new StreamLifecycleCommandDto
        {
            OccurredAtUtc = command.OccurredAtUtc
        };
    }

    private static PointsCommandDto Map(BridgePointsCommand command)
    {
        return new PointsCommandDto
        {
            SourceTwitchUserId = command.SourceTwitchUserId,
            SourceUsername = command.SourceUsername,
            SourceDisplayName = command.SourceDisplayName,
            TargetTwitchUserId = command.TargetTwitchUserId,
            TargetUsername = command.TargetUsername,
            TargetDisplayName = command.TargetDisplayName,
            Amount = command.Amount,
            OccurredAtUtc = command.OccurredAtUtc
        };
    }

    private static WatchtimeQueryDto Map(BridgeWatchtimeQuery query)
    {
        return new WatchtimeQueryDto
        {
            RequesterTwitchUserId = query.RequesterTwitchUserId,
            RequesterUsername = query.RequesterUsername,
            RequesterDisplayName = query.RequesterDisplayName,
            TargetTwitchUserId = query.TargetTwitchUserId,
            TargetUsername = query.TargetUsername,
            TargetDisplayName = query.TargetDisplayName,
            OccurredAtUtc = query.OccurredAtUtc
        };
    }

    private static TwitchSubscriberTier MapSubscriberTier(int subscriberTier)
    {
        return subscriberTier switch
        {
            1 => TwitchSubscriberTier.Tier1,
            2 => TwitchSubscriberTier.Tier2,
            3 => TwitchSubscriberTier.Tier3,
            _ => TwitchSubscriberTier.None
        };
    }

    private static BridgeResult Failure(string message)
    {
        return new BridgeResult
        {
            Success = false,
            Message = message
        };
    }
}
