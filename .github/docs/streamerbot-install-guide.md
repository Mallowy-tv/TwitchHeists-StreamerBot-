# Streamer.bot install guide

This guide uses the bridge assembly so Streamer.bot only needs one manual DLL reference and you do **not** need to add `netstandard.dll`.

## 1. Build the deployment files

Run this from the project root:

```powershell
dotnet build .\TwitchHeists.sln -c Release
```

Use this output folder as your deployment source:

`src\TwitchHeists.StreamerBot.Bridge\bin\Release\net48\`

Keep that full folder together. Do **not** copy only one DLL.

## 2. Copy the bridge output into Streamer.bot

Copy everything from the bridge release folder into:

```text
D:\Streamer Bot\Extensions\TwitchHeist\
```

Your folder should look roughly like this:

```text
D:\Streamer Bot\Extensions\TwitchHeist\
  appsettings.json
  bridge-readme.txt
  heist-messages.json
  Microsoft.Bcl.AsyncInterfaces.dll
  Microsoft.Data.Sqlite.dll
  SQLitePCLRaw.batteries_v2.dll
  SQLitePCLRaw.core.dll
  SQLitePCLRaw.provider.dynamic_cdecl.dll
  System.Buffers.dll
  System.Memory.dll
  System.Numerics.Vectors.dll
  System.Runtime.CompilerServices.Unsafe.dll
  System.Text.Encodings.Web.dll
  System.Text.Json.dll
  System.Threading.Tasks.Extensions.dll
  System.ValueTuple.dll
  TwitchHeists.Core.dll
  TwitchHeists.Data.Sqlite.dll
  TwitchHeists.StreamerBot.Bridge.dll
  TwitchHeists.StreamerBot.dll
  Configuration\
  runtimes\
```

The bridge creates `data\` automatically on first use, but keeping an empty `data\` folder there is also fine. Keep `runtimes\` and any generated support folders exactly as they came out of the build.

## 3. Tune settings if you want

The default `appsettings.json` expects the SQLite database here:

```json
{
  "ConnectionStrings": {
    "TwitchHeists": "Data Source=.\\data\\twitch-heists.db"
  }
}
```

You can also tune these values before first use:

| Setting | Default | Meaning |
|---|---|---|
| `Rewards.RewardInterval` | `00:05:00` | Watchtime payout interval |
| `Rewards.BasePointsPerInterval` | `10` | Base points every 5 minutes |
| `Rewards.Tier1Multiplier` | `1.5` | Tier 1 subscriber multiplier |
| `Rewards.Tier2Multiplier` | `2.0` | Tier 2 subscriber multiplier |
| `Rewards.Tier3Multiplier` | `3.0` | Tier 3 subscriber multiplier |
| `Heist.JoinWindow` | `00:02:00` | How long a heist stays open |
| `Heist.CooldownWindow` | `00:05:00` | Time after results before another heist can start |
| `Heist.OneMinuteReminderThreshold` | `00:01:00` | When to send the 1-minute countdown reminder |
| `Heist.ThirtySecondReminderThreshold` | `00:00:30` | When to send the 30-second countdown reminder |
| `Heist.TenSecondReminderThreshold` | `00:00:10` | When to send the 10-second countdown reminder |
| `Heist.MinimumSuccessChance` | `0.4` | Lowest allowed success chance |
| `Heist.MaximumSuccessChance` | `0.75` | Highest allowed success chance |
| `Heist.MinimumParticipants` | `2` | Minimum joined crew size before a heist can resolve normally |
| `Heist.MinimumJoinAmount` | `1000` | Lowest join stake accepted by `!join` |
| `Heist.WinnerBands` | built-in defaults | Adaptive winner ranges keyed by joined player count |
| `Heist.MaximumNamedResolutionCallouts` | `2` | Max named callouts before the result falls back to summary-only text |
| `Heist.SuccessfulPotMultiplier` | `2.0` | Pot multiplier on success |
| `Raffle.JoinWindow` | `00:02:00` | How long a raffle stays open before draw |
| `Raffle.OneMinuteReminderThreshold` | `00:01:00` | When to send the 1-minute raffle reminder |
| `Raffle.ThirtySecondReminderThreshold` | `00:00:30` | When to send the 30-second raffle reminder |
| `Raffle.TenSecondReminderThreshold` | `00:00:10` | When to send the 10-second raffle reminder |
| `Raffle.WinnerPoints` | `5000` | Default points awarded to each raffle winner when command input omits amount |
| `Raffle.ModeratorPointsLimit` | `5000` | Legacy moderator cap setting (raffle joins are free) |

`heist-messages.json` sits beside `appsettings.json` in the same install folder and controls all heist chat output without rebuilding the project.

By default, crews below `Heist.MinimumParticipants` resolve through the insufficient-crew path and get their reserved stakes refunded. Larger crews use the built-in adaptive winner bands instead of a fixed winner count.

Supported message groups:

| Group | Used for |
|---|---|
| `startMessages` | `!heist` success text |
| `cooldownMessages` | `!heist` rejection while cooldown is active |
| `reminderMessages` | 1-minute, 30-second, and 10-second countdown messages |
| `insufficientBalanceMessages` | `!heist` / `!join` failure text when a viewer does not have enough points for the stake |
| `alreadyJoinedMessages` | `!join` failure text when a viewer has already joined the open heist |
| `minimumJoinAmountMessages` | `!join` failure text when a viewer is below the minimum stake floor |
| `insufficientCrewMessages` | Result text when the crew is too small and everyone is refunded |
| `successHeadlines` | Opening line for a successful resolved heist |
| `failureHeadlines` | Opening line for a failed resolved heist |
| `successCallouts` | Winner-focused follow-up lines |
| `failureCallouts` | Loser-focused follow-up lines |
| `sacrificeCallouts` | Mixed winner/loser lines on successful heists with casualties |
| `resultSummaries` | Final summary sentence with totals and success chance |

Supported placeholders:

| Placeholder | Meaning |
|---|---|
| `{starter}` | Username that started the heist |
| `{stake}` | Start amount entered on `!heist` |
| `{joinWindow}` | Human-readable open window such as `2 minutes` |
| `{cooldownRemaining}` | Remaining cooldown such as `2m 0s` |
| `{countdown}` | Reminder text such as `1 minute` or `10 seconds` |
| `{pot}` | Current open pot before resolution |
| `{participantCount}` | Number of joined viewers |
| `{winner}` | Winner username for a callout |
| `{loser}` | Loser username for a callout |
| `{payout}` | Winner payout amount |
| `{winnerCount}` | Number of winners |
| `{loserCount}` | Number of losers |
| `{resolvedPot}` | Final resolved pot |
| `{successChancePercent}` | Final success chance like `68.58%` |
| `{viewer}` | Viewer username used in join-failure messages |
| `{minimumJoinAmount}` | Minimum stake floor used in minimum-join messages |

Edit the arrays in `heist-messages.json`, save the file, and the next heist action run will use the new wording. You do not need to rebuild the DLLs just to change chat lines.

For large crews, TwitchHeists intentionally keeps the final result compact: headline, a small number of named callouts, then the summary. If you want even shorter results, lower `Heist.MaximumNamedResolutionCallouts`.

## 4. Keep Streamer.bot references minimal

For each **Execute C# Code** action:

1. Keep the default framework references such as `mscorlib.dll` and `System.dll`.
2. Remove any manual `TwitchHeists.*.dll` references from the action.
3. Do **not** manually add `netstandard.dll`.

Streamer.bot can compile against an external DLL reference but still fail to load that DLL at runtime. The snippets below avoid that by loading `TwitchHeists.StreamerBot.Bridge.dll` directly from your install folder with reflection.

## 5. Use the runtime-safe loader pattern in your C# actions

Each Streamer.bot action should:

1. point at the same install folder;
2. register an assembly resolver **before** loading the bridge;
3. load `TwitchHeists.StreamerBot.Bridge.dll` from disk with `Assembly.LoadFrom(...)`;
4. create the bridge models and call `BridgeActions` through reflection.

The snippets below use real Streamer.bot args instead of placeholder usernames:

1. sender info comes from the standard Twitch chat args `userName`, `displayName`, and `userId`;
2. command amounts come from `input0`, `input1`, and so on;
3. target lookups should use a **Twitch -> User -> Get User Info for Target** sub-action before the Execute C# step, with **User Login** set to the actual target slot (`%input0%` for a dedicated `!points add` command, or `%input1%` if you use one shared `!points` command and the first token is `add/remove/give`). That gives you `targetUserName`, `targetUser`, and `targetUserId`.
4. subscriber multipliers only work if the chat/community snippets pass a real subscriber tier into the bridge. Leaving `SubscriberTier` hardcoded to `0` makes everyone look unsubscribed.
5. the sample `CPH.LogInfo(...)` calls use `[TwitchHeists][Area]` prefixes so you can filter Streamer.bot logs down to this project while debugging.

In every snippet below:

1. keep `private const string InstallDir = ...`;
2. add `private static bool resolverRegistered;` inside `CPHInline`;
3. call `RegisterAssemblyResolver();` as the **first line** inside `Execute()`.

## 6. Create the Streamer.bot actions

### Action 1: Community refresh

Run this on a **5-minute timer** after you pull the Community tab snapshot.

Subscriber tier values are:

- `0` = not subscribed
- `1` = Tier 1
- `2` = Tier 2
- `3` = Tier 3

```csharp
using System;
using System.Globalization;
using System.IO;
using System.Reflection;

public class CPHInline
{
    private const string InstallDir = @"D:\Streamer Bot\Extensions\TwitchHeist";
    private static bool resolverRegistered;

    public bool Execute()
    {
        RegisterAssemblyResolver();
        var bridgeAssembly = LoadBridgeAssembly();
        var bridgeActions = CreateBridgeActions(bridgeAssembly);
        var snapshot = CreateArray(bridgeAssembly, "TwitchHeists.StreamerBot.Bridge.Models.BridgeCommunityViewer", 1);
        var viewer = CreateInstance(bridgeAssembly, "TwitchHeists.StreamerBot.Bridge.Models.BridgeCommunityViewer");

        SetProperty(viewer, "TwitchUserId", "<viewer twitch user id>");
        SetProperty(viewer, "Username", "<viewer username>");
        SetProperty(viewer, "DisplayName", "<viewer display name>");
        SetProperty(viewer, "SubscriberTier", GetSubscriberTier());
        snapshot.SetValue(viewer, 0);

        var result = InvokeBridge(bridgeActions, "RefreshCommunityViewers", InstallDir, DateTimeOffset.UtcNow, snapshot);
        var message = GetStringProperty(result, "Message");

        CPH.LogInfo("[TwitchHeists][CommunityRefresh] " + message);
        return true;
    }

    private static void RegisterAssemblyResolver()
    {
        if (resolverRegistered)
        {
            return;
        }

        AppDomain.CurrentDomain.AssemblyResolve += ResolveFromInstallDir;
        resolverRegistered = true;
    }

    private static Assembly ResolveFromInstallDir(object sender, ResolveEventArgs args)
    {
        var assemblyName = new AssemblyName(args.Name).Name + ".dll";
        var assemblyPath = Path.Combine(InstallDir, assemblyName);

        if (File.Exists(assemblyPath))
        {
            return Assembly.LoadFrom(assemblyPath);
        }

        return null;
    }

    private static Assembly LoadBridgeAssembly()
    {
        var bridgePath = Path.Combine(InstallDir, "TwitchHeists.StreamerBot.Bridge.dll");
        if (!File.Exists(bridgePath))
        {
            throw new FileNotFoundException("Bridge DLL not found.", bridgePath);
        }

        return Assembly.LoadFrom(bridgePath);
    }

    private static object CreateBridgeActions(Assembly bridgeAssembly)
    {
        var bridgeActionsType = bridgeAssembly.GetType("TwitchHeists.StreamerBot.Bridge.Services.BridgeActions", true);
        return Activator.CreateInstance(bridgeActionsType);
    }

    private static object CreateInstance(Assembly bridgeAssembly, string typeName)
    {
        var type = bridgeAssembly.GetType(typeName, true);
        return Activator.CreateInstance(type);
    }

    private static Array CreateArray(Assembly bridgeAssembly, string elementTypeName, int length)
    {
        var elementType = bridgeAssembly.GetType(elementTypeName, true);
        return Array.CreateInstance(elementType, length);
    }

    private static void SetProperty(object target, string propertyName, object value)
    {
        target.GetType().GetProperty(propertyName).SetValue(target, value, null);
    }

