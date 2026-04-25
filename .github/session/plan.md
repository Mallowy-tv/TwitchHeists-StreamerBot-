# Points And Watchtime Commands Implementation Plan

**Goal:** Add Streamer.bot-friendly `!points add`, `!points remove`, `!points give`, and `!watchtime` commands on top of the existing SQLite balance and watchtime store.
**Architecture:** Reuse `viewer_balances` in `ViewerRepository` for both balance operations and watchtime lookups. Add transactional balance methods for the points commands, then expose both the new points actions and a new read-only watchtime action through the existing StreamerBot service layer and bridge layer so Streamer.bot actions can call them like the current heist commands. `remove` will clamp the recipient balance at zero, `give` will fail when the sender cannot afford the transfer, and `!watchtime` will return lifetime watchtime for the caller or an optional looked-up username.
**Tech Stack:** C#, .NET Standard 2.0 libraries, .NET Framework 4.8 bridge assembly, SQLite, xUnit.

## File map

| Action | Path | Responsibility |
|---|---|---|
| Modify | `src\TwitchHeists.Data.Sqlite\Repositories\ViewerRepository.cs` | Add transactional balance transfer, moderator adjustments, and lifetime watchtime lookup |
| Create | `src\TwitchHeists.StreamerBot\Contracts\PointsCommandDto.cs` | Internal StreamerBot DTO for add/remove/give point commands |
| Create | `src\TwitchHeists.StreamerBot\Contracts\WatchtimeQueryDto.cs` | Internal StreamerBot DTO for watchtime lookups |
| Create | `src\TwitchHeists.StreamerBot\Services\AddPointsAction.cs` | Moderator-only balance increase action |
| Create | `src\TwitchHeists.StreamerBot\Services\RemovePointsAction.cs` | Moderator-only balance decrease action with zero clamp |
| Create | `src\TwitchHeists.StreamerBot\Services\GivePointsAction.cs` | Viewer-to-viewer points transfer action |
| Create | `src\TwitchHeists.StreamerBot\Services\GetWatchtimeAction.cs` | Lifetime watchtime lookup action for self or optional target user |
| Modify | `src\TwitchHeists.StreamerBot\Composition\ActionRuntimeFactory.cs` | Wire the new points and watchtime actions into the existing runtime factory |
| Create | `src\TwitchHeists.StreamerBot.Bridge\Models\BridgePointsCommand.cs` | Bridge-facing DTO for points commands |
| Create | `src\TwitchHeists.StreamerBot.Bridge\Models\BridgeWatchtimeQuery.cs` | Bridge-facing DTO for watchtime lookups |
| Modify | `src\TwitchHeists.StreamerBot.Bridge\Services\BridgeActions.cs` | Add bridge wrappers for add/remove/give/watchtime |
| Modify | `tests\TwitchHeists.Tests\BridgeActionsTests.cs` | Cover bridge-level add/remove/give/watchtime behavior and friendly messages |
| Create | `tests\TwitchHeists.Tests\PointsActionsTests.cs` | Cover transfer rules, moderator adjustments, and zero clamp |
| Create | `tests\TwitchHeists.Tests\WatchtimeActionsTests.cs` | Cover lifetime watchtime lookups for self and optional target users |
| Modify | `.github\docs\streamerbot-install-guide.md` | Document the new commands, permissions, and loader-based snippets |
| Modify | `README.md` | Mention the new points and watchtime command capability |

## Tasks

### Task 1: Add repository balance and watchtime operations

**Files:**
- Modify: `src\TwitchHeists.Data.Sqlite\Repositories\ViewerRepository.cs`

- [ ] **Step 1: Add a transfer method**
Create a method that moves points from one normalized username to another inside one SQLite transaction. Validate that the amount is greater than zero, fail if the sender balance is too low, create the recipient row if missing, and update both balances with a single commit.

