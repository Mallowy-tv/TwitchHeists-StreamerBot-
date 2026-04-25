# Bulk Points All Implementation Plan

**Goal:** Add an `all` target for mod-only `!points add` and `!points remove` so those commands affect every viewer currently active in the TwitchHeists presence table.
**Architecture:** Reuse `viewer_presence.is_active = 1` as the source of truth for “currently present in this stream session.” Extend the repository with active-viewer bulk adjustment support, then teach the existing add/remove StreamerBot actions to treat `TargetUsername == "all"` as a bulk operation while leaving `!points give` single-target only. Keep the bridge surface stable so Streamer.bot snippets only need to pass the literal target string `all`.
**Tech Stack:** C#, .NET Standard 2.0 libraries, .NET Framework 4.8 bridge assembly, SQLite, xUnit.

## File map

| Action | Path | Responsibility |
|---|---|---|
| Modify | `src\TwitchHeists.Data.Sqlite\Repositories\ViewerRepository.cs` | Query active viewers and apply bulk add/remove operations transactionally |
| Modify | `src\TwitchHeists.StreamerBot\Services\AddPointsAction.cs` | Detect `all`, apply bulk add to active viewers, and return a summary message |
| Modify | `src\TwitchHeists.StreamerBot\Services\RemovePointsAction.cs` | Detect `all`, apply bulk remove with per-viewer clamp, and return a summary message |
| Modify | `tests\TwitchHeists.Tests\PointsActionsTests.cs` | Cover bulk add/remove behavior, empty-active-list handling, and single-target regression safety |
| Modify | `tests\TwitchHeists.Tests\BridgeActionsTests.cs` | Verify bridge-level success messages for bulk add/remove |
| Modify | `.github\docs\streamerbot-install-guide.md` | Document `all` for mod add/remove and note that it targets active presence |
| Modify | `README.md` | Mention bulk moderator adjustments for active viewers |

## Tasks

### Task 1: Add active-viewer bulk repository operations

**Files:**
- Modify: `src\TwitchHeists.Data.Sqlite\Repositories\ViewerRepository.cs`

- [ ] **Step 1: Add active-viewer lookup**
Create a repository method that returns the active viewer usernames from `viewer_presence` where `is_active = 1`. Return the normalized usernames already stored in the table so balance updates land on the same rows used by watchtime rewards.

- [ ] **Step 2: Add bulk add operation**
Create a repository method that accepts a sequence of normalized usernames, a positive amount, and a timestamp, then applies the same point addition to every user inside one SQLite transaction. Create missing `viewer_balances` rows as needed and return the count of updated viewers.

- [ ] **Step 3: Add bulk remove operation**
Create a repository method that accepts a sequence of normalized usernames, a positive amount, and a timestamp, then subtracts the amount from each target inside one SQLite transaction. Clamp each viewer balance at `0` and return the count of updated viewers.

- [ ] **Step 4: Verify the repository surface**
Run: `dotnet build .\TwitchHeists.sln`
Expected: the solution builds with the new repository APIs available for the action layer.

### Task 2: Write failing tests for `all`

**Files:**
- Modify: `tests\TwitchHeists.Tests\PointsActionsTests.cs`
- Modify: `tests\TwitchHeists.Tests\BridgeActionsTests.cs`

- [ ] **Step 1: Add action-level bulk tests**
Write failing tests for:
1. `AddPointsAction` adding points to every active viewer when `TargetUsername = "all"`;
2. `RemovePointsAction` subtracting from every active viewer and clamping each balance at zero;
3. `AddPointsAction` returning a bulk summary message for `all` that does **not** include `"Balance is now"`;
4. `RemovePointsAction` returning a bulk summary message for `all` that does **not** include `"Balance is now"`;
5. `AddPointsAction` returning a friendly failure when `all` is used but there are no active viewers;
6. `RemovePointsAction` returning a friendly failure when `all` is used but there are no active viewers.

- [ ] **Step 2: Preserve single-target coverage**
Keep one existing single-target add test and one existing single-target remove test green so the new `all` branch does not replace the normal per-user behavior.

- [ ] **Step 3: Add bridge-level bulk tests**
Write failing bridge tests for:
1. `AddPoints` with target `all` returning a success message that names the active-viewer count;
2. `RemovePoints` with target `all` returning a success message that names the active-viewer count.

