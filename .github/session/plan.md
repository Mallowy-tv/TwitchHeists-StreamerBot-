# Adaptive Heist Winners Implementation Plan

**Goal:** Replace the fixed heist winner cap with the approved tiered winner bands, make the minimum crew size configurable in `HeistSettings`, refund underfilled crews as “not enough crew,” and keep resolved heist messages short even for large crews.
**Architecture:** The heist rule change stays inside the owning heist flow: core resolution calculates a winner count from configurable bands, checks participant count against a configurable `MinimumPlayers` threshold, the repository applies payouts or refunds transactionally, and the message composer keeps all visible heist text JSON-driven while limiting named callouts for large rounds. The plan also extends the heist resolution model so “insufficient crew” is a first-class result instead of being squeezed into the existing success/failure states.
**Tech Stack:** .NET 9 tests, netstandard2.0 runtime libraries, SQLite repositories, StreamerBot bridge/actions, JSON-configured heist message templates.

## Approved winner table (joined players => winner range)

| Joined | Winners | Joined | Winners | Joined | Winners |
|---:|---:|---:|---:|---:|---:|
| 1 | not enough crew | 51 | 12-16 | 101 | 17-26 |
| 2 | 1-2 | 52 | 12-16 | 102 | 17-26 |
| 3 | 1-3 | 53 | 12-16 | 103 | 17-26 |
| 4 | 1-4 | 54 | 12-16 | 104 | 17-26 |
| 5 | 1-5 | 55 | 12-16 | 105 | 17-26 |
| 6 | 3-6 | 56 | 12-16 | 106 | 17-26 |
| 7 | 3-7 | 57 | 12-16 | 107 | 17-26 |
| 8 | 3-7 | 58 | 12-16 | 108 | 17-26 |
| 9 | 3-7 | 59 | 12-16 | 109 | 17-26 |
| 10 | 3-7 | 60 | 12-16 | 110 | 17-26 |
| 11 | 4-8 | 61 | 13-18 | 111 | 18-28 |
| 12 | 4-8 | 62 | 13-18 | 112 | 18-28 |
| 13 | 4-8 | 63 | 13-18 | 113 | 18-28 |
| 14 | 4-8 | 64 | 13-18 | 114 | 18-28 |
| 15 | 4-8 | 65 | 13-18 | 115 | 18-28 |
| 16 | 5-9 | 66 | 13-18 | 116 | 18-28 |
| 17 | 5-9 | 67 | 13-18 | 117 | 18-28 |
| 18 | 5-9 | 68 | 13-18 | 118 | 18-28 |
| 19 | 5-9 | 69 | 13-18 | 119 | 18-28 |
| 20 | 5-9 | 70 | 13-18 | 120 | 18-28 |
| 21 | 6-10 | 71 | 14-20 | 121 | 19-30 |
| 22 | 6-10 | 72 | 14-20 | 122 | 19-30 |
| 23 | 6-10 | 73 | 14-20 | 123 | 19-30 |
| 24 | 6-10 | 74 | 14-20 | 124 | 19-30 |
| 25 | 6-10 | 75 | 14-20 | 125 | 19-30 |
| 26 | 7-11 | 76 | 14-20 | 126 | 19-30 |
| 27 | 7-11 | 77 | 14-20 | 127 | 19-30 |
| 28 | 7-11 | 78 | 14-20 | 128 | 19-30 |
| 29 | 7-11 | 79 | 14-20 | 129 | 19-30 |
| 30 | 7-11 | 80 | 14-20 | 130 | 19-30 |
| 31 | 8-12 | 81 | 15-22 | 131 | 20-32 |
| 32 | 8-12 | 82 | 15-22 | 132 | 20-32 |
| 33 | 8-12 | 83 | 15-22 | 133 | 20-32 |
| 34 | 8-12 | 84 | 15-22 | 134 | 20-32 |
| 35 | 8-12 | 85 | 15-22 | 135 | 20-32 |
| 36 | 9-13 | 86 | 15-22 | 136 | 20-32 |
| 37 | 9-13 | 87 | 15-22 | 137 | 20-32 |
| 38 | 9-13 | 88 | 15-22 | 138 | 20-32 |
| 39 | 9-13 | 89 | 15-22 | 139 | 20-32 |
| 40 | 9-13 | 90 | 15-22 | 140 | 20-32 |
| 41 | 10-14 | 91 | 16-24 | 141 | 21-34 |
| 42 | 10-14 | 92 | 16-24 | 142 | 21-34 |
| 43 | 10-14 | 93 | 16-24 | 143 | 21-34 |
| 44 | 10-14 | 94 | 16-24 | 144 | 21-34 |
| 45 | 10-14 | 95 | 16-24 | 145 | 21-34 |
| 46 | 11-15 | 96 | 16-24 | 146 | 21-34 |
| 47 | 11-15 | 97 | 16-24 | 147 | 21-34 |
| 48 | 11-15 | 98 | 16-24 | 148 | 21-34 |
| 49 | 11-15 | 99 | 16-24 | 149 | 21-34 |
| 50 | 11-15 | 100 | 16-24 | 150 | 21-34 |