- [ ] **Step 2: Add moderator adjustment methods**
Create one method to add points to a target user and one method to remove points from a target user. The remove path must clamp the final balance at `0` instead of allowing negative values.

- [ ] **Step 3: Add a lifetime watchtime lookup**
Create a repository method that returns the stored `total_watch_minutes` for a normalized username and returns `0` when the user has never been rewarded.

- [ ] **Step 4: Verify repository compile surface**
Run: `dotnet build .\TwitchHeists.sln`
Expected: the solution still builds with the new repository methods referenced nowhere yet.

### Task 2: Write failing tests for points and watchtime commands

**Files:**
- Create: `tests\TwitchHeists.Tests\PointsActionsTests.cs`
- Create: `tests\TwitchHeists.Tests\WatchtimeActionsTests.cs`
- Modify: `tests\TwitchHeists.Tests\BridgeActionsTests.cs`

- [ ] **Step 1: Add points action-level tests**
Write failing tests for:
1. `GivePointsAction` transfers points from sender to recipient;
2. `GivePointsAction` fails when the sender does not have enough points;
3. `RemovePointsAction` clamps the target balance at zero;
4. `AddPointsAction` increases the target balance.

- [ ] **Step 2: Add watchtime action-level tests**
Write failing tests for:
1. `GetWatchtimeAction` returns the caller lifetime watchtime when no target username is supplied;
2. `GetWatchtimeAction` returns the target user lifetime watchtime when a username is supplied;
3. `GetWatchtimeAction` returns zero for a user with no stored watchtime.

- [ ] **Step 3: Add bridge-level tests**
Write failing bridge tests for:
1. `GivePoints` returning a friendly failure message when the sender lacks funds;
2. `AddPoints` succeeding with a simple `BridgeResult`;
3. `RemovePoints` succeeding while clamping a low balance to zero;
4. `GetWatchtime` returning a simple success message for a stored lifetime total.

- [ ] **Step 4: Run the focused test filter**
Run: `dotnet test .\tests\TwitchHeists.Tests\TwitchHeists.Tests.csproj --filter "Points|Watchtime|Bridge"`
Expected: the new tests fail because the DTOs, services, and bridge methods do not exist yet.

### Task 3: Add StreamerBot DTOs and services

**Files:**
- Create: `src\TwitchHeists.StreamerBot\Contracts\PointsCommandDto.cs`
- Create: `src\TwitchHeists.StreamerBot\Contracts\WatchtimeQueryDto.cs`
- Create: `src\TwitchHeists.StreamerBot\Services\AddPointsAction.cs`
- Create: `src\TwitchHeists.StreamerBot\Services\RemovePointsAction.cs`
- Create: `src\TwitchHeists.StreamerBot\Services\GivePointsAction.cs`
- Create: `src\TwitchHeists.StreamerBot\Services\GetWatchtimeAction.cs`

- [ ] **Step 1: Create the internal command/query DTOs**
Add a points DTO with source user, target user, source display name, target display name, amount, and command timestamp fields. Add a watchtime query DTO with requester username, requester display name, optional target username, optional target display name, and command timestamp fields.

- [ ] **Step 2: Implement `AddPointsAction`**
Call the repository add method, reject non-positive amounts, and return a success message such as `"<target> received 250 points."`

- [ ] **Step 3: Implement `RemovePointsAction`**
Call the repository remove method, reject non-positive amounts, and return a success message that includes the amount removed and the target balance after clamping.

- [ ] **Step 4: Implement `GivePointsAction`**
Call the repository transfer method, reject non-positive amounts, and return a success message that names both users and the transferred amount. Convert repository failures into friendly action messages.

- [ ] **Step 5: Implement `GetWatchtimeAction`**
Normalize the lookup username, read lifetime watch minutes from the repository, format the total into a friendly message such as `"<user> has watched for 2h 15m total."`, and support both self lookup and optional target lookup.