- [ ] **Step 4: Run the focused test filter**
Run: `dotnet test .\tests\TwitchHeists.Tests\TwitchHeists.Tests.csproj --filter "Points|Bridge"`
Expected: the new bulk tests fail because the action and repository layers do not handle `all` yet.

### Task 3: Implement `all` in the StreamerBot points actions

**Files:**
- Modify: `src\TwitchHeists.StreamerBot\Services\AddPointsAction.cs`
- Modify: `src\TwitchHeists.StreamerBot\Services\RemovePointsAction.cs`

- [ ] **Step 1: Implement bulk add branch**
In `AddPointsAction`, detect `TargetUsername` equal to `all` after normalization. Load the active normalized usernames from the repository, fail with a friendly message if the list is empty, call the new bulk add repository method, and return a summary such as `"18 active viewers each received 500 points."` Do not include any `"Balance is now X"` wording in the bulk response.

- [ ] **Step 2: Implement bulk remove branch**
In `RemovePointsAction`, detect `TargetUsername` equal to `all` after normalization. Load the active normalized usernames from the repository, fail with a friendly message if the list is empty, call the new bulk remove repository method, and return a summary such as `"18 active viewers each lost 500 points."` Do not include any `"Balance is now X"` wording in the bulk response.

- [ ] **Step 3: Preserve single-target behavior**
Keep the existing single-target path unchanged for any target other than `all`, including the current success wording and per-user balance lookup.

- [ ] **Step 4: Run the focused action test filter**
Run: `dotnet test .\tests\TwitchHeists.Tests\TwitchHeists.Tests.csproj --filter "PointsActions"`
Expected: bulk and single-target action tests pass.

### Task 4: Verify bridge behavior for `all`

**Files:**
- Modify: `tests\TwitchHeists.Tests\BridgeActionsTests.cs`

- [ ] **Step 1: Re-run the bridge filter**
Run: `dotnet test .\tests\TwitchHeists.Tests\TwitchHeists.Tests.csproj --filter "BridgeActionsTests"`
Expected: bridge tests pass without changing the bridge DTO or method signatures.

- [ ] **Step 2: Confirm command contract stability**
Review `src\TwitchHeists.StreamerBot.Bridge\Services\BridgeActions.cs` and confirm no code changes are needed because the bridge should pass the literal target string `all` through the existing `BridgePointsCommand`.

### Task 5: Update the Streamer.bot guide

**Files:**
- Modify: `.github\docs\streamerbot-install-guide.md`
- Modify: `README.md`

- [ ] **Step 1: Document `all` behavior**
Add a note that `!points add all <amount>` and `!points remove all <amount>` target every viewer currently active in TwitchHeists presence tracking. State clearly that `!points give all` is not supported.

- [ ] **Step 2: Update the points command snippets**
In the full standalone `CPHInline` examples for add/remove, note that the target lookup sub-action should be skipped when the literal target is `all`, because there is no Twitch user to resolve in that case.

- [ ] **Step 3: Update the README capability summary**
Mention bulk moderator adjustments for currently active viewers alongside the existing points command summary.

### Task 6: Run final verification

**Files:**
- Modify docs or action messages if verification finds mismatches

- [ ] **Step 1: Run the full test suite**
Run: `dotnet test .\TwitchHeists.sln`
Expected: all tests pass, including new bulk add/remove coverage.

- [ ] **Step 2: Build release output**
Run: `dotnet build .\TwitchHeists.sln -c Release`
Expected: the solution builds successfully and the bridge release output remains deployable.

- [ ] **Step 3: Confirm docs match runtime behavior**
Review `.github\docs\streamerbot-install-guide.md` against the implemented `AddPointsAction` and `RemovePointsAction` behavior to confirm the guide accurately describes active-viewer targeting, unsupported `give all`, and when to skip target lookup.

## Notes

- Treat active viewers as whatever `ViewerRepository.GetActivePresence()` returns at command time. This includes chat fallback presence that has not expired yet and community-confirmed presence.
- Keep `all` support limited to `!points add` and `!points remove`; do not add bulk `give`.
- If `all` is used with no active viewers, return a friendly failure message instead of silently succeeding.
- Bulk `all` responses should stay aggregate-only; they must not include a single balance line such as `"Balance is now X"`.
- Use the existing normalized username rules so bulk updates hit the same balance rows as single-target commands and watchtime rewards.
