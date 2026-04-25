# TwitchHeists

TwitchHeists is a Streamer.bot integration that tracks watchtime, awards points with subscriber multipliers, supports balance commands including bulk mod adjustments for active viewers, and runs Twitch heists against a local SQLite database.

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

Mixed-load benchmark command:

```powershell
dotnet test .\tests\TwitchHeists.Tests\TwitchHeists.Tests.csproj --filter MixedLoadPerformanceTests --logger "console;verbosity=detailed"
```

The benchmark runs a 1,000-viewer mixed stream scenario and prints timings for the community refresh phase, chat burst phase, heist start/join/resolve phases, and total runtime.

Use this folder for Streamer.bot deployment:

- `src\TwitchHeists.StreamerBot.Bridge\bin\Release\net48\`

Keep the full output folder together so the bridge can load its dependent DLLs, `appsettings.json`, `heist-messages.json`, and the SQLite provider files. The bridge also builds a `netstandard2.0` target for automated tests, but Streamer.bot users should use the `net48` output.

Streamer.bot inline C# should load `TwitchHeists.StreamerBot.Bridge.dll` from that install folder at runtime with `Assembly.LoadFrom(...)` rather than relying on a direct external action reference.

## Database and configuration

The default configuration files are copied into the bridge output as `appsettings.json` and `heist-messages.json`.

Default SQLite location:

```json
{
  "ConnectionStrings": {
    "TwitchHeists": "Data Source=.\\data\\twitch-heists.db"
  }
}
```

The bridge resolves the database under `data\twitch-heists.db` inside the install folder and creates the `data\` directory on first use.

`heist-messages.json` controls all heist chat text:

- start messages
- cooldown messages
- countdown reminders
- insufficient-crew results
- success headlines and callouts
- failure headlines and callouts
- final result summaries

Edit that JSON file in the deployed bridge folder to add, remove, or rewrite heist lines without rebuilding the DLLs.

## Streamer.bot wiring

Set up Execute C# Code actions in Streamer.bot and call the bridge layer through a runtime loader:

1. **Community refresh** every 5 minutes with `RefreshCommunityViewers`
2. **Chat presence** on every chat message with `RecordChatPresence`
3. **Stream start** on your go-live trigger with `StartStream`
4. **Stream end** on your offline trigger with `EndStream`
5. **Heist start** on `!heist <amount>` with `StartHeist`
6. **Heist join** on `!join <amount>` with `JoinHeist`
7. **Heist resolution** on a short timer with `ResolveDueHeists`
8. **Points add** on `!points add <user> <amount>` with `AddPoints`
9. **Points remove** on `!points remove <user> <amount>` with `RemovePoints`
10. **Points give** on `!points give <user> <amount>` with `GivePoints`
11. **Watchtime** on `!watchtime` or `!watchtime <user>` with `GetWatchtime`

All bridge methods return a `BridgeResult` with `Success`, `Message`, `RewardedViewerCount`, `ExpiredViewerCount`, and `TotalPointsAwarded`.

## Default reward settings

- Reward interval: `00:05:00`
- Base points: `10`
- Tier 1 multiplier: `1.5x`
- Tier 2 multiplier: `2.0x`
- Tier 3 multiplier: `3.0x`

## Default heist settings

- Join window: `00:02:00`
- Cooldown window: `00:05:00`
- Reminder thresholds: `00:01:00`, `00:00:30`, `00:00:10`
- Success chance floor: `40%`
- Success chance ceiling: `75%`
- Minimum players: `2`
- Winner bands: adaptive by crew size (for example `2-5 => 1-5`, `6-10 => 3-7`, `51-60 => 12-16`, `141-150 => 21-34`)
- Maximum named resolution callouts: `2`
- Maximum winner count: `5` fallback only when no winner band applies
- Success pot multiplier: `2x`

## Behavior notes

- Community refreshes are processed as a single cycle against SQLite instead of one write per viewer.
- Reward cycles are idempotent per timestamp, so retries do not double-award watchtime or points.
- Chat-only presence expires at the next refresh boundary if the viewer never appears in the Community snapshot.
- Watch streaks only qualify while an explicit stream is active; `StartStream` marks the stream live, `EndStream` marks it offline, and off-stream chat does not advance streaks.
- A viewer gets streak points silently on the first qualifying sighting of a live stream: streak `1` awards `100`, streak `2` awards `200`, and so on.
- If a viewer missed the previous completed stream, their next qualifying sighting restarts the streak at `1` instead of continuing the old chain.
- Heist stake reservations and resolutions run inside transactions so points and round state stay aligned.
- `!heist` opens a 2-minute join window, sends 1m / 30s / 10s countdown reminders through the heist timer action, and enforces a 5-minute cooldown after results.
- Heists with fewer than `MinimumPlayers` resolve as **not enough crew** and refund every joined stake instead of forcing a win or loss.
- Successful heists now choose a winner count from the configured adaptive winner bands instead of always using a fixed cap.
- Resolved heist messages stay summary-first and only use a small number of named callouts, controlled by `MaximumNamedResolutionCallouts`.
- `heist-messages.json` drives every heist chat line with placeholder tokens such as `{starter}`, `{stake}`, `{joinWindow}`, `{cooldownRemaining}`, `{countdown}`, `{pot}`, `{participantCount}`, `{winner}`, `{loser}`, `{payout}`, `{winnerCount}`, `{loserCount}`, `{resolvedPot}`, and `{successChancePercent}`. The new `insufficientCrewMessages` group uses the same token set, especially `{participantCount}` and `{resolvedPot}`.
- Balances, watchtime, and heist stakes now use Twitch user ID as the canonical identity whenever Streamer.bot provides it, then fall back to usernames for older rows and older command payloads.
- When a legacy username-only balance row is next seen with a Twitch user ID, TwitchHeists adopts that row into the Twitch-ID-backed identity so future Twitch renames do not split points or watchtime.
- `!points give` transfers existing balance from the sender to the target; it does not mint new points.
- `!points add all` and `!points remove all` target viewers currently marked active in TwitchHeists presence tracking.
- `!watchtime` reads lifetime rewarded watch minutes from the same SQLite balance store.