    private static object InvokeBridge(object bridgeActions, string methodName, params object[] arguments)
    {
        return bridgeActions.GetType().GetMethod(methodName).Invoke(bridgeActions, arguments);
    }

    private static string GetStringProperty(object target, string propertyName)
    {
        var value = target.GetType().GetProperty(propertyName).GetValue(target, null);
        return value == null ? string.Empty : value.ToString();
    }

    private string GetSenderUsername()
    {
        return GetRequiredStringArg("userName", "user");
    }

    private string GetSenderDisplayName(string fallbackUsername)
    {
        return GetOptionalStringArg("displayName", "user") ?? fallbackUsername;
    }

    private string? GetSenderUserId()
    {
        return GetOptionalStringArg("userId");
    }

    private decimal GetRequiredDecimalArg(params string[] argNames)
    {
        var rawValue = GetRequiredStringArg(argNames);
        if (decimal.TryParse(rawValue, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsedAmount))
        {
            return parsedAmount;
        }

        if (decimal.TryParse(rawValue, NumberStyles.Number, CultureInfo.CurrentCulture, out parsedAmount))
        {
            return parsedAmount;
        }

        throw new InvalidOperationException($"'{rawValue}' is not a valid amount.");
    }

    private string GetRequiredStringArg(params string[] argNames)
    {
        return GetOptionalStringArg(argNames)
            ?? throw new InvalidOperationException(
                $"Missing required Streamer.bot argument. Tried: {string.Join(", ", argNames)}.");
    }

    private string? GetOptionalStringArg(params string[] argNames)
    {
        foreach (var argName in argNames)
        {
            if (CPH.TryGetArg<string>(argName, out var value) && !string.IsNullOrWhiteSpace(value))
            {
                return value.Trim();
            }
        }

        return null;
    }

    private int GetSubscriberTier()
    {
        var rawTier = GetOptionalStringArg("subscriberTier", "subscriptionTier", "subTier", "tier");
        if (!string.IsNullOrWhiteSpace(rawTier))
        {
            return ParseSubscriberTier(rawTier);
        }

        return GetOptionalBooleanArg("isSubscribed", "subscribed") ? 1 : 0;
    }

    private bool GetOptionalBooleanArg(params string[] argNames)
    {
        foreach (var argName in argNames)
        {
            if (CPH.TryGetArg<bool>(argName, out var boolValue))
            {
                return boolValue;
            }

            if (CPH.TryGetArg<string>(argName, out var stringValue) && bool.TryParse(stringValue, out boolValue))
            {
                return boolValue;
            }
        }

        return false;
    }

    private static int ParseSubscriberTier(string rawTier)
    {
        return rawTier.Trim().ToLowerInvariant() switch
        {
            "1" or "1000" or "prime" or "tier1" => 1,
            "2" or "2000" or "tier2" => 2,
            "3" or "3000" or "tier3" => 3,
            _ => 0
        };
    }
}
```

### Action 2: Chat presence fallback

Run this on **every chat message** so chatters count until the next Community refresh.

This snippet includes the shared username, user ID, and numeric input handlers used by the other chat and command actions.

```csharp
using System;
using System.Globalization;
using System.IO;
using System.Reflection;

public class CPHInline
{
    private const string InstallDir = @"D:\Streamer Bot\Extensions\TwitchHeist";
    private static bool resolverRegistered;

    public bool Execute()
    {
        RegisterAssemblyResolver();
        var bridgeAssembly = LoadBridgeAssembly();
        var bridgeActions = CreateBridgeActions(bridgeAssembly);
        var username = GetSenderUsername();
        var nowUtc = DateTimeOffset.UtcNow;
        var nextRefreshBoundaryUtc = nowUtc
            .AddMinutes(5 - (nowUtc.Minute % 5))
            .AddSeconds(-nowUtc.Second);
        var presence = CreateInstance(bridgeAssembly, "TwitchHeists.StreamerBot.Bridge.Models.BridgeChatPresence");

        SetProperty(presence, "TwitchUserId", GetSenderUserId());
        SetProperty(presence, "Username", username);
        SetProperty(presence, "DisplayName", GetSenderDisplayName(username));
        SetProperty(presence, "SubscriberTier", GetSubscriberTier());
        SetProperty(presence, "MessageReceivedAtUtc", nowUtc);

        var result = InvokeBridge(bridgeActions, "RecordChatPresence", InstallDir, presence, nextRefreshBoundaryUtc);
        var message = GetStringProperty(result, "Message");

        CPH.LogInfo("[TwitchHeists][ChatPresence] " + message);
        return true;
    }

    private static void RegisterAssemblyResolver()
    {
        if (resolverRegistered)
        {
            return;
        }

        AppDomain.CurrentDomain.AssemblyResolve += ResolveFromInstallDir;
        resolverRegistered = true;
    }

    private static Assembly ResolveFromInstallDir(object sender, ResolveEventArgs args)
    {
        var assemblyName = new AssemblyName(args.Name).Name + ".dll";
        var assemblyPath = Path.Combine(InstallDir, assemblyName);

        if (File.Exists(assemblyPath))
        {
            return Assembly.LoadFrom(assemblyPath);
        }

        return null;
    }

    private static Assembly LoadBridgeAssembly()
    {
        var bridgePath = Path.Combine(InstallDir, "TwitchHeists.StreamerBot.Bridge.dll");
        if (!File.Exists(bridgePath))
        {
            throw new FileNotFoundException("Bridge DLL not found.", bridgePath);
        }

        return Assembly.LoadFrom(bridgePath);
    }

    private static object CreateBridgeActions(Assembly bridgeAssembly)
    {
        var bridgeActionsType = bridgeAssembly.GetType("TwitchHeists.StreamerBot.Bridge.Services.BridgeActions", true);
        return Activator.CreateInstance(bridgeActionsType);
    }

    private static object InvokeBridge(object bridgeActions, string methodName, params object[] arguments)
    {
        return bridgeActions.GetType().GetMethod(methodName).Invoke(bridgeActions, arguments);
    }

    private static string GetStringProperty(object target, string propertyName)
    {
        var value = target.GetType().GetProperty(propertyName).GetValue(target, null);
        return value == null ? string.Empty : value.ToString();
    }

    private string GetSenderUsername()
    {
        return GetRequiredStringArg("userName", "user");
    }

    private string GetSenderDisplayName(string fallbackUsername)
    {
        return GetOptionalStringArg("displayName", "user") ?? fallbackUsername;
    }

    private string? GetSenderUserId()
    {
        return GetOptionalStringArg("userId");
    }

    private string GetResolvedTargetUsername(int inputIndex)
    {
        return GetOptionalStringArg("targetUserName", $"input{inputIndex}")
            ?? throw new InvalidOperationException(
                $"Missing target username. Add a Get User Info for Target sub-action with %input{inputIndex}% or pass the username directly.");
    }

    private string GetResolvedTargetDisplayName(string fallbackUsername)
    {
        return GetOptionalStringArg("targetUser", "targetUserName") ?? fallbackUsername;
    }

    private string? GetResolvedTargetUserId()
    {
        return GetOptionalStringArg("targetUserId");
    }

    private decimal GetRequiredDecimalArg(params string[] argNames)
    {
        var rawValue = GetRequiredStringArg(argNames);
        if (decimal.TryParse(rawValue, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsedAmount))
        {
            return parsedAmount;
        }

        if (decimal.TryParse(rawValue, NumberStyles.Number, CultureInfo.CurrentCulture, out parsedAmount))
        {
            return parsedAmount;
        }

        throw new InvalidOperationException($"'{rawValue}' is not a valid amount.");
    }

    private string GetRequiredStringArg(params string[] argNames)
    {
        return GetOptionalStringArg(argNames)
            ?? throw new InvalidOperationException(
                $"Missing required Streamer.bot argument. Tried: {string.Join(", ", argNames)}.");
    }

    private string? GetOptionalStringArg(params string[] argNames)
    {
        foreach (var argName in argNames)
        {
            if (CPH.TryGetArg<string>(argName, out var value) && !string.IsNullOrWhiteSpace(value))
            {
                return value.Trim();
            }
        }

        return null;
    }

    private int GetSubscriberTier()
    {
        var rawTier = GetOptionalStringArg("subscriberTier", "subscriptionTier", "subTier", "tier");
        if (!string.IsNullOrWhiteSpace(rawTier))
        {
            return ParseSubscriberTier(rawTier);
        }

        return GetOptionalBooleanArg("isSubscribed", "subscribed") ? 1 : 0;
    }

    private bool GetOptionalBooleanArg(params string[] argNames)
    {
        foreach (var argName in argNames)
        {
            if (CPH.TryGetArg<bool>(argName, out var boolValue))
            {
                return boolValue;
            }

            if (CPH.TryGetArg<string>(argName, out var stringValue) && bool.TryParse(stringValue, out boolValue))
            {
                return boolValue;
            }
        }

        return false;
    }

    private static int ParseSubscriberTier(string rawTier)
    {
        return rawTier.Trim().ToLowerInvariant() switch
        {
            "1" or "1000" or "prime" or "tier1" => 1,
            "2" or "2000" or "tier2" => 2,
            "3" or "3000" or "tier3" => 3,
            _ => 0
        };
    }

    private int GetPointsTargetInputIndex()
    {
        return GetPointsVerbOffset();
    }

    private string GetPointsAmountArgName()
    {
        return $"input{GetPointsVerbOffset() + 1}";
    }

    private int GetPointsVerbOffset()
    {
        var verb = GetOptionalStringArg("input0");
        return string.Equals(verb, "add", StringComparison.OrdinalIgnoreCase)
            || string.Equals(verb, "remove", StringComparison.OrdinalIgnoreCase)
            || string.Equals(verb, "give", StringComparison.OrdinalIgnoreCase)
            ? 1
            : 0;
    }
}
```

### Action 3: Heist start

Run this when chat matches `!heist <amount>`.

This action should usually send the result back to chat.

Successful starts now return a message from `heist-messages.json`, using placeholders like `{starter}`, `{stake}`, and `{joinWindow}`. If the previous round already finished and the 5-minute cooldown is still active, the action returns a `cooldownMessages` entry with `{cooldownRemaining}` filled in instead.

Use the same helper members from **Action 2** so the chatter username and Twitch user ID come straight from Streamer.bot.

```csharp
using System;
using System.Globalization;
using System.IO;
using System.Reflection;

public class CPHInline
{
    private const string InstallDir = @"D:\Streamer Bot\Extensions\TwitchHeist";
    private static bool resolverRegistered;

    public bool Execute()
    {
        RegisterAssemblyResolver();
        var bridgeAssembly = LoadBridgeAssembly();
        var bridgeActions = CreateBridgeActions(bridgeAssembly);
        var username = GetSenderUsername();
        var stakeAmount = GetRequiredDecimalArg("input0");
        var command = CreateInstance(bridgeAssembly, "TwitchHeists.StreamerBot.Bridge.Models.BridgeHeistCommand");

        SetProperty(command, "TwitchUserId", GetSenderUserId());
        SetProperty(command, "Username", username);
        SetProperty(command, "DisplayName", GetSenderDisplayName(username));
        SetProperty(command, "StakeAmount", stakeAmount);
        SetProperty(command, "OccurredAtUtc", DateTimeOffset.UtcNow);

        var result = InvokeBridge(bridgeActions, "StartHeist", InstallDir, command);
        var message = GetStringProperty(result, "Message");

        if (!string.IsNullOrWhiteSpace(message))
        {
            CPH.SendMessage(message);
        }

        CPH.LogInfo("[TwitchHeists][HeistStart] " + message);
        return true;
    }

    private static void RegisterAssemblyResolver()
    {
        if (resolverRegistered)
        {
            return;
        }

        AppDomain.CurrentDomain.AssemblyResolve += ResolveFromInstallDir;
        resolverRegistered = true;
    }

    private static Assembly ResolveFromInstallDir(object sender, ResolveEventArgs args)
    {
        var assemblyName = new AssemblyName(args.Name).Name + ".dll";
        var assemblyPath = Path.Combine(InstallDir, assemblyName);

        if (File.Exists(assemblyPath))
        {
            return Assembly.LoadFrom(assemblyPath);
        }

        return null;
    }

