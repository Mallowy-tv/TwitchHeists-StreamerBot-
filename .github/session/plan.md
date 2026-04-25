# Heist Conversation Templates Implementation Plan

**Goal:** Move all heist chat text into a dedicated JSON template file so start, reminder, cooldown, success, and failure messages can be changed without rebuilding the DLLs.
**Architecture:** Keep the message-template feature inside the StreamerBot-facing layer because it is presentation behavior, not heist math or persistence. Ship a `heist-messages.json` file alongside `appsettings.json`, load it through the existing bridge/runtime setup, and have the start and resolver actions compose chat lines from typed template data plus live round data.
**Tech Stack:** C#, .NET, System.Text.Json, Streamer.bot bridge, xUnit.

## File map

| Action | Path | Responsibility |
|---|---|---|
| Create | `src\TwitchHeists.StreamerBot\Configuration\heist-messages.json` | Default editable heist conversation templates copied into the deployment output. |
| Create | `src\TwitchHeists.StreamerBot\Configuration\HeistMessageTemplates.cs` | Strongly typed models for start, reminder, cooldown, success, and failure template groups. |
| Create | `src\TwitchHeists.StreamerBot\Services\HeistMessageTemplateLoader.cs` | Load `heist-messages.json`, validate shape, and fall back to defaults when the file is missing or blank. |
| Create | `src\TwitchHeists.StreamerBot\Services\HeistMessageComposer.cs` | Build final chat lines from templates and runtime data such as usernames, pot, winners, losers, and countdown text. |
| Modify | `src\TwitchHeists.StreamerBot\Services\StartHeistAction.cs` | Replace hard-coded start and cooldown text with template-driven messages. |
| Modify | `src\TwitchHeists.StreamerBot\Services\ResolveDueHeistsAction.cs` | Replace hard-coded reminder and result text with template-driven messages and structured flavor callouts. |
| Modify | `src\TwitchHeists.StreamerBot\Composition\ActionRuntimeFactory.cs` | Load the template file and inject the composer into the heist actions. |
| Modify | `src\TwitchHeists.StreamerBot\TwitchHeists.StreamerBot.csproj` | Copy `heist-messages.json` into the action-layer output. |
| Modify | `src\TwitchHeists.StreamerBot.Bridge\Services\BridgeRuntimeFactory.cs` | Resolve the install-folder path for `heist-messages.json` alongside `appsettings.json`. |
| Modify | `src\TwitchHeists.StreamerBot.Bridge\TwitchHeists.StreamerBot.Bridge.csproj` | Link and copy `heist-messages.json` into the bridge deployment output. |
| Modify | `tests\TwitchHeists.Tests\HeistActionsTests.cs` | Add TDD coverage for template-driven start, reminder, cooldown, and result output. |
| Modify | `tests\TwitchHeists.Tests\BridgeActionsTests.cs` | Verify the bridge loads shipped templates from the install folder and returns the new messages. |
| Modify | `README.md` | Document that heist chat wording now lives in `heist-messages.json`. |
| Modify | `.github\docs\streamerbot-install-guide.md` | Document where to place `heist-messages.json`, how to edit it, and that changes do not require rebuilding. |

## Tasks

### Task 1: Define the editable heist message asset

**Files:**
- Create: `src\TwitchHeists.StreamerBot\Configuration\heist-messages.json`
- Create: `src\TwitchHeists.StreamerBot\Configuration\HeistMessageTemplates.cs`
- Modify: `src\TwitchHeists.StreamerBot\TwitchHeists.StreamerBot.csproj`
- Modify: `src\TwitchHeists.StreamerBot.Bridge\TwitchHeists.StreamerBot.Bridge.csproj`