- [ ] **Step 6: Run the focused action test filter**
Run: `dotnet test .\tests\TwitchHeists.Tests\TwitchHeists.Tests.csproj --filter "PointsActions|WatchtimeActions"`
Expected: action-level tests now pass while bridge tests still fail.

### Task 4: Wire the new actions into runtime and bridge layers

**Files:**
- Modify: `src\TwitchHeists.StreamerBot\Composition\ActionRuntimeFactory.cs`
- Create: `src\TwitchHeists.StreamerBot.Bridge\Models\BridgePointsCommand.cs`
- Create: `src\TwitchHeists.StreamerBot.Bridge\Models\BridgeWatchtimeQuery.cs`
- Modify: `src\TwitchHeists.StreamerBot.Bridge\Services\BridgeActions.cs`

- [ ] **Step 1: Extend the runtime factory**
Add `CreateAddPointsAction`, `CreateRemovePointsAction`, `CreateGivePointsAction`, and `CreateGetWatchtimeAction` methods that create a `ViewerRepository` and return the new services.

- [ ] **Step 2: Add bridge DTOs**
Create a bridge-facing points DTO with source username, source display name, target username, target display name, amount, and occurred-at fields. Create a bridge-facing watchtime DTO with requester username, requester display name, optional target username, optional target display name, and occurred-at fields.

- [ ] **Step 3: Add bridge wrapper methods**
Add `AddPoints`, `RemovePoints`, `GivePoints`, and `GetWatchtime` methods to `BridgeActions`, map the bridge DTOs to the new internal DTOs, and return `BridgeResult` exactly like the existing heist wrappers.

- [ ] **Step 4: Re-run the focused bridge tests**
Run: `dotnet test .\tests\TwitchHeists.Tests\TwitchHeists.Tests.csproj --filter "Points|Watchtime|Bridge"`
Expected: both action and bridge tests pass.

### Task 5: Update Streamer.bot docs

**Files:**
- Modify: `.github\docs\streamerbot-install-guide.md`
- Modify: `README.md`

- [ ] **Step 1: Document command behavior**
Add a section that states:
1. `!points add <user> <amount>` is mod-only;
2. `!points remove <user> <amount>` is mod-only and clamps to zero;
3. `!points give <user> <amount>` is available to everyone and subtracts from the sender balance;
4. `!watchtime` shows the caller lifetime watchtime;
5. `!watchtime <user>` looks up the lifetime watchtime for another user.

- [ ] **Step 2: Add loader-based snippets**
Add Streamer.bot loader snippets for all three points commands and the `!watchtime` command using the same `RegisterAssemblyResolver()` pattern as the current heist snippets.

- [ ] **Step 3: Update the README capability summary**
Mention points transfer, moderator balance adjustments, and lifetime watchtime lookup alongside watchtime accrual, points accrual, and heists.

### Task 6: Run final verification

**Files:**
- Modify docs or action messages if verification finds mismatches

- [ ] **Step 1: Run the full test suite**
Run: `dotnet test .\TwitchHeists.sln`
Expected: all existing tests plus the new points and watchtime tests pass.

- [ ] **Step 2: Build release output**
Run: `dotnet build .\TwitchHeists.sln -c Release`
Expected: the solution builds successfully and the bridge release output still contains the deployable files.

- [ ] **Step 3: Confirm docs match behavior**
Review the command names, permission notes, zero-clamp rule, and lifetime watchtime behavior in `.github\docs\streamerbot-install-guide.md` against the implemented services and bridge methods.

## Notes

- Use the same normalized username rules the heist actions already use so transfers, adjustments, and watchtime lookups land on the same balance rows as watchtime rewards.
- Keep permission enforcement at the Streamer.bot trigger/snippet layer for moderator-only commands; the library layer should focus on balance rules and messages.
- `give` should never mint points. It must fail when the sender cannot cover the requested amount.
- `!watchtime` is lifetime-only in this plan because the repository already stores lifetime rewarded minutes in `viewer_balances.total_watch_minutes`.