    private static Assembly LoadBridgeAssembly()
    {
        var bridgePath = Path.Combine(InstallDir, "TwitchHeists.StreamerBot.Bridge.dll");
        if (!File.Exists(bridgePath))
        {
            throw new FileNotFoundException("Bridge DLL not found.", bridgePath);
        }

        return Assembly.LoadFrom(bridgePath);
    }

    private static object CreateBridgeActions(Assembly bridgeAssembly)
    {
        var bridgeActionsType = bridgeAssembly.GetType("TwitchHeists.StreamerBot.Bridge.Services.BridgeActions", true);
        return Activator.CreateInstance(bridgeActionsType);
    }

    private static object CreateInstance(Assembly bridgeAssembly, string typeName)
    {
        var type = bridgeAssembly.GetType(typeName, true);
        return Activator.CreateInstance(type);
    }

    private static void SetProperty(object target, string propertyName, object value)
    {
        target.GetType().GetProperty(propertyName).SetValue(target, value, null);
    }

    private static object InvokeBridge(object bridgeActions, string methodName, params object[] arguments)
    {
        return bridgeActions.GetType().GetMethod(methodName).Invoke(bridgeActions, arguments);
    }

    private static string GetStringProperty(object target, string propertyName)
    {
        var value = target.GetType().GetProperty(propertyName).GetValue(target, null);
        return value == null ? string.Empty : value.ToString();
    }

    private string GetSenderUsername()
    {
        return GetRequiredStringArg("userName", "user");
    }

    private string GetSenderDisplayName(string fallbackUsername)
    {
        return GetOptionalStringArg("displayName", "user") ?? fallbackUsername;
    }

    private string? GetSenderUserId()
    {
        return GetOptionalStringArg("userId");
    }

    private decimal GetRequiredDecimalArg(params string[] argNames)
    {
        var rawValue = GetRequiredStringArg(argNames);
        if (decimal.TryParse(rawValue, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsedAmount))
        {
            return parsedAmount;
        }

        if (decimal.TryParse(rawValue, NumberStyles.Number, CultureInfo.CurrentCulture, out parsedAmount))
        {
            return parsedAmount;
        }

        throw new InvalidOperationException($"'{rawValue}' is not a valid amount.");
    }

    private string GetRequiredStringArg(params string[] argNames)
    {
        return GetOptionalStringArg(argNames)
            ?? throw new InvalidOperationException(
                $"Missing required Streamer.bot argument. Tried: {string.Join(", ", argNames)}.");
    }

    private string? GetOptionalStringArg(params string[] argNames)
    {
        foreach (var argName in argNames)
        {
            if (CPH.TryGetArg<string>(argName, out var value) && !string.IsNullOrWhiteSpace(value))
            {
                return value.Trim();
            }
        }

        return null;
    }
}
```

### Action 15: Points lookup

Run this when chat matches `!points` or `!points <user>`.

This command returns the current points balance for the sender by default, or for the tagged target when a username is supplied.

If you want `!points <user>` to resolve a real Twitch account instead of a raw typed string, add a **Get User Info for Target** sub-action before this Execute C# step with **User Login** = `%input0%`.

This is a **full standalone `CPHInline` class**. Copy the entire block exactly as shown.

```csharp

### Action 4: Heist join

Run this when chat matches `!join <amount>`.

This action should usually send the result back to chat.

Use the same helper members from **Action 2** so the chatter username and Twitch user ID come straight from Streamer.bot.

```csharp
using System;
using System.Globalization;
using System.IO;
using System.Reflection;

public class CPHInline
{
    private const string InstallDir = @"D:\Streamer Bot\Extensions\TwitchHeist";
    private static bool resolverRegistered;

    public bool Execute()
    {
        RegisterAssemblyResolver();
        var bridgeAssembly = LoadBridgeAssembly();
        var bridgeActions = CreateBridgeActions(bridgeAssembly);
        var username = GetSenderUsername();
        var stakeAmount = GetRequiredDecimalArg("input0");
        var command = CreateInstance(bridgeAssembly, "TwitchHeists.StreamerBot.Bridge.Models.BridgeHeistCommand");

        SetProperty(command, "TwitchUserId", GetSenderUserId());
        SetProperty(command, "Username", username);
        SetProperty(command, "DisplayName", GetSenderDisplayName(username));
        SetProperty(command, "StakeAmount", stakeAmount);
        SetProperty(command, "OccurredAtUtc", DateTimeOffset.UtcNow);

        var result = InvokeBridge(bridgeActions, "JoinHeist", InstallDir, command);
        var message = GetStringProperty(result, "Message");

        if (!string.IsNullOrWhiteSpace(message))
        {
            CPH.SendMessage(message);
        }

        CPH.LogInfo("[TwitchHeists][HeistJoin] " + message);
        return true;
    }

    private static void RegisterAssemblyResolver()
    {
        if (resolverRegistered)
        {
            return;
        }

        AppDomain.CurrentDomain.AssemblyResolve += ResolveFromInstallDir;
        resolverRegistered = true;
    }

    private static Assembly ResolveFromInstallDir(object sender, ResolveEventArgs args)
    {
        var assemblyName = new AssemblyName(args.Name).Name + ".dll";
        var assemblyPath = Path.Combine(InstallDir, assemblyName);

        if (File.Exists(assemblyPath))
        {
            return Assembly.LoadFrom(assemblyPath);
        }

        return null;
    }

    private static Assembly LoadBridgeAssembly()
    {
        var bridgePath = Path.Combine(InstallDir, "TwitchHeists.StreamerBot.Bridge.dll");
        if (!File.Exists(bridgePath))
        {
            throw new FileNotFoundException("Bridge DLL not found.", bridgePath);
        }

        return Assembly.LoadFrom(bridgePath);
    }

    private static object CreateBridgeActions(Assembly bridgeAssembly)
    {
        var bridgeActionsType = bridgeAssembly.GetType("TwitchHeists.StreamerBot.Bridge.Services.BridgeActions", true);
        return Activator.CreateInstance(bridgeActionsType);
    }

    private static object CreateInstance(Assembly bridgeAssembly, string typeName)
    {
        var type = bridgeAssembly.GetType(typeName, true);
        return Activator.CreateInstance(type);
    }

    private static void SetProperty(object target, string propertyName, object value)
    {
        target.GetType().GetProperty(propertyName).SetValue(target, value, null);
    }

    private static object InvokeBridge(object bridgeActions, string methodName, params object[] arguments)
    {
        return bridgeActions.GetType().GetMethod(methodName).Invoke(bridgeActions, arguments);
    }

    private static string GetStringProperty(object target, string propertyName)
    {
        var value = target.GetType().GetProperty(propertyName).GetValue(target, null);
        return value == null ? string.Empty : value.ToString();
    }

    private string GetSenderUsername()
    {
        return GetRequiredStringArg("userName", "user");
    }

    private string GetSenderDisplayName(string fallbackUsername)
    {
        return GetOptionalStringArg("displayName", "user") ?? fallbackUsername;
    }

    private string? GetSenderUserId()
    {
        return GetOptionalStringArg("userId");
    }

    private decimal GetRequiredDecimalArg(params string[] argNames)
    {
        var rawValue = GetRequiredStringArg(argNames);
        if (decimal.TryParse(rawValue, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsedAmount))
        {
            return parsedAmount;
        }

        if (decimal.TryParse(rawValue, NumberStyles.Number, CultureInfo.CurrentCulture, out parsedAmount))
        {
            return parsedAmount;
        }

        throw new InvalidOperationException($"'{rawValue}' is not a valid amount.");
    }

    private string GetRequiredStringArg(params string[] argNames)
    {
        return GetOptionalStringArg(argNames)
            ?? throw new InvalidOperationException(
                $"Missing required Streamer.bot argument. Tried: {string.Join(", ", argNames)}.");
    }

    private string? GetOptionalStringArg(params string[] argNames)
    {
        foreach (var argName in argNames)
        {
            if (CPH.TryGetArg<string>(argName, out var value) && !string.IsNullOrWhiteSpace(value))
            {
                return value.Trim();
            }
        }

        return null;
    }
}
```

### Action 5: Heist resolution

Run this on a short timer such as every **10 to 30 seconds**.

This timer action now handles all post-start heist chat output:

1. the **1-minute** reminder;
2. the **30-second** reminder;
3. the **10-second** reminder;
4. the final result message.

If you want those reminders and the resolved outcome announced in chat, send the returned message here too and keep this timer running continuously. The wording comes from `heist-messages.json`, so you can tune the flavor later without changing the code. That same timer now covers insufficient-crew refunds as well as normal win/loss resolutions.

```csharp
using System;
using System.IO;
using System.Reflection;

public class CPHInline
{
    private const string InstallDir = @"D:\Streamer Bot\Extensions\TwitchHeist";
    private static bool resolverRegistered;

    public bool Execute()
    {
        RegisterAssemblyResolver();
        var bridgeAssembly = LoadBridgeAssembly();
        var bridgeActions = CreateBridgeActions(bridgeAssembly);
        var result = InvokeBridge(bridgeActions, "ResolveDueHeists", InstallDir, DateTimeOffset.UtcNow);
        var message = GetStringProperty(result, "Message");

        if (!string.IsNullOrWhiteSpace(message) && message != "No due heists to resolve.")
        {
            CPH.SendMessage(message);
        }

        CPH.LogInfo("[TwitchHeists][HeistResolve] " + message);
        return true;
    }

    private static void RegisterAssemblyResolver()
    {
        if (resolverRegistered)
        {
            return;
        }

        AppDomain.CurrentDomain.AssemblyResolve += ResolveFromInstallDir;
        resolverRegistered = true;
    }

    private static Assembly ResolveFromInstallDir(object sender, ResolveEventArgs args)
    {
        var assemblyName = new AssemblyName(args.Name).Name + ".dll";
        var assemblyPath = Path.Combine(InstallDir, assemblyName);

        if (File.Exists(assemblyPath))
        {
            return Assembly.LoadFrom(assemblyPath);
        }

        return null;
    }

    private static Assembly LoadBridgeAssembly()
    {
        var bridgePath = Path.Combine(InstallDir, "TwitchHeists.StreamerBot.Bridge.dll");
        if (!File.Exists(bridgePath))
        {
            throw new FileNotFoundException("Bridge DLL not found.", bridgePath);
        }

        return Assembly.LoadFrom(bridgePath);
    }

    private static object CreateBridgeActions(Assembly bridgeAssembly)
    {
        var bridgeActionsType = bridgeAssembly.GetType("TwitchHeists.StreamerBot.Bridge.Services.BridgeActions", true);
        return Activator.CreateInstance(bridgeActionsType);
    }

    private static object InvokeBridge(object bridgeActions, string methodName, params object[] arguments)
    {
        return bridgeActions.GetType().GetMethod(methodName).Invoke(bridgeActions, arguments);
    }

    private static string GetStringProperty(object target, string propertyName)
    {
        var value = target.GetType().GetProperty(propertyName).GetValue(target, null);
        return value == null ? string.Empty : value.ToString();
    }
}
```

### Action 6: Points add

Run this when chat matches `!points add <user> <amount>`.

Set the Streamer.bot command permissions so only moderators can trigger it.

Add a **Get User Info for Target** sub-action before this Execute C# step with **User Login** = `%input0%` for a dedicated `!points add` command, or `%input1%` for a shared `!points` command.

If the literal target is `all`, skip the target lookup sub-action. `!points add all <amount>` awards that amount to every viewer currently active in TwitchHeists presence tracking and returns a summary message instead of per-user balances.

This is a **full standalone `CPHInline` class**. Copy the entire block exactly as shown.

