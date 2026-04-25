# TwitchHeists

TwitchHeists is a Streamer.bot integration that tracks watchtime, awards points with subscriber multipliers, supports balance commands, and runs Twitch heists against a local SQLite database.

For the Streamer.bot install flow, use `.github\docs\streamerbot-install-guide.md`.

## Projects

| Project | Purpose |
|---|---|
| `src\TwitchHeists.Core` | Watchtime, points, success-chance, and heist resolution rules |
| `src\TwitchHeists.Data.Sqlite` | SQLite schema, transactions, viewer persistence, balances, settings, and heist storage |
| `src\TwitchHeists.StreamerBot` | Internal action layer used by the bridge |
| `src\TwitchHeists.StreamerBot.Bridge` | Streamer.bot-facing bridge assembly |
| `tests\TwitchHeists.Tests` | Unit and repository tests |

## Build

```powershell
dotnet build .\TwitchHeists.sln
dotnet test .\TwitchHeists.sln
```

Use this folder for Streamer.bot deployment:

- `src\TwitchHeists.StreamerBot.Bridge\bin\Release\net48\`

Keep the full output folder together so the bridge can load its dependent DLLs, `appsettings.json`, and the SQLite provider files. The bridge also builds a `netstandard2.0` target for automated tests, but Streamer.bot users should use the `net48` output.

Streamer.bot inline C# should load `TwitchHeists.StreamerBot.Bridge.dll` from that install folder at runtime with `Assembly.LoadFrom(...)` rather than relying on a direct external action reference.

## Database and configuration

The default configuration file is copied into the bridge output as `appsettings.json`.

Default SQLite location:

```json
{
  "ConnectionStrings": {
    "TwitchHeists": "Data Source=.\\data\\twitch-heists.db"
  }
}
```

The bridge resolves the database under `data\twitch-heists.db` inside the install folder and creates the `data\` directory on first use.

## Streamer.bot wiring

Set up Execute C# Code actions in Streamer.bot and call the bridge layer through a runtime loader:

1. **Community refresh** every 5 minutes with `RefreshCommunityViewers`
2. **Chat presence** on every chat message with `RecordChatPresence`
3. **Heist start** on `!heist <amount>` with `StartHeist`
4. **Heist join** on `!join <amount>` with `JoinHeist`
5. **Heist resolution** on a short timer with `ResolveDueHeists`
6. **Points add** on `!points add <user> <amount>` with `AddPoints`
7. **Points remove** on `!points remove <user> <amount>` with `RemovePoints`
8. **Points give** on `!points give <user> <amount>` with `GivePoints`
9. **Watchtime** on `!watchtime` or `!watchtime <user>` with `GetWatchtime`

All bridge methods return a `BridgeResult` with `Success`, `Message`, `RewardedViewerCount`, `ExpiredViewerCount`, and `TotalPointsAwarded`.

## Default reward settings

- Reward interval: `00:05:00`
- Base points: `10`
- Tier 1 multiplier: `1.5x`
- Tier 2 multiplier: `2.0x`
- Tier 3 multiplier: `3.0x`

## Default heist settings

- Join window: `00:02:00`
- Success chance floor: `40%`
- Success chance ceiling: `75%`
- Max winners: `5`
- Success pot multiplier: `2x`

## Behavior notes

- Community refreshes are processed as a single cycle against SQLite instead of one write per viewer.
- Reward cycles are idempotent per timestamp, so retries do not double-award watchtime or points.
- Chat-only presence expires at the next refresh boundary if the viewer never appears in the Community snapshot.
- Heist stake reservations and resolutions run inside transactions so points and round state stay aligned.
- `!points give` transfers existing balance from the sender to the target; it does not mint new points.
- `!watchtime` reads lifetime rewarded watch minutes from the same SQLite balance store.