- [ ] **Step 1: Create the default JSON template file**
Write `src\TwitchHeists.StreamerBot\Configuration\heist-messages.json` with grouped arrays for each chat surface:
```json
{
  "startMessages": [
    "{starter} started a heist with {stake} points. Starting in {joinWindow}.",
    "{starter} is putting together a crew for {stake} points. The heist starts in {joinWindow}."
  ],
  "cooldownMessages": [
    "The crew is laying low. A new heist can start in {cooldownRemaining}.",
    "The heat is still on. Try another heist in {cooldownRemaining}."
  ],
  "reminderMessages": [
    "Heist starts in {countdown}. Pot is now {pot} points across {participantCount} viewers.",
    "{participantCount} crew members are ready. The heist starts in {countdown} with {pot} points on the line."
  ],
  "successHeadlines": [
    "The crew cracked the vault and got away clean.",
    "The crew blasted through the alarms and escaped with the haul."
  ],
  "failureHeadlines": [
    "Police captured the whole crew before anyone escaped.",
    "The crew got boxed in and everyone was gunned down."
  ],
  "successCallouts": [
    "{winner} slipped out with {payout} points.",
    "{winner} dove into the van with {payout} points."
  ],
  "failureCallouts": [
    "{loser} got left behind in the crossfire.",
    "{loser} took a bullet covering the escape."
  ],
  "sacrificeCallouts": [
    "{loser} took a bullet for {winner}.",
    "{loser} held the line so {winner} could escape."
  ],
  "resultSummaries": [
    "{winnerCount} got out with {resolvedPot} points. Success chance was {successChancePercent}.",
    "{loserCount} went down. Success chance was {successChancePercent}."
  ]
}
```

- [ ] **Step 2: Add typed template models**
Create `HeistMessageTemplates.cs` with one root type and array-backed properties for each group above so deserialization stays explicit and tests can construct templates in memory without reading the file system.

- [ ] **Step 3: Ship the template file with the build output**
Add `<None Include="Configuration\heist-messages.json">` to `src\TwitchHeists.StreamerBot\TwitchHeists.StreamerBot.csproj` and a linked copy entry in `src\TwitchHeists.StreamerBot.Bridge\TwitchHeists.StreamerBot.Bridge.csproj`, matching the existing `appsettings.json` deployment pattern.

### Task 2: Load templates from the install folder

**Files:**
- Create: `src\TwitchHeists.StreamerBot\Services\HeistMessageTemplateLoader.cs`
- Modify: `src\TwitchHeists.StreamerBot\Composition\ActionRuntimeFactory.cs`
- Modify: `src\TwitchHeists.StreamerBot.Bridge\Services\BridgeRuntimeFactory.cs`

- [ ] **Step 1: Implement a loader with default fallback**
Create `HeistMessageTemplateLoader.cs` with a method that:
1. reads `heist-messages.json` when the file exists,
2. deserializes to `HeistMessageTemplates`,
3. treats missing or empty arrays as invalid for that group,
4. falls back to an in-memory default template set when the file is missing.

- [ ] **Step 2: Resolve the template file path from the bridge**
Update `BridgeRuntimeFactory` so the install directory resolves both `appsettings.json` and `heist-messages.json`, then pass the template path into `ActionRuntimeFactory.CreateStartHeistAction(...)` and `CreateResolveDueHeistsAction(...)`.

- [ ] **Step 3: Inject templates into the action runtime**
Update `ActionRuntimeFactory` to construct one `HeistMessageComposer` per action using the loaded templates, while keeping the existing `HeistSettings` and repository wiring unchanged.

### Task 3: Add failing tests for template-driven heist chat

**Files:**
- Modify: `tests\TwitchHeists.Tests\HeistActionsTests.cs`
- Modify: `tests\TwitchHeists.Tests\BridgeActionsTests.cs`

- [ ] **Step 1: Add start and cooldown message tests**
Add tests that create a custom template set in memory and expect `StartHeistAction` to emit the selected start string and the selected cooldown string instead of the current hard-coded wording.

- [ ] **Step 2: Add reminder and result message tests**
Add tests that expect `ResolveDueHeistsAction` to:
1. use the reminder template text at 1 minute, 30 seconds, and 10 seconds,
2. use a success headline plus success callouts plus summary when the heist succeeds,
3. use a failure headline plus failure callouts plus summary when the heist fails.

- [ ] **Step 3: Add bridge file-loading tests**
Write bridge tests that place a custom `heist-messages.json` inside the temporary install directory and verify `BridgeActions.StartHeist(...)` and `BridgeActions.ResolveDueHeists(...)` return text from that file.