```csharp
using System;
using System.Globalization;
using System.IO;
using System.Reflection;

public class CPHInline
{
    private const string InstallDir = @"D:\Streamer Bot\Extensions\TwitchHeist";
    private static bool resolverRegistered;

public bool Execute()
{
    RegisterAssemblyResolver();
    var bridgeAssembly = LoadBridgeAssembly();
    var bridgeActions = CreateBridgeActions(bridgeAssembly);
    var command = CreateInstance(bridgeAssembly, "TwitchHeists.StreamerBot.Bridge.Models.BridgePointsCommand");
    var targetUsername = GetResolvedTargetUsername(GetPointsTargetInputIndex());
    var targetUserId = GetResolvedTargetUserId();
    var targetDisplayName = GetResolvedTargetDisplayName(targetUsername);
    var amount = GetRequiredDecimalArg(GetPointsAmountArgName());

    SetProperty(command, "TargetTwitchUserId", targetUserId);
    SetProperty(command, "TargetUsername", targetUsername);
    SetProperty(command, "TargetDisplayName", targetDisplayName);
    SetProperty(command, "Amount", amount);
    SetProperty(command, "OccurredAtUtc", DateTimeOffset.UtcNow);

    var result = InvokeBridge(bridgeActions, "AddPoints", InstallDir, command);
    var message = GetStringProperty(result, "Message");

    if (!string.IsNullOrWhiteSpace(message))
    {
        CPH.SendMessage(message);
    }

    CPH.LogInfo("[TwitchHeists][PointsAdd] " + message);
    return true;
}

    private static void RegisterAssemblyResolver()
    {
        if (resolverRegistered)
        {
            return;
        }

        AppDomain.CurrentDomain.AssemblyResolve += ResolveFromInstallDir;
        resolverRegistered = true;
    }

    private static Assembly ResolveFromInstallDir(object sender, ResolveEventArgs args)
    {
        var assemblyName = new AssemblyName(args.Name).Name + ".dll";
        var assemblyPath = Path.Combine(InstallDir, assemblyName);

        if (File.Exists(assemblyPath))
        {
            return Assembly.LoadFrom(assemblyPath);
        }

        return null;
    }

    private static Assembly LoadBridgeAssembly()
    {
        var bridgePath = Path.Combine(InstallDir, "TwitchHeists.StreamerBot.Bridge.dll");
        if (!File.Exists(bridgePath))
        {
            throw new FileNotFoundException("Bridge DLL not found.", bridgePath);
        }

        return Assembly.LoadFrom(bridgePath);
    }

    private static object CreateBridgeActions(Assembly bridgeAssembly)
    {
        var bridgeActionsType = bridgeAssembly.GetType("TwitchHeists.StreamerBot.Bridge.Services.BridgeActions", true);
        return Activator.CreateInstance(bridgeActionsType);
    }

    private static object CreateInstance(Assembly bridgeAssembly, string typeName)
    {
        var type = bridgeAssembly.GetType(typeName, true);
        return Activator.CreateInstance(type);
    }

    private static void SetProperty(object target, string propertyName, object value)
    {
        target.GetType().GetProperty(propertyName).SetValue(target, value, null);
    }

    private static object InvokeBridge(object bridgeActions, string methodName, params object[] arguments)
    {
        return bridgeActions.GetType().GetMethod(methodName).Invoke(bridgeActions, arguments);
    }

    private static string GetStringProperty(object target, string propertyName)
    {
        var value = target.GetType().GetProperty(propertyName).GetValue(target, null);
        return value == null ? string.Empty : value.ToString();
    }

    private string GetResolvedTargetUsername(int inputIndex)
    {
        var username = GetOptionalStringArg("targetUserName", "targetUserName", "input" + inputIndex);
        if (!string.IsNullOrWhiteSpace(username))
        {
            return username;
        }

        throw new InvalidOperationException(
            "Missing target username. Add a Get User Info for Target sub-action or pass the username directly.");
    }

    private string GetResolvedTargetDisplayName(string fallbackUsername)
    {
        var displayName = GetOptionalStringArg("targetUser", "targetUserName");
        return string.IsNullOrWhiteSpace(displayName) ? fallbackUsername : displayName;
    }

    private string? GetResolvedTargetUserId()
    {
        return GetOptionalStringArg("targetUserId");
    }

    private decimal GetRequiredDecimalArg(string argName)
    {
        var rawValue = GetRequiredStringArg(argName);
        decimal parsedAmount;
        if (decimal.TryParse(rawValue, NumberStyles.Number, CultureInfo.InvariantCulture, out parsedAmount))
        {
            return parsedAmount;
        }

        if (decimal.TryParse(rawValue, NumberStyles.Number, CultureInfo.CurrentCulture, out parsedAmount))
        {
            return parsedAmount;
        }

        throw new InvalidOperationException("'" + rawValue + "' is not a valid amount.");
    }

    private string GetRequiredStringArg(string argName)
    {
        var value = GetOptionalStringArg(argName);
        if (!string.IsNullOrWhiteSpace(value))
        {
            return value;
        }

        throw new InvalidOperationException("Missing required Streamer.bot argument: " + argName + ".");
    }

    private string GetOptionalStringArg(params string[] argNames)
    {
        foreach (var argName in argNames)
        {
            string value;
            if (CPH.TryGetArg<string>(argName, out value) && !string.IsNullOrWhiteSpace(value))
            {
                return value.Trim();
            }
        }

        return null;
    }

    private int GetPointsTargetInputIndex()
    {
        return GetPointsVerbOffset();
    }

    private string GetPointsAmountArgName()
    {
        return "input" + (GetPointsVerbOffset() + 1);
    }

    private int GetPointsVerbOffset()
    {
        var verb = GetOptionalStringArg("input0");
        if (string.Equals(verb, "add", StringComparison.OrdinalIgnoreCase)
            || string.Equals(verb, "remove", StringComparison.OrdinalIgnoreCase)
            || string.Equals(verb, "give", StringComparison.OrdinalIgnoreCase))
        {
            return 1;
        }

        return 0;
    }
}
```

### Action 7: Points remove

Run this when chat matches `!points remove <user> <amount>`.

Set the Streamer.bot command permissions so only moderators can trigger it.

Add a **Get User Info for Target** sub-action before this Execute C# step with **User Login** = `%input0%` for a dedicated `!points remove` command, or `%input1%` for a shared `!points` command.

If the literal target is `all`, skip the target lookup sub-action. `!points remove all <amount>` removes that amount from every currently active viewer, clamps each viewer at zero, and returns a summary message instead of per-user balances.

This is a **full standalone `CPHInline` class**. Copy the entire block exactly as shown.

```csharp
using System;
using System.Globalization;
using System.IO;
using System.Reflection;

public class CPHInline
{
    private const string InstallDir = @"D:\Streamer Bot\Extensions\TwitchHeist";
    private static bool resolverRegistered;

public bool Execute()
{
    RegisterAssemblyResolver();
    var bridgeAssembly = LoadBridgeAssembly();
    var bridgeActions = CreateBridgeActions(bridgeAssembly);
    var command = CreateInstance(bridgeAssembly, "TwitchHeists.StreamerBot.Bridge.Models.BridgePointsCommand");
    var targetUsername = GetResolvedTargetUsername(GetPointsTargetInputIndex());
    var targetUserId = GetResolvedTargetUserId();
    var targetDisplayName = GetResolvedTargetDisplayName(targetUsername);
    var amount = GetRequiredDecimalArg(GetPointsAmountArgName());

    SetProperty(command, "TargetTwitchUserId", targetUserId);
    SetProperty(command, "TargetUsername", targetUsername);
    SetProperty(command, "TargetDisplayName", targetDisplayName);
    SetProperty(command, "Amount", amount);
    SetProperty(command, "OccurredAtUtc", DateTimeOffset.UtcNow);

    var result = InvokeBridge(bridgeActions, "RemovePoints", InstallDir, command);
    var message = GetStringProperty(result, "Message");

    if (!string.IsNullOrWhiteSpace(message))
    {
        CPH.SendMessage(message);
    }

    CPH.LogInfo("[TwitchHeists][PointsRemove] " + message);
    return true;
}

    private static void RegisterAssemblyResolver()
    {
        if (resolverRegistered)
        {
            return;
        }

        AppDomain.CurrentDomain.AssemblyResolve += ResolveFromInstallDir;
        resolverRegistered = true;
    }

    private static Assembly ResolveFromInstallDir(object sender, ResolveEventArgs args)
    {
        var assemblyName = new AssemblyName(args.Name).Name + ".dll";
        var assemblyPath = Path.Combine(InstallDir, assemblyName);

        if (File.Exists(assemblyPath))
        {
            return Assembly.LoadFrom(assemblyPath);
        }

        return null;
    }

    private static Assembly LoadBridgeAssembly()
    {
        var bridgePath = Path.Combine(InstallDir, "TwitchHeists.StreamerBot.Bridge.dll");
        if (!File.Exists(bridgePath))
        {
            throw new FileNotFoundException("Bridge DLL not found.", bridgePath);
        }

        return Assembly.LoadFrom(bridgePath);
    }

    private static object CreateBridgeActions(Assembly bridgeAssembly)
    {
        var bridgeActionsType = bridgeAssembly.GetType("TwitchHeists.StreamerBot.Bridge.Services.BridgeActions", true);
        return Activator.CreateInstance(bridgeActionsType);
    }

    private static object CreateInstance(Assembly bridgeAssembly, string typeName)
    {
        var type = bridgeAssembly.GetType(typeName, true);
        return Activator.CreateInstance(type);
    }

    private static void SetProperty(object target, string propertyName, object value)
    {
        target.GetType().GetProperty(propertyName).SetValue(target, value, null);
    }

    private static object InvokeBridge(object bridgeActions, string methodName, params object[] arguments)
    {
        return bridgeActions.GetType().GetMethod(methodName).Invoke(bridgeActions, arguments);
    }

    private static string GetStringProperty(object target, string propertyName)
    {
        var value = target.GetType().GetProperty(propertyName).GetValue(target, null);
        return value == null ? string.Empty : value.ToString();
    }

    private string GetResolvedTargetUsername(int inputIndex)
    {
        var username = GetOptionalStringArg("targetUserName", "targetUserName", "input" + inputIndex);
        if (!string.IsNullOrWhiteSpace(username))
        {
            return username;
        }

        throw new InvalidOperationException(
            "Missing target username. Add a Get User Info for Target sub-action or pass the username directly.");
    }

    private string GetResolvedTargetDisplayName(string fallbackUsername)
    {
        var displayName = GetOptionalStringArg("targetUser", "targetUserName");
        return string.IsNullOrWhiteSpace(displayName) ? fallbackUsername : displayName;
    }

    private string? GetResolvedTargetUserId()
    {
        return GetOptionalStringArg("targetUserId");
    }

    private decimal GetRequiredDecimalArg(string argName)
    {
        var rawValue = GetRequiredStringArg(argName);
        decimal parsedAmount;
        if (decimal.TryParse(rawValue, NumberStyles.Number, CultureInfo.InvariantCulture, out parsedAmount))
        {
            return parsedAmount;
        }

        if (decimal.TryParse(rawValue, NumberStyles.Number, CultureInfo.CurrentCulture, out parsedAmount))
        {
            return parsedAmount;
        }

        throw new InvalidOperationException("'" + rawValue + "' is not a valid amount.");
    }

    private string GetRequiredStringArg(string argName)
    {
        var value = GetOptionalStringArg(argName);
        if (!string.IsNullOrWhiteSpace(value))
        {
            return value;
        }

        throw new InvalidOperationException("Missing required Streamer.bot argument: " + argName + ".");
    }

    private string GetOptionalStringArg(params string[] argNames)
    {
        foreach (var argName in argNames)
        {
            string value;
            if (CPH.TryGetArg<string>(argName, out value) && !string.IsNullOrWhiteSpace(value))
            {
                return value.Trim();
            }
        }

        return null;
    }

    private int GetPointsTargetInputIndex()
    {
        return GetPointsVerbOffset();
    }

    private string GetPointsAmountArgName()
    {
        return "input" + (GetPointsVerbOffset() + 1);
    }

    private int GetPointsVerbOffset()
    {
        var verb = GetOptionalStringArg("input0");
        if (string.Equals(verb, "add", StringComparison.OrdinalIgnoreCase)
            || string.Equals(verb, "remove", StringComparison.OrdinalIgnoreCase)
            || string.Equals(verb, "give", StringComparison.OrdinalIgnoreCase))
        {
            return 1;
        }

        return 0;
    }
}
```

### Action 8: Points give

Run this when chat matches `!points give <user> <amount>`.

This command is intended for everyone. It subtracts points from the sender balance and adds them to the target balance.

Add a **Get User Info for Target** sub-action before this Execute C# step with **User Login** = `%input0%` for a dedicated `!points give` command, or `%input1%` for a shared `!points` command.

`!points give all` is not supported. Use a real target username for give.

This is a **full standalone `CPHInline` class**. Copy the entire block exactly as shown.