## File map

| Action | Path | Responsibility |
|---|---|---|
| Create | `src\TwitchHeists.Core\Options\HeistWinnerBand.cs` | Value object for participant bands and winner ranges |
| Modify | `src\TwitchHeists.Core\Options\HeistSettings.cs` | Default minimum-player threshold, winner bands, and compact-message settings |
| Modify | `src\TwitchHeists.Core\Models\HeistRoundState.cs` | Add explicit insufficient-crew terminal state |
| Modify | `src\TwitchHeists.Core\Models\HeistResolutionResult.cs` | Carry refunded participants for insufficient-crew outcomes |
| Modify | `src\TwitchHeists.Core\Services\HeistResolver.cs` | Resolve winner count from approved bands and refund crews below the configured minimum |
| Modify | `src\TwitchHeists.Data.Sqlite\Repositories\HeistRepository.cs` | Refund insufficient-crew participants and persist the new terminal state |
| Modify | `src\TwitchHeists.StreamerBot\Configuration\HeistMessageTemplates.cs` | Add JSON template group for insufficient-crew results and defaults |
| Modify | `src\TwitchHeists.StreamerBot\Configuration\heist-messages.json` | Ship default insufficient-crew template in the deployable JSON file |
| Modify | `src\TwitchHeists.StreamerBot\Services\HeistMessageTemplateLoader.cs` | Load and sanitize the new template group |
| Modify | `src\TwitchHeists.StreamerBot\Services\HeistMessageComposer.cs` | Build compact adaptive resolution text and insufficient-crew messages |
| Modify | `src\TwitchHeists.StreamerBot\Services\ResolveDueHeistsAction.cs` | Surface the new insufficient-crew outcome and compact result text |
| Modify | `tests\TwitchHeists.Tests\HeistResolverTests.cs` | Failing-first core tests for winner bands, refunds, and payout safety |
| Modify | `tests\TwitchHeists.Tests\HeistActionsTests.cs` | Failing-first action tests for insufficient crew and compact resolved text |
| Modify | `tests\TwitchHeists.Tests\BridgeActionsTests.cs` | Bridge coverage proving JSON templates still control new heist outcomes |
| Modify | `README.md` | Document adaptive winner bands and configurable minimum-player refund behavior |
| Modify | `.github\docs\streamerbot-install-guide.md` | Document new heist message JSON group and approved winner-band rules |

### Task 1: Add adaptive heist configuration and result model

**Files:**
- Create: `src\TwitchHeists.Core\Options\HeistWinnerBand.cs`
- Modify: `src\TwitchHeists.Core\Options\HeistSettings.cs`
- Modify: `src\TwitchHeists.Core\Models\HeistRoundState.cs`
- Modify: `src\TwitchHeists.Core\Models\HeistResolutionResult.cs`
- Modify: `src\TwitchHeists.StreamerBot\Configuration\HeistMessageTemplates.cs`
- Modify: `src\TwitchHeists.StreamerBot\Configuration\heist-messages.json`
- Modify: `src\TwitchHeists.StreamerBot\Services\HeistMessageTemplateLoader.cs`

