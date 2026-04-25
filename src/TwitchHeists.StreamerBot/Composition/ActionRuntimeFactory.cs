using System.Text.Json;
using TwitchHeists.Core.Options;
using TwitchHeists.Core.Services;
using TwitchHeists.Data.Sqlite.Repositories;
using TwitchHeists.Data.Sqlite.Schema;
using TwitchHeists.StreamerBot.Configuration;
using TwitchHeists.StreamerBot.Services;

namespace TwitchHeists.StreamerBot.Composition;

public sealed class ActionRuntimeFactory
{
    public RefreshCommunityViewersAction CreateRefreshCommunityViewersAction(string connectionString, string? configurationPath = null)
    {
        var rewardSettings = LoadRewardSettings(configurationPath);
        var schemaBootstrapper = new SchemaBootstrapper();
        var viewerRepository = new ViewerRepository(connectionString, schemaBootstrapper);
        var watchStreakRepository = new WatchStreakRepository(connectionString, schemaBootstrapper);
        var watchStreakService = new WatchStreakService(watchStreakRepository, viewerRepository);
        return new RefreshCommunityViewersAction(viewerRepository, new WatchtimeCalculator(rewardSettings), watchStreakService);
    }

    public RecordChatPresenceAction CreateRecordChatPresenceAction(string connectionString)
    {
        var schemaBootstrapper = new SchemaBootstrapper();
        var viewerRepository = new ViewerRepository(connectionString, schemaBootstrapper);
        var watchStreakRepository = new WatchStreakRepository(connectionString, schemaBootstrapper);
        var watchStreakService = new WatchStreakService(watchStreakRepository, viewerRepository);
        return new RecordChatPresenceAction(viewerRepository, watchStreakService);
    }

    public AddPointsAction CreateAddPointsAction(string connectionString)
    {
        var viewerRepository = new ViewerRepository(connectionString, new SchemaBootstrapper());
        return new AddPointsAction(viewerRepository);
    }

    public RemovePointsAction CreateRemovePointsAction(string connectionString)
    {
        var viewerRepository = new ViewerRepository(connectionString, new SchemaBootstrapper());
        return new RemovePointsAction(viewerRepository);
    }

    public GivePointsAction CreateGivePointsAction(string connectionString)
    {
        var viewerRepository = new ViewerRepository(connectionString, new SchemaBootstrapper());
        return new GivePointsAction(viewerRepository);
    }

    public GetWatchtimeAction CreateGetWatchtimeAction(string connectionString)
    {
        var viewerRepository = new ViewerRepository(connectionString, new SchemaBootstrapper());
        return new GetWatchtimeAction(viewerRepository);
    }

    public StartStreamAction CreateStartStreamAction(string connectionString)
    {
        var watchStreakRepository = new WatchStreakRepository(connectionString, new SchemaBootstrapper());
        return new StartStreamAction(watchStreakRepository);
    }

    public EndStreamAction CreateEndStreamAction(string connectionString)
    {
        var watchStreakRepository = new WatchStreakRepository(connectionString, new SchemaBootstrapper());
        return new EndStreamAction(watchStreakRepository);
    }

    public StartHeistAction CreateStartHeistAction(string connectionString, string? configurationPath = null, string? heistMessageTemplatesPath = null)
    {
        var heistSettings = LoadHeistSettings(configurationPath);
        var heistRepository = new HeistRepository(connectionString, new SchemaBootstrapper());
        var messageComposer = CreateHeistMessageComposer(heistMessageTemplatesPath);
        return new StartHeistAction(heistRepository, heistSettings, messageComposer);
    }

    public JoinHeistAction CreateJoinHeistAction(string connectionString)
    {
        var heistRepository = new HeistRepository(connectionString, new SchemaBootstrapper());
        return new JoinHeistAction(heistRepository);
    }

    public ResolveDueHeistsAction CreateResolveDueHeistsAction(string connectionString, string? configurationPath = null, string? heistMessageTemplatesPath = null)
    {
        var heistSettings = LoadHeistSettings(configurationPath);
        var heistRepository = new HeistRepository(connectionString, new SchemaBootstrapper());
        var messageComposer = CreateHeistMessageComposer(heistMessageTemplatesPath);
        return new ResolveDueHeistsAction(
            heistRepository,
            new HeistChanceCalculator(heistSettings),
            new HeistResolver(heistSettings),
            heistSettings,
            messageComposer);
    }

    private static HeistMessageComposer CreateHeistMessageComposer(string? heistMessageTemplatesPath)
    {
        var loader = new HeistMessageTemplateLoader();
        HeistMessageTemplates templates = loader.Load(heistMessageTemplatesPath);
        return new HeistMessageComposer(templates);
    }

    private static RewardSettings LoadRewardSettings(string? configurationPath)
    {
        if (string.IsNullOrWhiteSpace(configurationPath) || !File.Exists(configurationPath))
        {
            return new RewardSettings();
        }

        var json = File.ReadAllText(configurationPath);
        var configuration = JsonSerializer.Deserialize<RuntimeConfiguration>(json);

        return configuration?.Rewards ?? new RewardSettings();
    }

    private static HeistSettings LoadHeistSettings(string? configurationPath)
    {
        if (string.IsNullOrWhiteSpace(configurationPath) || !File.Exists(configurationPath))
        {
            return new HeistSettings();
        }

        var json = File.ReadAllText(configurationPath);
        var configuration = JsonSerializer.Deserialize<RuntimeConfiguration>(json);

        return configuration?.Heist ?? new HeistSettings();
    }

    private sealed class RuntimeConfiguration
    {
        public RewardSettings? Rewards { get; set; }

        public HeistSettings? Heist { get; set; }
    }
}