```csharp
using System;
using System.Globalization;
using System.IO;
using System.Reflection;

public class CPHInline
{
    private const string InstallDir = @"D:\Streamer Bot\Extensions\TwitchHeist";
    private static bool resolverRegistered;

public bool Execute()
{
    RegisterAssemblyResolver();
    var bridgeAssembly = LoadBridgeAssembly();
    var bridgeActions = CreateBridgeActions(bridgeAssembly);
    var command = CreateInstance(bridgeAssembly, "TwitchHeists.StreamerBot.Bridge.Models.BridgePointsCommand");
    var sourceUserId = GetSenderUserId();
    var sourceUsername = GetSenderUsername();
    var sourceDisplayName = GetSenderDisplayName(sourceUsername);
    var targetUsername = GetResolvedTargetUsername(GetPointsTargetInputIndex());
    var targetUserId = GetResolvedTargetUserId();
    var targetDisplayName = GetResolvedTargetDisplayName(targetUsername);
    var amount = GetRequiredDecimalArg(GetPointsAmountArgName());

    SetProperty(command, "SourceTwitchUserId", sourceUserId);
    SetProperty(command, "SourceUsername", sourceUsername);
    SetProperty(command, "SourceDisplayName", sourceDisplayName);
    SetProperty(command, "TargetTwitchUserId", targetUserId);
    SetProperty(command, "TargetUsername", targetUsername);
    SetProperty(command, "TargetDisplayName", targetDisplayName);
    SetProperty(command, "Amount", amount);
    SetProperty(command, "OccurredAtUtc", DateTimeOffset.UtcNow);

    var result = InvokeBridge(bridgeActions, "GivePoints", InstallDir, command);
    var message = GetStringProperty(result, "Message");

    if (!string.IsNullOrWhiteSpace(message))
    {
        CPH.SendMessage(message);
    }

    CPH.LogInfo("[TwitchHeists][PointsGive] " + message);
    return true;
}

    private static void RegisterAssemblyResolver()
    {
        if (resolverRegistered)
        {
            return;
        }

        AppDomain.CurrentDomain.AssemblyResolve += ResolveFromInstallDir;
        resolverRegistered = true;
    }

    private static Assembly ResolveFromInstallDir(object sender, ResolveEventArgs args)
    {
        var assemblyName = new AssemblyName(args.Name).Name + ".dll";
        var assemblyPath = Path.Combine(InstallDir, assemblyName);

        if (File.Exists(assemblyPath))
        {
            return Assembly.LoadFrom(assemblyPath);
        }

        return null;
    }

    private static Assembly LoadBridgeAssembly()
    {
        var bridgePath = Path.Combine(InstallDir, "TwitchHeists.StreamerBot.Bridge.dll");
        if (!File.Exists(bridgePath))
        {
            throw new FileNotFoundException("Bridge DLL not found.", bridgePath);
        }

        return Assembly.LoadFrom(bridgePath);
    }

    private static object CreateBridgeActions(Assembly bridgeAssembly)
    {
        var bridgeActionsType = bridgeAssembly.GetType("TwitchHeists.StreamerBot.Bridge.Services.BridgeActions", true);
        return Activator.CreateInstance(bridgeActionsType);
    }

    private static object CreateInstance(Assembly bridgeAssembly, string typeName)
    {
        var type = bridgeAssembly.GetType(typeName, true);
        return Activator.CreateInstance(type);
    }

    private static void SetProperty(object target, string propertyName, object value)
    {
        target.GetType().GetProperty(propertyName).SetValue(target, value, null);
    }

    private static object InvokeBridge(object bridgeActions, string methodName, params object[] arguments)
    {
        return bridgeActions.GetType().GetMethod(methodName).Invoke(bridgeActions, arguments);
    }

    private static string GetStringProperty(object target, string propertyName)
    {
        var value = target.GetType().GetProperty(propertyName).GetValue(target, null);
        return value == null ? string.Empty : value.ToString();
    }

    private string GetSenderUsername()
    {
        var username = GetOptionalStringArg("userName", "user");
        if (!string.IsNullOrWhiteSpace(username))
        {
            return username;
        }

        throw new InvalidOperationException("Missing sender username.");
    }

    private string GetSenderDisplayName(string fallbackUsername)
    {
        var displayName = GetOptionalStringArg("displayName", "user");
        return string.IsNullOrWhiteSpace(displayName) ? fallbackUsername : displayName;
    }

    private string? GetSenderUserId()
    {
        return GetOptionalStringArg("userId");
    }

    private string GetResolvedTargetUsername(int inputIndex)
    {
        var username = GetOptionalStringArg("targetUserName", "targetUserName", "input" + inputIndex);
        if (!string.IsNullOrWhiteSpace(username))
        {
            return username;
        }

        throw new InvalidOperationException(
            "Missing target username. Add a Get User Info for Target sub-action or pass the username directly.");
    }

    private string GetResolvedTargetDisplayName(string fallbackUsername)
    {
        var displayName = GetOptionalStringArg("targetUser", "targetUserName");
        return string.IsNullOrWhiteSpace(displayName) ? fallbackUsername : displayName;
    }

    private string? GetResolvedTargetUserId()
    {
        return GetOptionalStringArg("targetUserId");
    }

    private decimal GetRequiredDecimalArg(string argName)
    {
        var rawValue = GetRequiredStringArg(argName);
        decimal parsedAmount;
        if (decimal.TryParse(rawValue, NumberStyles.Number, CultureInfo.InvariantCulture, out parsedAmount))
        {
            return parsedAmount;
        }

        if (decimal.TryParse(rawValue, NumberStyles.Number, CultureInfo.CurrentCulture, out parsedAmount))
        {
            return parsedAmount;
        }

        throw new InvalidOperationException("'" + rawValue + "' is not a valid amount.");
    }

    private string GetRequiredStringArg(string argName)
    {
        var value = GetOptionalStringArg(argName);
        if (!string.IsNullOrWhiteSpace(value))
        {
            return value;
        }

        throw new InvalidOperationException("Missing required Streamer.bot argument: " + argName + ".");
    }

    private string GetOptionalStringArg(params string[] argNames)
    {
        foreach (var argName in argNames)
        {
            string value;
            if (CPH.TryGetArg<string>(argName, out value) && !string.IsNullOrWhiteSpace(value))
            {
                return value.Trim();
            }
        }

        return null;
    }

    private int GetPointsTargetInputIndex()
    {
        return GetPointsVerbOffset();
    }

    private string GetPointsAmountArgName()
    {
        return "input" + (GetPointsVerbOffset() + 1);
    }

    private int GetPointsVerbOffset()
    {
        var verb = GetOptionalStringArg("input0");
        if (string.Equals(verb, "add", StringComparison.OrdinalIgnoreCase)
            || string.Equals(verb, "remove", StringComparison.OrdinalIgnoreCase)
            || string.Equals(verb, "give", StringComparison.OrdinalIgnoreCase))
        {
            return 1;
        }

        return 0;
    }
}
```

### Action 9: Raffle (multi-winner)

Run this when chat matches `!raffle` or `!raffle <points>`.

Set the Streamer.bot command permissions so only moderators and broadcaster can trigger it.

Use this action to run the adaptive multi-winner draw. TwitchHeists applies the StreamElements-style winner bands automatically.
Raffle joins are free: `!rjoin` never spends viewer points and joined entrants are not filtered by balance.

This snippet reads `isBroadcaster`/`broadcaster` from Streamer.bot args. If your trigger does not provide either arg, add a **Set Argument** sub-action for the broadcaster command path (`isBroadcaster = true`) so raffle rounds are tagged correctly in logs and storage.

This is a **full standalone `CPHInline` class**. Copy the entire block exactly as shown.

```csharp
using System;
using System.Globalization;
using System.IO;
using System.Reflection;

public class CPHInline
{
    private const string InstallDir = @"D:\Streamer Bot\Extensions\TwitchHeist";
    private static bool resolverRegistered;

public bool Execute()
{
    RegisterAssemblyResolver();
    var bridgeAssembly = LoadBridgeAssembly();
    var bridgeActions = CreateBridgeActions(bridgeAssembly);
    var command = CreateInstance(bridgeAssembly, "TwitchHeists.StreamerBot.Bridge.Models.BridgeRaffleCommand");
    var sourceUserId = GetSenderUserId();
    var sourceUsername = GetSenderUsername();
    var sourceDisplayName = GetSenderDisplayName(sourceUsername);
    var isBroadcaster = GetIsBroadcaster();
    var winnerPoints = GetRaffleWinnerPoints();

    SetProperty(command, "SourceTwitchUserId", sourceUserId);
    SetProperty(command, "SourceUsername", sourceUsername);
    SetProperty(command, "SourceDisplayName", sourceDisplayName);
    SetProperty(command, "IsBroadcaster", isBroadcaster);
    if (winnerPoints.HasValue)
    {
        SetProperty(command, "WinnerPoints", winnerPoints);
    }
    SetProperty(command, "OccurredAtUtc", DateTimeOffset.UtcNow);

    var result = InvokeBridge(bridgeActions, "RunRaffle", InstallDir, command);
    var message = GetStringProperty(result, "Message");

    if (!string.IsNullOrWhiteSpace(message))
    {
        CPH.SendMessage(message);
    }

    CPH.LogInfo("[TwitchHeists][Raffle] " + message);
    return true;
}

    private static void RegisterAssemblyResolver()
    {
        if (resolverRegistered)
        {
            return;
        }

        AppDomain.CurrentDomain.AssemblyResolve += ResolveFromInstallDir;
        resolverRegistered = true;
    }

    private static Assembly ResolveFromInstallDir(object sender, ResolveEventArgs args)
    {
        var assemblyName = new AssemblyName(args.Name).Name + ".dll";
        var assemblyPath = Path.Combine(InstallDir, assemblyName);

        if (File.Exists(assemblyPath))
        {
            return Assembly.LoadFrom(assemblyPath);
        }

        return null;
    }

    private static Assembly LoadBridgeAssembly()
    {
        var bridgePath = Path.Combine(InstallDir, "TwitchHeists.StreamerBot.Bridge.dll");
        if (!File.Exists(bridgePath))
        {
            throw new FileNotFoundException("Bridge DLL not found.", bridgePath);
        }

        return Assembly.LoadFrom(bridgePath);
    }

    private static object CreateBridgeActions(Assembly bridgeAssembly)
    {
        var bridgeActionsType = bridgeAssembly.GetType("TwitchHeists.StreamerBot.Bridge.Services.BridgeActions", true);
        return Activator.CreateInstance(bridgeActionsType);
    }

    private static object CreateInstance(Assembly bridgeAssembly, string typeName)
    {
        var type = bridgeAssembly.GetType(typeName, true);
        return Activator.CreateInstance(type);
    }

    private static void SetProperty(object target, string propertyName, object value)
    {
        target.GetType().GetProperty(propertyName).SetValue(target, value, null);
    }

    private static object InvokeBridge(object bridgeActions, string methodName, params object[] arguments)
    {
        return bridgeActions.GetType().GetMethod(methodName).Invoke(bridgeActions, arguments);
    }

    private static string GetStringProperty(object target, string propertyName)
    {
        var value = target.GetType().GetProperty(propertyName).GetValue(target, null);
        return value == null ? string.Empty : value.ToString();
    }

    private string GetSenderUsername()
    {
        var username = GetOptionalStringArg("userName", "user");
        if (!string.IsNullOrWhiteSpace(username))
        {
            return username;
        }

        throw new InvalidOperationException("Missing sender username.");
    }

    private string GetSenderDisplayName(string fallbackUsername)
    {
        var displayName = GetOptionalStringArg("displayName", "user");
        return string.IsNullOrWhiteSpace(displayName) ? fallbackUsername : displayName;
    }

    private string GetSenderUserId()
    {
        return GetOptionalStringArg("userId");
    }

    private bool GetIsBroadcaster()
    {
        return GetOptionalBoolArg("isBroadcaster", "broadcaster");
    }

    private decimal? GetRaffleWinnerPoints()
    {
        var rawInput = GetOptionalStringArg("input0", "input");
        if (!string.IsNullOrWhiteSpace(rawInput) &&
            decimal.TryParse(rawInput, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsedAmount) &&
            parsedAmount > 0)
        {
            return parsedAmount;
        }

        return null;
    }

    private bool GetOptionalBoolArg(params string[] argNames)
    {
        foreach (var argName in argNames)
        {
            bool boolValue;
            if (CPH.TryGetArg<bool>(argName, out boolValue))
            {
                return boolValue;
            }

            string stringValue;
            if (CPH.TryGetArg<string>(argName, out stringValue) && !string.IsNullOrWhiteSpace(stringValue))
            {
                if (string.Equals(stringValue, "true", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(stringValue, "1", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private string GetOptionalStringArg(params string[] argNames)
    {
        foreach (var argName in argNames)
        {
            string value;
            if (CPH.TryGetArg<string>(argName, out value) && !string.IsNullOrWhiteSpace(value))
            {
                return value.Trim();
            }
        }

        return null;
    }
}
```