- [ ] **Step 4: Confirm the tests fail first**
Run: `dotnet test .\tests\TwitchHeists.Tests\TwitchHeists.Tests.csproj --filter "Heist|Bridge"`
Expected: failures showing the actions still use hard-coded messages and do not yet load the custom template file.

### Task 4: Implement the heist message composer

**Files:**
- Create: `src\TwitchHeists.StreamerBot\Services\HeistMessageComposer.cs`
- Modify: `src\TwitchHeists.StreamerBot\Services\StartHeistAction.cs`
- Modify: `src\TwitchHeists.StreamerBot\Services\ResolveDueHeistsAction.cs`

- [ ] **Step 1: Build placeholder replacement**
Implement `HeistMessageComposer` so it replaces placeholders such as `{starter}`, `{stake}`, `{joinWindow}`, `{cooldownRemaining}`, `{countdown}`, `{pot}`, `{participantCount}`, `{winner}`, `{loser}`, `{winnerCount}`, `{loserCount}`, `{resolvedPot}`, and `{successChancePercent}`.

- [ ] **Step 2: Randomize within a template group safely**
Have the composer choose one string from each relevant array. Keep the random selection inside the composer so the actions remain deterministic in tests by allowing a controllable selector or random delegate.

- [ ] **Step 3: Switch start and cooldown messaging**
Update `StartHeistAction` to ask the composer for:
1. a start message after the round is created,
2. a cooldown message when `GetActiveCooldownEndsAtUtc(...)` blocks a new heist.

- [ ] **Step 4: Switch reminder and result messaging**
Update `ResolveDueHeistsAction` to ask the composer for:
1. the countdown reminder text,
2. the final success or failure message built as one headline, up to two callouts, and one short summary.

- [ ] **Step 5: Keep flavor structured and bounded**
Cap final output to a chat-friendly structure:
1. exactly one headline,
2. zero to two callouts depending on available winners and losers,
3. exactly one summary sentence.
This keeps the message readable even when many viewers join.

- [ ] **Step 6: Re-run focused tests**
Run: `dotnet test .\tests\TwitchHeists.Tests\TwitchHeists.Tests.csproj --filter "Heist|Bridge"`
Expected: all focused heist and bridge tests pass with custom-template coverage.

### Task 5: Document the external message file

**Files:**
- Modify: `README.md`
- Modify: `.github\docs\streamerbot-install-guide.md`

- [ ] **Step 1: Document the new deployment artifact**
Update both docs to state that the release output now includes `heist-messages.json` and that Streamer.bot users must keep it beside `TwitchHeists.StreamerBot.Bridge.dll` and `appsettings.json`.

- [ ] **Step 2: Document how to customize without rebuilding**
Add a short example showing that users can add, remove, or edit message strings in `heist-messages.json`, then save the file and let the next action run pick up the new text.

- [ ] **Step 3: Document supported placeholders**
List the supported placeholder names for each message group so users know which tokens are safe to edit.

### Task 6: Full verification

**Files:**
- Modify: none

- [ ] **Step 1: Run the full solution tests**
Run: `dotnet test .\TwitchHeists.sln`
Expected: all tests passing, zero failures.

- [ ] **Step 2: Run the release build**
Run: `dotnet build .\TwitchHeists.sln -c Release`
Expected: build succeeds for all projects and the bridge output contains `heist-messages.json`.

- [ ] **Step 3: Record the rollout note**
Note that Streamer.bot deployments must copy the updated `net48` bridge output folder so the new JSON template file is present beside the DLLs.

## Notes

- Keep the conversation-template feature in `TwitchHeists.StreamerBot` and `TwitchHeists.StreamerBot.Bridge`; do not move it into `TwitchHeists.Core` because it is chat-presentation behavior.
- Prefer a dedicated `heist-messages.json` file over adding large template arrays to `appsettings.json` so users can manage message text separately from timing and numeric heist settings.
- The composer should still work when a heist has only one participant, no winners, or no losers by choosing only the callout groups that have valid data for that outcome.