- [ ] **Step 1: Create the winner-band type**
Write `src\TwitchHeists.Core\Options\HeistWinnerBand.cs` with a small immutable type that holds:
1. `MinimumParticipants`
2. `MaximumParticipants`
3. `MinimumWinners`
4. `MaximumWinners`
and exposes helpers that answer “does this band contain this participant count?” and “what is the clamped winner range for this participant count?”.

- [ ] **Step 2: Add defaults to `HeistSettings`**
Modify `src\TwitchHeists.Core\Options\HeistSettings.cs` so it keeps:
1. the existing join/cooldown/success settings
2. a new `MinimumPlayers = 2` setting for insufficient-crew handling
3. a default `List<HeistWinnerBand>` matching the approved table above
4. a compact-message setting such as `MaximumNamedResolutionCallouts = 2`

- [ ] **Step 3: Extend the resolution model**
Modify `src\TwitchHeists.Core\Models\HeistRoundState.cs` to add a new terminal state for insufficient crew, and modify `src\TwitchHeists.Core\Models\HeistResolutionResult.cs` to add a `RefundedParticipants` collection so the repository can refund every participant in an underfilled crew cleanly.

- [ ] **Step 4: Extend JSON-configured heist templates**
Modify `src\TwitchHeists.StreamerBot\Configuration\HeistMessageTemplates.cs`, `src\TwitchHeists.StreamerBot\Configuration\heist-messages.json`, and `src\TwitchHeists.StreamerBot\Services\HeistMessageTemplateLoader.cs` so the template model includes an `insufficientCrewMessages` group and the loader fills it from JSON with default fallback behavior.

- [ ] **Step 5: Verify configuration compiles**
Run: `dotnet test .\tests\TwitchHeists.Tests\TwitchHeists.Tests.csproj --filter HeistResolverTests`
Expected: the project builds and the existing resolver tests still run, even though new adaptive-behavior tests are not written yet.

### Task 2: Add failing tests for adaptive winners and compact messages

**Files:**
- Modify: `tests\TwitchHeists.Tests\HeistResolverTests.cs`
- Modify: `tests\TwitchHeists.Tests\HeistActionsTests.cs`
- Modify: `tests\TwitchHeists.Tests\BridgeActionsTests.cs`

- [ ] **Step 1: Add failing resolver tests**
Extend `tests\TwitchHeists.Tests\HeistResolverTests.cs` with tests that prove:
1. one participant resolves as insufficient crew with the default `MinimumPlayers = 2`
2. all underfilled participants are refunded when `MinimumPlayers` is configured above the joined count
3. the winner count chosen for representative participant counts (for example 2, 6, 21, 51, 101, 150) lands inside the approved winner band
4. payout math remains proportional by stake

- [ ] **Step 2: Add failing action tests**
Extend `tests\TwitchHeists.Tests\HeistActionsTests.cs` with tests that prove:
1. a resolved heist below `MinimumPlayers` returns the insufficient-crew message
2. large successful heists produce a compact message that uses summary counts instead of long winner lists
3. provided JSON templates still override the new insufficient-crew and resolution wording

- [ ] **Step 3: Add failing bridge tests**
Extend `tests\TwitchHeists.Tests\BridgeActionsTests.cs` so the bridge path also proves:
1. the deployed `heist-messages.json` file controls the insufficient-crew message
2. `MinimumPlayers` can be loaded from configuration
3. the resolved heist message still stays short for larger crews

- [ ] **Step 4: Verify the tests fail for the right reason**
Run: `dotnet test .\tests\TwitchHeists.Tests\TwitchHeists.Tests.csproj --filter "HeistResolverTests|HeistActionsTests|BridgeActionsTests"`
Expected: the new adaptive-heist tests fail because the current implementation still uses a fixed winner cap and has no insufficient-crew outcome yet.

### Task 3: Implement adaptive winner selection and insufficient-crew refunds