### Action 10: Raffle (single winner)

Run this when chat matches `!sraffle` or `!sraffle <points>`.

Set the Streamer.bot command permissions so only moderators and broadcaster can trigger it.

Use this action to force exactly one winner while keeping the same entrant filtering rules.

This is a **full standalone `CPHInline` class**. Copy the entire block exactly as shown.

```csharp
using System;
using System.Globalization;
using System.IO;
using System.Reflection;

public class CPHInline
{
    private const string InstallDir = @"D:\Streamer Bot\Extensions\TwitchHeist";
    private static bool resolverRegistered;

public bool Execute()
{
    RegisterAssemblyResolver();
    var bridgeAssembly = LoadBridgeAssembly();
    var bridgeActions = CreateBridgeActions(bridgeAssembly);
    var command = CreateInstance(bridgeAssembly, "TwitchHeists.StreamerBot.Bridge.Models.BridgeRaffleCommand");
    var sourceUserId = GetSenderUserId();
    var sourceUsername = GetSenderUsername();
    var sourceDisplayName = GetSenderDisplayName(sourceUsername);
    var isBroadcaster = GetIsBroadcaster();
    var winnerPoints = GetRaffleWinnerPoints();

    SetProperty(command, "SourceTwitchUserId", sourceUserId);
    SetProperty(command, "SourceUsername", sourceUsername);
    SetProperty(command, "SourceDisplayName", sourceDisplayName);
    SetProperty(command, "IsBroadcaster", isBroadcaster);
    if (winnerPoints.HasValue)
    {
        SetProperty(command, "WinnerPoints", winnerPoints);
    }
    SetProperty(command, "OccurredAtUtc", DateTimeOffset.UtcNow);

    var result = InvokeBridge(bridgeActions, "RunSingleWinnerRaffle", InstallDir, command);
    var message = GetStringProperty(result, "Message");

    if (!string.IsNullOrWhiteSpace(message))
    {
        CPH.SendMessage(message);
    }

    CPH.LogInfo("[TwitchHeists][SingleRaffle] " + message);
    return true;
}

    private static void RegisterAssemblyResolver()
    {
        if (resolverRegistered)
        {
            return;
        }

        AppDomain.CurrentDomain.AssemblyResolve += ResolveFromInstallDir;
        resolverRegistered = true;
    }

    private static Assembly ResolveFromInstallDir(object sender, ResolveEventArgs args)
    {
        var assemblyName = new AssemblyName(args.Name).Name + ".dll";
        var assemblyPath = Path.Combine(InstallDir, assemblyName);

        if (File.Exists(assemblyPath))
        {
            return Assembly.LoadFrom(assemblyPath);
        }

        return null;
    }

    private static Assembly LoadBridgeAssembly()
    {
        var bridgePath = Path.Combine(InstallDir, "TwitchHeists.StreamerBot.Bridge.dll");
        if (!File.Exists(bridgePath))
        {
            throw new FileNotFoundException("Bridge DLL not found.", bridgePath);
        }

        return Assembly.LoadFrom(bridgePath);
    }

    private static object CreateBridgeActions(Assembly bridgeAssembly)
    {
        var bridgeActionsType = bridgeAssembly.GetType("TwitchHeists.StreamerBot.Bridge.Services.BridgeActions", true);
        return Activator.CreateInstance(bridgeActionsType);
    }

    private static object CreateInstance(Assembly bridgeAssembly, string typeName)
    {
        var type = bridgeAssembly.GetType(typeName, true);
        return Activator.CreateInstance(type);
    }

    private static void SetProperty(object target, string propertyName, object value)
    {
        target.GetType().GetProperty(propertyName).SetValue(target, value, null);
    }

    private static object InvokeBridge(object bridgeActions, string methodName, params object[] arguments)
    {
        return bridgeActions.GetType().GetMethod(methodName).Invoke(bridgeActions, arguments);
    }

    private static string GetStringProperty(object target, string propertyName)
    {
        var value = target.GetType().GetProperty(propertyName).GetValue(target, null);
        return value == null ? string.Empty : value.ToString();
    }

    private string GetSenderUsername()
    {
        var username = GetOptionalStringArg("userName", "user");
        if (!string.IsNullOrWhiteSpace(username))
        {
            return username;
        }

        throw new InvalidOperationException("Missing sender username.");
    }

    private string GetSenderDisplayName(string fallbackUsername)
    {
        var displayName = GetOptionalStringArg("displayName", "user");
        return string.IsNullOrWhiteSpace(displayName) ? fallbackUsername : displayName;
    }

    private string GetSenderUserId()
    {
        return GetOptionalStringArg("userId");
    }

    private bool GetIsBroadcaster()
    {
        return GetOptionalBoolArg("isBroadcaster", "broadcaster");
    }

    private decimal? GetRaffleWinnerPoints()
    {
        var rawInput = GetOptionalStringArg("input0", "input");
        if (!string.IsNullOrWhiteSpace(rawInput) &&
            decimal.TryParse(rawInput, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsedAmount) &&
            parsedAmount > 0)
        {
            return parsedAmount;
        }

        return null;
    }

    private bool GetOptionalBoolArg(params string[] argNames)
    {
        foreach (var argName in argNames)
        {
            bool boolValue;
            if (CPH.TryGetArg<bool>(argName, out boolValue))
            {
                return boolValue;
            }

            string stringValue;
            if (CPH.TryGetArg<string>(argName, out stringValue) && !string.IsNullOrWhiteSpace(stringValue))
            {
                if (string.Equals(stringValue, "true", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(stringValue, "1", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private string GetOptionalStringArg(params string[] argNames)
    {
        foreach (var argName in argNames)
        {
            string value;
            if (CPH.TryGetArg<string>(argName, out value) && !string.IsNullOrWhiteSpace(value))
            {
                return value.Trim();
            }
        }

        return null;
    }
}
```

### Action 11: Raffle join

Run this when chat matches `!rjoin`.

This command opts the sender into the currently open raffle window.

This is a **full standalone `CPHInline` class**. Copy the entire block exactly as shown.

```csharp
using System;
using System.IO;
using System.Reflection;

public class CPHInline
{
    private const string InstallDir = @"D:\Streamer Bot\Extensions\TwitchHeist";
    private static bool resolverRegistered;

public bool Execute()
{
    RegisterAssemblyResolver();
    var bridgeAssembly = LoadBridgeAssembly();
    var bridgeActions = CreateBridgeActions(bridgeAssembly);
    var command = CreateInstance(bridgeAssembly, "TwitchHeists.StreamerBot.Bridge.Models.BridgeRaffleCommand");
    var sourceUserId = GetSenderUserId();
    var sourceUsername = GetSenderUsername();
    var sourceDisplayName = GetSenderDisplayName(sourceUsername);

    SetProperty(command, "SourceTwitchUserId", sourceUserId);
    SetProperty(command, "SourceUsername", sourceUsername);
    SetProperty(command, "SourceDisplayName", sourceDisplayName);
    SetProperty(command, "OccurredAtUtc", DateTimeOffset.UtcNow);

    var result = InvokeBridge(bridgeActions, "JoinRaffle", InstallDir, command);
    var message = GetStringProperty(result, "Message");

    if (!string.IsNullOrWhiteSpace(message))
    {
        CPH.SendMessage(message);
    }

    CPH.LogInfo("[TwitchHeists][RaffleJoin] " + message);
    return true;
}

    private static void RegisterAssemblyResolver()
    {
        if (resolverRegistered)
        {
            return;
        }

        AppDomain.CurrentDomain.AssemblyResolve += ResolveFromInstallDir;
        resolverRegistered = true;
    }

    private static Assembly ResolveFromInstallDir(object sender, ResolveEventArgs args)
    {
        var assemblyName = new AssemblyName(args.Name).Name + ".dll";
        var assemblyPath = Path.Combine(InstallDir, assemblyName);

        if (File.Exists(assemblyPath))
        {
            return Assembly.LoadFrom(assemblyPath);
        }

        return null;
    }

    private static Assembly LoadBridgeAssembly()
    {
        var bridgePath = Path.Combine(InstallDir, "TwitchHeists.StreamerBot.Bridge.dll");
        if (!File.Exists(bridgePath))
        {
            throw new FileNotFoundException("Bridge DLL not found.", bridgePath);
        }

        return Assembly.LoadFrom(bridgePath);
    }

    private static object CreateBridgeActions(Assembly bridgeAssembly)
    {
        var bridgeActionsType = bridgeAssembly.GetType("TwitchHeists.StreamerBot.Bridge.Services.BridgeActions", true);
        return Activator.CreateInstance(bridgeActionsType);
    }

    private static object CreateInstance(Assembly bridgeAssembly, string typeName)
    {
        var type = bridgeAssembly.GetType(typeName, true);
        return Activator.CreateInstance(type);
    }

    private static void SetProperty(object target, string propertyName, object value)
    {
        target.GetType().GetProperty(propertyName).SetValue(target, value, null);
    }

    private static object InvokeBridge(object bridgeActions, string methodName, params object[] arguments)
    {
        return bridgeActions.GetType().GetMethod(methodName).Invoke(bridgeActions, arguments);
    }

    private static string GetStringProperty(object target, string propertyName)
    {
        var value = target.GetType().GetProperty(propertyName).GetValue(target, null);
        return value == null ? string.Empty : value.ToString();
    }

    private string GetSenderUsername()
    {
        var username = GetOptionalStringArg("userName", "user");
        if (!string.IsNullOrWhiteSpace(username))
        {
            return username;
        }

        throw new InvalidOperationException("Missing sender username.");
    }

    private string GetSenderDisplayName(string fallbackUsername)
    {
        var displayName = GetOptionalStringArg("displayName", "user");
        return string.IsNullOrWhiteSpace(displayName) ? fallbackUsername : displayName;
    }

    private string? GetSenderUserId()
    {
        return GetOptionalStringArg("userId");
    }

    private string? GetOptionalStringArg(params string[] argNames)
    {
        foreach (var argName in argNames)
        {
            string value;
            if (CPH.TryGetArg<string>(argName, out value) && !string.IsNullOrWhiteSpace(value))
            {
                return value.Trim();
            }
        }

        return null;
    }
}
```

### Action 12: Raffle resolution timer

Run this on a short repeating timer (10-30 seconds), just like heist resolution.

This timer sends raffle countdown reminders (1m / 30s / 10s) and posts the final winner message when a raffle is due.

This is a **full standalone `CPHInline` class**. Copy the entire block exactly as shown.

```csharp
using System;
using System.IO;
using System.Reflection;

public class CPHInline
{
    private const string InstallDir = @"D:\Streamer Bot\Extensions\TwitchHeist";
    private static bool resolverRegistered;

public bool Execute()
{
    RegisterAssemblyResolver();
    var bridgeAssembly = LoadBridgeAssembly();
    var bridgeActions = CreateBridgeActions(bridgeAssembly);
    var result = InvokeBridge(bridgeActions, "ResolveDueRaffles", InstallDir, DateTimeOffset.UtcNow);
    var message = GetStringProperty(result, "Message");

    if (!string.IsNullOrWhiteSpace(message) &&
        !string.Equals(message, "No due raffles to resolve.", StringComparison.OrdinalIgnoreCase))
    {
        CPH.SendMessage(message);
    }

    CPH.LogInfo("[TwitchHeists][RaffleTimer] " + message);
    return true;
}

    private static void RegisterAssemblyResolver()
    {
        if (resolverRegistered)
        {
            return;
        }

        AppDomain.CurrentDomain.AssemblyResolve += ResolveFromInstallDir;
        resolverRegistered = true;
    }

    private static Assembly ResolveFromInstallDir(object sender, ResolveEventArgs args)
    {
        var assemblyName = new AssemblyName(args.Name).Name + ".dll";
        var assemblyPath = Path.Combine(InstallDir, assemblyName);

        if (File.Exists(assemblyPath))
        {
            return Assembly.LoadFrom(assemblyPath);
        }

        return null;
    }

    private static Assembly LoadBridgeAssembly()
    {
        var bridgePath = Path.Combine(InstallDir, "TwitchHeists.StreamerBot.Bridge.dll");
        if (!File.Exists(bridgePath))
        {
            throw new FileNotFoundException("Bridge DLL not found.", bridgePath);
        }

        return Assembly.LoadFrom(bridgePath);
    }

    private static object CreateBridgeActions(Assembly bridgeAssembly)
    {
        var bridgeActionsType = bridgeAssembly.GetType("TwitchHeists.StreamerBot.Bridge.Services.BridgeActions", true);
        return Activator.CreateInstance(bridgeActionsType);
    }

    private static object InvokeBridge(object bridgeActions, string methodName, params object[] arguments)
    {
        return bridgeActions.GetType().GetMethod(methodName).Invoke(bridgeActions, arguments);
    }

    private static string GetStringProperty(object target, string propertyName)
    {
        var value = target.GetType().GetProperty(propertyName).GetValue(target, null);
        return value == null ? string.Empty : value.ToString();
    }
}
```

