using System.Text.Json;
using TwitchHeists.StreamerBot.Composition;
using TwitchHeists.StreamerBot.Services;

namespace TwitchHeists.StreamerBot.Bridge.Services;

public sealed class BridgeRuntimeFactory
{
    private const string DatabaseFileName = "twitch-heists.db";
    private const string ConfigurationFileName = "appsettings.json";
    private const string HeistMessagesFileName = "heist-messages.json";
    private readonly ActionRuntimeFactory actionRuntimeFactory;

    public BridgeRuntimeFactory()
        : this(new ActionRuntimeFactory())
    {
    }

    public BridgeRuntimeFactory(ActionRuntimeFactory actionRuntimeFactory)
    {
        this.actionRuntimeFactory = actionRuntimeFactory;
    }

    public RefreshCommunityViewersAction CreateRefreshCommunityViewersAction(string installDirectory)
    {
        return actionRuntimeFactory.CreateRefreshCommunityViewersAction(
            BuildConnectionString(installDirectory),
            ResolveConfigurationPath(installDirectory));
    }

    public RecordChatPresenceAction CreateRecordChatPresenceAction(string installDirectory)
    {
        return actionRuntimeFactory.CreateRecordChatPresenceAction(BuildConnectionString(installDirectory));
    }

    public AddPointsAction CreateAddPointsAction(string installDirectory)
    {
        return actionRuntimeFactory.CreateAddPointsAction(BuildConnectionString(installDirectory));
    }

    public RemovePointsAction CreateRemovePointsAction(string installDirectory)
    {
        return actionRuntimeFactory.CreateRemovePointsAction(BuildConnectionString(installDirectory));
    }

    public GivePointsAction CreateGivePointsAction(string installDirectory)
    {
        return actionRuntimeFactory.CreateGivePointsAction(BuildConnectionString(installDirectory));
    }

    public GetWatchtimeAction CreateGetWatchtimeAction(string installDirectory)
    {
        return actionRuntimeFactory.CreateGetWatchtimeAction(BuildConnectionString(installDirectory));
    }

    public StartHeistAction CreateStartHeistAction(string installDirectory)
    {
        return actionRuntimeFactory.CreateStartHeistAction(
            BuildConnectionString(installDirectory),
            ResolveConfigurationPath(installDirectory),
            ResolveHeistMessageTemplatesPath(installDirectory));
    }

    public JoinHeistAction CreateJoinHeistAction(string installDirectory)
    {
        return actionRuntimeFactory.CreateJoinHeistAction(BuildConnectionString(installDirectory));
    }

    public ResolveDueHeistsAction CreateResolveDueHeistsAction(string installDirectory)
    {
        return actionRuntimeFactory.CreateResolveDueHeistsAction(
            BuildConnectionString(installDirectory),
            ResolveConfigurationPath(installDirectory),
            ResolveHeistMessageTemplatesPath(installDirectory));
    }

    private static string BuildConnectionString(string installDirectory)
    {
        var validatedInstallDirectory = ValidateInstallDirectory(installDirectory);
        var dataDirectory = Path.Combine(validatedInstallDirectory, "data");
        Directory.CreateDirectory(dataDirectory);
        var databasePath = Path.Combine(dataDirectory, DatabaseFileName);

        return $@"Data Source={databasePath};Pooling=False";
    }

    private static string? ResolveConfigurationPath(string installDirectory)
    {
        var validatedInstallDirectory = ValidateInstallDirectory(installDirectory);
        var configurationPath = Path.Combine(validatedInstallDirectory, ConfigurationFileName);

        if (!File.Exists(configurationPath))
        {
            return null;
        }

        ValidateConfigurationFile(configurationPath);
        return configurationPath;
    }

    private static string? ResolveHeistMessageTemplatesPath(string installDirectory)
    {
        var validatedInstallDirectory = ValidateInstallDirectory(installDirectory);
        var templatePath = Path.Combine(validatedInstallDirectory, HeistMessagesFileName);
        return File.Exists(templatePath) ? templatePath : null;
    }

    private static string ValidateInstallDirectory(string installDirectory)
    {
        if (string.IsNullOrWhiteSpace(installDirectory))
        {
            throw new ArgumentException("The TwitchHeists install directory is required.", nameof(installDirectory));
        }

        if (!Directory.Exists(installDirectory))
        {
            throw new DirectoryNotFoundException($"The TwitchHeists install directory was not found: {installDirectory}");
        }

        return installDirectory;
    }

    private static void ValidateConfigurationFile(string configurationPath)
    {
        using var stream = File.OpenRead(configurationPath);
        using var document = JsonDocument.Parse(stream);
    }
}