**Files:**
- Modify: `src\TwitchHeists.Core\Services\HeistResolver.cs`
- Modify: `src\TwitchHeists.Data.Sqlite\Repositories\HeistRepository.cs`

- [ ] **Step 1: Resolve winner counts from the approved bands**
Modify `src\TwitchHeists.Core\Services\HeistResolver.cs` so successful heists:
1. locate the correct `HeistWinnerBand` from `HeistSettings`
2. roll a winner count inside that band
3. clamp that count to actual participant count
4. keep random winner selection and proportional payout logic intact

- [ ] **Step 2: Add the insufficient-crew outcome**
Modify `src\TwitchHeists.Core\Services\HeistResolver.cs` so any heist with fewer participants than `heistSettings.MinimumPlayers` returns the new insufficient-crew state, leaves `Winners` and `Losers` empty, and populates `RefundedParticipants` with every joined participant and their original stake as the refund amount.

- [ ] **Step 3: Refund underfilled crews transactionally**
Modify `src\TwitchHeists.Data.Sqlite\Repositories\HeistRepository.cs` so `ApplyResolution(...)` credits refunded participants back to balance before writing the final round state, while preserving the existing transaction boundary for successful and failed heists.

- [ ] **Step 4: Verify resolver and repository behavior**
Run: `dotnet test .\tests\TwitchHeists.Tests\TwitchHeists.Tests.csproj --filter "HeistResolverTests|HeistRepositoryTests"`
Expected: resolver and repository tests pass with the new winner-band and refund behavior.

### Task 4: Implement compact adaptive heist messaging

**Files:**
- Modify: `src\TwitchHeists.StreamerBot\Services\HeistMessageComposer.cs`
- Modify: `src\TwitchHeists.StreamerBot\Services\ResolveDueHeistsAction.cs`

- [ ] **Step 1: Add insufficient-crew message composition**
Modify `src\TwitchHeists.StreamerBot\Services\HeistMessageComposer.cs` so it can compose the new insufficient-crew message from JSON templates using existing placeholder replacement rules.

- [ ] **Step 2: Keep resolution output short**
Modify `src\TwitchHeists.StreamerBot\Services\HeistMessageComposer.cs` so successful and failed resolutions still include a headline and summary but cap named callouts using the setting from `HeistSettings`, rather than expanding with crew size.

- [ ] **Step 3: Surface the new outcome through the action**
Modify `src\TwitchHeists.StreamerBot\Services\ResolveDueHeistsAction.cs` so insufficient-crew outcomes return the new JSON-driven message and do not read like a normal success or failure result.

- [ ] **Step 4: Verify action and bridge message behavior**
Run: `dotnet test .\tests\TwitchHeists.Tests\TwitchHeists.Tests.csproj --filter "HeistActionsTests|BridgeActionsTests"`
Expected: action and bridge tests pass with compact resolution text and the new insufficient-crew messaging.

### Task 5: Document and verify the adaptive heist rules

**Files:**
- Modify: `README.md`
- Modify: `.github\docs\streamerbot-install-guide.md`

- [ ] **Step 1: Document the adaptive winner rules**
Update `README.md` and `.github\docs\streamerbot-install-guide.md` so they explain:
1. the approved winner-band behavior
2. that crews below `MinimumPlayers` are refunded as “not enough crew”
3. that `heist-messages.json` now includes an `insufficientCrewMessages` group
4. that result messages intentionally stay short on large crews

- [ ] **Step 2: Run focused heist verification**
Run: `dotnet test .\tests\TwitchHeists.Tests\TwitchHeists.Tests.csproj --filter "HeistResolverTests|HeistActionsTests|BridgeActionsTests"`
Expected: all focused heist tests pass.

- [ ] **Step 3: Run full solution verification**
Run: `dotnet test .\TwitchHeists.sln`
Expected: the full solution test suite passes after the adaptive-heist changes.

- [ ] **Step 4: Record the outcome**
Note in the handoff summary that the fixed five-winner rule was replaced by the approved tiered winner bands, `MinimumPlayers` now controls insufficient-crew refunds, and resolved messages are now intentionally compact for large rounds.