### Action 13: Watchtime lookup

Run this when chat matches `!watchtime` or `!watchtime <user>`.

This command returns lifetime watchtime stored in the database. If no username is supplied, it should look up the triggering chatter.

If you want `!watchtime <user>` to resolve a real Twitch account instead of a raw typed string, add a **Get User Info for Target** sub-action before this Execute C# step with **User Login** = `%input0%`.

This is a **full standalone `CPHInline` class**. Copy the entire block exactly as shown.

```csharp
using System;
using System.IO;
using System.Reflection;

public class CPHInline
{
    private const string InstallDir = @"D:\Streamer Bot\Extensions\TwitchHeist";
    private static bool resolverRegistered;

public bool Execute()
{
    RegisterAssemblyResolver();
    var bridgeAssembly = LoadBridgeAssembly();
    var bridgeActions = CreateBridgeActions(bridgeAssembly);
    var query = CreateInstance(bridgeAssembly, "TwitchHeists.StreamerBot.Bridge.Models.BridgeWatchtimeQuery");
    var requesterUserId = GetSenderUserId();
    var requesterUsername = GetSenderUsername();
    var requesterDisplayName = GetSenderDisplayName(requesterUsername);
    var targetUserId = GetOptionalStringArg("targetUserId");
    var targetUsername = GetOptionalStringArg("targetUserName", "input0");
    var targetDisplayName = string.IsNullOrWhiteSpace(targetUsername)
        ? null
        : GetResolvedTargetDisplayName(targetUsername);

    SetProperty(query, "RequesterTwitchUserId", requesterUserId);
    SetProperty(query, "RequesterUsername", requesterUsername);
    SetProperty(query, "RequesterDisplayName", requesterDisplayName);
    SetProperty(query, "TargetTwitchUserId", targetUserId);
    SetProperty(query, "TargetUsername", targetUsername);
    SetProperty(query, "TargetDisplayName", targetDisplayName);
    SetProperty(query, "OccurredAtUtc", DateTimeOffset.UtcNow);

    var result = InvokeBridge(bridgeActions, "GetWatchtime", InstallDir, query);
    var message = GetStringProperty(result, "Message");

    if (!string.IsNullOrWhiteSpace(message))
    {
        CPH.SendMessage(message);
    }

    CPH.LogInfo("[TwitchHeists][Watchtime] " + message);
    return true;
}

    private static void RegisterAssemblyResolver()
    {
        if (resolverRegistered)
        {
            return;
        }

        AppDomain.CurrentDomain.AssemblyResolve += ResolveFromInstallDir;
        resolverRegistered = true;
    }

    private static Assembly ResolveFromInstallDir(object sender, ResolveEventArgs args)
    {
        var assemblyName = new AssemblyName(args.Name).Name + ".dll";
        var assemblyPath = Path.Combine(InstallDir, assemblyName);

        if (File.Exists(assemblyPath))
        {
            return Assembly.LoadFrom(assemblyPath);
        }

        return null;
    }

    private static Assembly LoadBridgeAssembly()
    {
        var bridgePath = Path.Combine(InstallDir, "TwitchHeists.StreamerBot.Bridge.dll");
        if (!File.Exists(bridgePath))
        {
            throw new FileNotFoundException("Bridge DLL not found.", bridgePath);
        }

        return Assembly.LoadFrom(bridgePath);
    }

    private static object CreateBridgeActions(Assembly bridgeAssembly)
    {
        var bridgeActionsType = bridgeAssembly.GetType("TwitchHeists.StreamerBot.Bridge.Services.BridgeActions", true);
        return Activator.CreateInstance(bridgeActionsType);
    }

    private static object CreateInstance(Assembly bridgeAssembly, string typeName)
    {
        var type = bridgeAssembly.GetType(typeName, true);
        return Activator.CreateInstance(type);
    }

    private static void SetProperty(object target, string propertyName, object value)
    {
        target.GetType().GetProperty(propertyName).SetValue(target, value, null);
    }

    private static object InvokeBridge(object bridgeActions, string methodName, params object[] arguments)
    {
        return bridgeActions.GetType().GetMethod(methodName).Invoke(bridgeActions, arguments);
    }

    private static string GetStringProperty(object target, string propertyName)
    {
        var value = target.GetType().GetProperty(propertyName).GetValue(target, null);
        return value == null ? string.Empty : value.ToString();
    }

    private string GetSenderUsername()
    {
        var username = GetOptionalStringArg("userName", "user");
        if (!string.IsNullOrWhiteSpace(username))
        {
            return username;
        }

        throw new InvalidOperationException("Missing sender username.");
    }

    private string GetSenderDisplayName(string fallbackUsername)
    {
        var displayName = GetOptionalStringArg("displayName", "user");
        return string.IsNullOrWhiteSpace(displayName) ? fallbackUsername : displayName;
    }

    private string? GetSenderUserId()
    {
        return GetOptionalStringArg("userId");
    }

    private string GetResolvedTargetDisplayName(string fallbackUsername)
    {
        var displayName = GetOptionalStringArg("targetUser", "targetUserName");
        return string.IsNullOrWhiteSpace(displayName) ? fallbackUsername : displayName;
    }

    private string GetOptionalStringArg(params string[] argNames)
    {
        foreach (var argName in argNames)
        {
            string value;
            if (CPH.TryGetArg<string>(argName, out value) && !string.IsNullOrWhiteSpace(value))
            {
                return value.Trim();
            }
        }

        return null;
    }
}
```

### Action 14: Leaderboard lookup

Run this when chat matches `!leaderboard`.

This command returns the top five viewers by points, formatted as `1. username (points)` on separate lines.

```csharp
using System;
using System.IO;
using System.Reflection;

public class CPHInline
{
    private const string InstallDir = @"D:\Streamer Bot\Extensions\TwitchHeist";
    private static bool resolverRegistered;

    public bool Execute()
    {
        RegisterAssemblyResolver();
        var bridgeAssembly = LoadBridgeAssembly();
        var bridgeActions = CreateBridgeActions(bridgeAssembly);
        var result = InvokeBridge(bridgeActions, "GetLeaderboard", InstallDir);
        var message = GetStringProperty(result, "Message");

        if (!string.IsNullOrWhiteSpace(message))
        {
            CPH.SendMessage(message);
        }

        CPH.LogInfo("[TwitchHeists][Leaderboard] " + message);
        return true;
    }

    private static void RegisterAssemblyResolver()
    {
        if (resolverRegistered)
        {
            return;
        }

        AppDomain.CurrentDomain.AssemblyResolve += ResolveFromInstallDir;
        resolverRegistered = true;
    }

    private static Assembly ResolveFromInstallDir(object sender, ResolveEventArgs args)
    {
        var assemblyName = new AssemblyName(args.Name).Name + ".dll";
        var assemblyPath = Path.Combine(InstallDir, assemblyName);

        return File.Exists(assemblyPath)
            ? Assembly.LoadFrom(assemblyPath)
            : null;
    }

    private static Assembly LoadBridgeAssembly()
    {
        var bridgePath = Path.Combine(InstallDir, "TwitchHeists.StreamerBot.Bridge.dll");
        if (!File.Exists(bridgePath))
        {
            throw new FileNotFoundException("Bridge DLL not found.", bridgePath);
        }

        return Assembly.LoadFrom(bridgePath);
    }

    private static object CreateBridgeActions(Assembly bridgeAssembly)
    {
        var bridgeActionsType = bridgeAssembly.GetType("TwitchHeists.StreamerBot.Bridge.Services.BridgeActions", true);
        return Activator.CreateInstance(bridgeActionsType);
    }

    private static object InvokeBridge(object bridgeActions, string methodName, params object[] arguments)
    {
        return bridgeActions.GetType().GetMethod(methodName).Invoke(bridgeActions, arguments);
    }

    private static string GetStringProperty(object target, string propertyName)
    {
        var value = target.GetType().GetProperty(propertyName).GetValue(target, null);
        return value == null ? string.Empty : value.ToString();
    }
}
```

### Action 15: Points lookup

Run this when chat matches `!points` or `!points <user>`.

This command returns the current points balance for the sender by default, or for the tagged target when a username is supplied.

If you want `!points <user>` to resolve a real Twitch account instead of a raw typed string, add a **Get User Info for Target** sub-action before this Execute C# step with **User Login** = `%input0%`.

This is a **full standalone `CPHInline` class**. Copy the entire block exactly as shown.

```csharp
using System;
using System.IO;
using System.Reflection;

public class CPHInline
{
    private const string InstallDir = @"D:\Streamer Bot\Extensions\TwitchHeist";
    private static bool resolverRegistered;

    public bool Execute()
    {
        RegisterAssemblyResolver();
        var bridgeAssembly = LoadBridgeAssembly();
        var bridgeActions = CreateBridgeActions(bridgeAssembly);
        var query = CreateInstance(bridgeAssembly, "TwitchHeists.StreamerBot.Bridge.Models.BridgePointsQuery");
        var requesterUserId = GetSenderUserId();
        var requesterUsername = GetSenderUsername();
        var requesterDisplayName = GetSenderDisplayName(requesterUsername);
        var targetUserId = GetOptionalStringArg("targetUserId");
        var targetUsername = GetOptionalStringArg("targetUserName", "input0");
        var targetDisplayName = string.IsNullOrWhiteSpace(targetUsername)
            ? null
            : GetResolvedTargetDisplayName(targetUsername);

        SetProperty(query, "RequesterTwitchUserId", requesterUserId);
        SetProperty(query, "RequesterUsername", requesterUsername);
        SetProperty(query, "RequesterDisplayName", requesterDisplayName);
        SetProperty(query, "TargetTwitchUserId", targetUserId);
        SetProperty(query, "TargetUsername", targetUsername);
        SetProperty(query, "TargetDisplayName", targetDisplayName);
        SetProperty(query, "OccurredAtUtc", DateTimeOffset.UtcNow);

        var result = InvokeBridge(bridgeActions, "GetPoints", InstallDir, query);
        var message = GetStringProperty(result, "Message");

        if (!string.IsNullOrWhiteSpace(message))
        {
            CPH.SendMessage(message);
        }

        CPH.LogInfo("[TwitchHeists][PointsLookup] " + message);
        return true;
    }

    private static void RegisterAssemblyResolver()
    {
        if (resolverRegistered)
        {
            return;
        }

        AppDomain.CurrentDomain.AssemblyResolve += ResolveFromInstallDir;
        resolverRegistered = true;
    }

    private static Assembly ResolveFromInstallDir(object sender, ResolveEventArgs args)
    {
        var assemblyName = new AssemblyName(args.Name).Name + ".dll";
        var assemblyPath = Path.Combine(InstallDir, assemblyName);

        return File.Exists(assemblyPath)
            ? Assembly.LoadFrom(assemblyPath)
            : null;
    }

    private static Assembly LoadBridgeAssembly()
    {
        var bridgePath = Path.Combine(InstallDir, "TwitchHeists.StreamerBot.Bridge.dll");
        if (!File.Exists(bridgePath))
        {
            throw new FileNotFoundException("Bridge DLL not found.", bridgePath);
        }

        return Assembly.LoadFrom(bridgePath);
    }

    private static object CreateBridgeActions(Assembly bridgeAssembly)
    {
        var bridgeActionsType = bridgeAssembly.GetType("TwitchHeists.StreamerBot.Bridge.Services.BridgeActions", true);
        return Activator.CreateInstance(bridgeActionsType);
    }

    private static object CreateInstance(Assembly bridgeAssembly, string typeName)
    {
        var type = bridgeAssembly.GetType(typeName, true);
        return Activator.CreateInstance(type);
    }

    private static void SetProperty(object target, string propertyName, object value)
    {
        target.GetType().GetProperty(propertyName).SetValue(target, value, null);
    }

    private static object InvokeBridge(object bridgeActions, string methodName, params object[] arguments)
    {
        return bridgeActions.GetType().GetMethod(methodName).Invoke(bridgeActions, arguments);
    }

    private static string GetStringProperty(object target, string propertyName)
    {
        var value = target.GetType().GetProperty(propertyName).GetValue(target, null);
        return value == null ? string.Empty : value.ToString();
    }

    private string GetSenderUsername()
    {
        var username = GetOptionalStringArg("userName", "user");
        if (!string.IsNullOrWhiteSpace(username))
        {
            return username;
        }

        throw new InvalidOperationException("Missing sender username.");
    }

    private string GetSenderDisplayName(string fallbackUsername)
    {
        var displayName = GetOptionalStringArg("displayName", "user");
        return string.IsNullOrWhiteSpace(displayName) ? fallbackUsername : displayName;
    }

    private string? GetSenderUserId()
    {
        return GetOptionalStringArg("userId");
    }

    private string GetResolvedTargetDisplayName(string fallbackUsername)
    {
        var displayName = GetOptionalStringArg("targetUser", "targetUserName");
        return string.IsNullOrWhiteSpace(displayName) ? fallbackUsername : displayName;
    }

    private string? GetOptionalStringArg(params string[] argNames)
    {
        foreach (var argName in argNames)
        {
            string value;
            if (CPH.TryGetArg<string>(argName, out value) && !string.IsNullOrWhiteSpace(value))
            {
                return value.Trim();
            }
        }

        return null;
    }
}
```

### Action 2b: Stream start

Run this on your **stream online / go-live trigger**.

This action only logs to Streamer.bot. It does **not** send a chat message.

```csharp
using System;
using System.IO;
using System.Reflection;

public class CPHInline
{
    private const string InstallDir = @"D:\Streamer Bot\Extensions\TwitchHeist";
    private static bool resolverRegistered;

    public bool Execute()
    {
        RegisterAssemblyResolver();
        var bridgeAssembly = LoadBridgeAssembly();
        var bridgeActions = CreateBridgeActions(bridgeAssembly);
        var command = CreateInstance(bridgeAssembly, "TwitchHeists.StreamerBot.Bridge.Models.BridgeStreamLifecycleCommand");

        SetProperty(command, "OccurredAtUtc", DateTimeOffset.UtcNow);

        var result = InvokeBridge(bridgeActions, "StartStream", InstallDir, command);
        var message = GetStringProperty(result, "Message");

        CPH.LogInfo("[TwitchHeists][StreamStart] " + message);
        return true;
    }

    private static void RegisterAssemblyResolver()
    {
        if (resolverRegistered)
        {
            return;
        }

        AppDomain.CurrentDomain.AssemblyResolve += ResolveFromInstallDir;
        resolverRegistered = true;
    }

    private static Assembly ResolveFromInstallDir(object sender, ResolveEventArgs args)
    {
        var assemblyName = new AssemblyName(args.Name).Name + ".dll";
        var assemblyPath = Path.Combine(InstallDir, assemblyName);

        if (File.Exists(assemblyPath))
        {
            return Assembly.LoadFrom(assemblyPath);
        }

        return null;
    }

    private static Assembly LoadBridgeAssembly()
    {
        var bridgePath = Path.Combine(InstallDir, "TwitchHeists.StreamerBot.Bridge.dll");
        if (!File.Exists(bridgePath))
        {
            throw new FileNotFoundException("Bridge DLL not found.", bridgePath);
        }

        return Assembly.LoadFrom(bridgePath);
    }

    private static object CreateBridgeActions(Assembly bridgeAssembly)
    {
        var bridgeActionsType = bridgeAssembly.GetType("TwitchHeists.StreamerBot.Bridge.Services.BridgeActions", true);
        return Activator.CreateInstance(bridgeActionsType);
    }

    private static object CreateInstance(Assembly bridgeAssembly, string typeName)
    {
        var type = bridgeAssembly.GetType(typeName, true);
        return Activator.CreateInstance(type);
    }

    private static void SetProperty(object target, string propertyName, object value)
    {
        target.GetType().GetProperty(propertyName).SetValue(target, value, null);
    }

    private static object InvokeBridge(object bridgeActions, string methodName, params object[] arguments)
    {
        return bridgeActions.GetType().GetMethod(methodName).Invoke(bridgeActions, arguments);
    }

    private static string GetStringProperty(object target, string propertyName)
    {
        var value = target.GetType().GetProperty(propertyName).GetValue(target, null);
        return value == null ? string.Empty : value.ToString();
    }
}
```

### Action 2c: Stream end

Run this on your **stream offline / end trigger**.

This action only logs to Streamer.bot. It does **not** send a chat message.

```csharp
using System;
using System.IO;
using System.Reflection;

public class CPHInline
{
    private const string InstallDir = @"D:\Streamer Bot\Extensions\TwitchHeist";
    private static bool resolverRegistered;

    public bool Execute()
    {
        RegisterAssemblyResolver();
        var bridgeAssembly = LoadBridgeAssembly();
        var bridgeActions = CreateBridgeActions(bridgeAssembly);
        var command = CreateInstance(bridgeAssembly, "TwitchHeists.StreamerBot.Bridge.Models.BridgeStreamLifecycleCommand");

        SetProperty(command, "OccurredAtUtc", DateTimeOffset.UtcNow);

        var result = InvokeBridge(bridgeActions, "EndStream", InstallDir, command);
        var message = GetStringProperty(result, "Message");

        CPH.LogInfo("[TwitchHeists][StreamEnd] " + message);
        return true;
    }

    private static void RegisterAssemblyResolver()
    {
        if (resolverRegistered)
        {
            return;
        }

        AppDomain.CurrentDomain.AssemblyResolve += ResolveFromInstallDir;
        resolverRegistered = true;
    }

    private static Assembly ResolveFromInstallDir(object sender, ResolveEventArgs args)
    {
        var assemblyName = new AssemblyName(args.Name).Name + ".dll";
        var assemblyPath = Path.Combine(InstallDir, assemblyName);

        if (File.Exists(assemblyPath))
        {
            return Assembly.LoadFrom(assemblyPath);
        }

        return null;
    }

    private static Assembly LoadBridgeAssembly()
    {
        var bridgePath = Path.Combine(InstallDir, "TwitchHeists.StreamerBot.Bridge.dll");
        if (!File.Exists(bridgePath))
        {
            throw new FileNotFoundException("Bridge DLL not found.", bridgePath);
        }

        return Assembly.LoadFrom(bridgePath);
    }

    private static object CreateBridgeActions(Assembly bridgeAssembly)
    {
        var bridgeActionsType = bridgeAssembly.GetType("TwitchHeists.StreamerBot.Bridge.Services.BridgeActions", true);
        return Activator.CreateInstance(bridgeActionsType);
    }

    private static object CreateInstance(Assembly bridgeAssembly, string typeName)
    {
        var type = bridgeAssembly.GetType(typeName, true);
        return Activator.CreateInstance(type);
    }

    private static void SetProperty(object target, string propertyName, object value)
    {
        target.GetType().GetProperty(propertyName).SetValue(target, value, null);
    }

    private static object InvokeBridge(object bridgeActions, string methodName, params object[] arguments)
    {
        return bridgeActions.GetType().GetMethod(methodName).Invoke(bridgeActions, arguments);
    }

    private static string GetStringProperty(object target, string propertyName)
    {
        var value = target.GetType().GetProperty(propertyName).GetValue(target, null);
        return value == null ? string.Empty : value.ToString();
    }
}
```

## 7. Result handling

The reflection loader still returns the bridge result object at runtime. The snippets above read `result.Message` with reflection and then:

- log it in every action;
- send it to chat for `!heist`, `!join`, resolved heists, points commands, `!raffle`, `!sraffle`, `!rjoin`, and `!watchtime`;
- avoid sending the `chatPresence`, `StartStream`, and `EndStream` results to chat so you do not spam every message.

## 8. Recommended trigger map

| Streamer.bot trigger | TwitchHeists bridge call |
|---|---|
| 5-minute timer | `RefreshCommunityViewers` |
| Chat message | `RecordChatPresence` |
| Stream online / go-live trigger | `StartStream` |
| Stream offline / end trigger | `EndStream` |
| Command `!heist` | `StartHeist` |
| Command `!join` | `JoinHeist` |
| 10-30 second timer | `ResolveDueHeists` |
| Command `!points add` (mods only) | `AddPoints` |
| Command `!points remove` (mods only) | `RemovePoints` |
| Command `!points give` | `GivePoints` |
| Command `!points` | `GetPoints` |
| Command `!leaderboard` | `GetLeaderboard` |
| Command `!raffle [points]` (mods + broadcaster) | `RunRaffle` |
| Command `!sraffle [points]` (mods + broadcaster) | `RunSingleWinnerRaffle` |
| Command `!rjoin` | `JoinRaffle` |
| 10-30 second timer | `ResolveDueRaffles` |
| Command `!watchtime` | `GetWatchtime` |

Use the `userId` chat arg for the sender and the `targetUserId` value from **Get User Info for Target** wherever those values are available. TwitchHeists now treats Twitch user ID as the stable identity for balances, watchtime, and heist ownership, while still falling back to usernames for older rows and commands.

Watch streaks are silent. They do not send chat output when a viewer qualifies. Instead, the first time a viewer is seen during an active stream, TwitchHeists awards `100 * streak` points in the background. Missing the previous completed stream resets the next qualifying stream back to streak `1`.

## 9. First-run checklist

1. The full contents of `src\TwitchHeists.StreamerBot.Bridge\bin\Release\net48\` were copied into one folder.
2. No `TwitchHeists.*.dll` files are manually added in Streamer.bot action references.
3. `appsettings.json` and `heist-messages.json` are beside the bridge DLL.
4. You copied the `runtimes\` folder too.
5. Every Streamer.bot action uses the same `installDir`.
6. Your Community refresh action runs every 5 minutes.
7. Your heist and raffle resolution actions run more frequently than their join windows.
8. You did **not** add `netstandard.dll`.
9. Your Execute C# snippets call `RegisterAssemblyResolver();` before loading the bridge.
10. Your Execute C# snippets load `TwitchHeists.StreamerBot.Bridge.dll` from `InstallDir` with `Assembly.LoadFrom(...)`.
11. Your `!points add` and `!points remove` command permissions are restricted to moderators.
12. Your `!raffle` and `!sraffle` command permissions allow moderators and broadcaster.
13. Your target-based commands add **Get User Info for Target** before Execute C# so `targetUserName`, `targetUser`, and `targetUserId` are available.
14. Your chat and community snippets pass a real subscriber tier instead of leaving `SubscriberTier` hardcoded to `0`.
15. Your Streamer.bot workflow fires `StartStream` when you go live and `EndStream` when the stream ends. If you start Streamer.bot after the stream is already live, manually run your `StartStream` action once so streak logic is in sync.

## 10. Operational notes

1. Refresh cycles are idempotent per timestamp, so retries should not double-award points.
2. Chat-only presence expires at the next refresh boundary if the viewer never appears in the Community snapshot.
3. Heist points are reserved when a user starts or joins a round.
4. On failure, reserved points stay lost.
5. Crews below `Heist.MinimumParticipants` are refunded instead of being forced into a normal resolution.
6. On success, the pot is doubled and split across an adaptive winner count from the configured winner bands.
7. Large heist result messages stay compact and respect `Heist.MaximumNamedResolutionCallouts`.
8. `!points remove` clamps the target balance to zero instead of allowing negative balances.
9. `!points give` fails when the sender does not have enough points.
10. `!raffle <points>` opens a timed window, posts 1m/30s/10s countdown messages from the raffle timer, then applies StreamElements-style multi-winner bands.
11. `!sraffle <points>` opens the same timed window but always resolves to one winner.
12. If raffle command input omits points, both commands fall back to `5000`.
13. Raffle winners receive command-specified points (or default `Raffle.WinnerPoints`) each, and the raffle result message includes the awarded points.
14. `!rjoin` is free: joining never spends points, and joined entrants are eligible regardless of balance.
15. `!watchtime` returns lifetime rewarded watch minutes, not just the current stream session.
16. Points are always rounded to whole numbers across rewards, command adjustments, heist stakes/payouts, and balance responses.
