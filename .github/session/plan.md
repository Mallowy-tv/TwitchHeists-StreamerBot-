# Mixed Load Baseline And Optimisation Plan

**Goal:** Add a repeatable 1,000-viewer mixed-load benchmark inside the existing test project, record the current baseline, then optimise the proven hotspots without changing behaviour.
**Architecture:** The first phase adds a test-owned harness that exercises the real StreamerBot action and SQLite paths for stream start, community refresh, chat bursts, and heist traffic. The second phase keeps the same scenario and correctness assertions while reducing avoidable database round-trips in the refresh-time streak path and revisiting SQLite connection settings that currently add overhead.
**Tech Stack:** .NET 9 xUnit tests, StreamerBot action layer, SQLite repositories, Stopwatch-based timing output.

## File map

| Action | Path | Responsibility |
|---|---|---|
| Create | `tests\TwitchHeists.Tests\MixedLoadPerformanceTests.cs` | Mixed 1,000-viewer baseline harness and timing output |
| Create | `tests\TwitchHeists.Tests\Performance\MixedLoadScenarioRunner.cs` | Shared scenario setup, seeding, execution, and metric collection |
| Create | `tests\TwitchHeists.Tests\Performance\MixedLoadScenarioResult.cs` | Strongly typed timing and count output for the load harness |
| Modify | `src\TwitchHeists.StreamerBot\Services\RefreshCommunityViewersAction.cs` | Switch refresh-time streak handling from per-viewer calls to a batch-oriented flow |
| Modify | `src\TwitchHeists.StreamerBot\Services\WatchStreakService.cs` | Add a batch-friendly API that preserves existing streak rules |
| Modify | `src\TwitchHeists.Data.Sqlite\Repositories\WatchStreakRepository.cs` | Support efficient stream-state and streak lookups/updates for bulk refresh qualification |
| Modify | `src\TwitchHeists.StreamerBot.Bridge\Services\BridgeRuntimeFactory.cs` | Revisit SQLite connection-string settings after baseline measurement |
| Modify | `tests\TwitchHeists.Tests\RefreshCommunityViewersActionTests.cs` | Regression coverage proving optimised refresh logic preserves streak and reward behaviour |
| Modify | `README.md` | Document the benchmark command and what it measures |

### Task 1: Add the mixed-load baseline harness

**Files:**
- Create: `tests\TwitchHeists.Tests\MixedLoadPerformanceTests.cs`
- Create: `tests\TwitchHeists.Tests\Performance\MixedLoadScenarioRunner.cs`
- Create: `tests\TwitchHeists.Tests\Performance\MixedLoadScenarioResult.cs`

- [ ] **Step 1: Create the scenario result type**
Write `tests\TwitchHeists.Tests\Performance\MixedLoadScenarioResult.cs` with a small immutable model that stores viewer counts, chat event counts, heist join counts, and elapsed timings for refresh, chat, heist, and total runtime.

- [ ] **Step 2: Create the scenario runner**
Write `tests\TwitchHeists.Tests\Performance\MixedLoadScenarioRunner.cs` to:
1. create a temporary SQLite database
2. seed 1,000 viewers with Twitch IDs and balances
3. start a stream
4. run at least one full community refresh over all viewers
5. execute a controlled burst of chat-presence calls
6. start a heist and process a large batch of joins
7. resolve the heist
8. capture Stopwatch timings per phase and total runtime
9. return a `MixedLoadScenarioResult`

- [ ] **Step 3: Add the baseline performance test**
Write `tests\TwitchHeists.Tests\MixedLoadPerformanceTests.cs` with a single named test that runs the scenario, writes the recorded timings to the xUnit output, and asserts correctness invariants such as:
1. refresh awarded balances
2. chat presence persisted
3. heist participants were recorded
4. no phase failed under the 1,000-viewer scenario

- [ ] **Step 4: Verify the new harness**
Run: `dotnet test .\tests\TwitchHeists.Tests\TwitchHeists.Tests.csproj --filter MixedLoadPerformanceTests`
Expected: the mixed-load test passes and prints baseline timing metrics for refresh, chat, heist, and total runtime.

### Task 2: Lock in regression coverage around refresh behaviour

**Files:**
- Modify: `tests\TwitchHeists.Tests\RefreshCommunityViewersActionTests.cs`

- [ ] **Step 1: Add refresh-focused regression assertions**
Extend `tests\TwitchHeists.Tests\RefreshCommunityViewersActionTests.cs` so it explicitly covers:
1. one streak award per stream during refresh
2. refresh rewards still use subscriber multipliers
3. the optimised refresh path does not break Twitch ID preservation

- [ ] **Step 2: Verify the focused refresh suite**
Run: `dotnet test .\tests\TwitchHeists.Tests\TwitchHeists.Tests.csproj --filter RefreshCommunityViewersActionTests`
Expected: all refresh tests pass before optimisation work begins.

### Task 3: Optimise refresh-time streak qualification

**Files:**
- Modify: `src\TwitchHeists.StreamerBot\Services\RefreshCommunityViewersAction.cs`
- Modify: `src\TwitchHeists.StreamerBot\Services\WatchStreakService.cs`
- Modify: `src\TwitchHeists.Data.Sqlite\Repositories\WatchStreakRepository.cs`

- [ ] **Step 1: Refactor streak evaluation for batch refresh use**
Add a batch-oriented entry point in `WatchStreakService` that accepts the refresh timestamp and the confirmed viewer set, reuses one stream-state lookup, and avoids reopening repositories per viewer where possible.

- [ ] **Step 2: Add repository support for bulk streak reads/writes**
Update `WatchStreakRepository` so refresh-time qualification can read the relevant streak state and record awards with fewer SQLite round-trips while preserving the existing streak rules exactly.

- [ ] **Step 3: Switch refresh action to the batch path**
Modify `RefreshCommunityViewersAction` to use the new batch streak flow after the reward cycle is applied, keeping the public response shape unchanged.

- [ ] **Step 4: Verify behaviour after optimisation**
Run: `dotnet test .\tests\TwitchHeists.Tests\TwitchHeists.Tests.csproj --filter "RefreshCommunityViewersActionTests|WatchStreakActionsTests"`
Expected: refresh and streak tests still pass with no behaviour changes.

### Task 4: Optimise SQLite connection overhead

**Files:**
- Modify: `src\TwitchHeists.StreamerBot.Bridge\Services\BridgeRuntimeFactory.cs`

- [ ] **Step 1: Revisit the bridge connection string**
Update `BridgeRuntimeFactory.BuildConnectionString(...)` to remove avoidable overhead from the current SQLite settings while keeping the existing install-directory resolution and database location behaviour intact.

- [ ] **Step 2: Re-run the mixed-load harness**
Run: `dotnet test .\tests\TwitchHeists.Tests\TwitchHeists.Tests.csproj --filter MixedLoadPerformanceTests`
Expected: the same mixed-load test passes again and prints a second set of timings that can be compared with the original baseline.

### Task 5: Document and verify the before/after result

**Files:**
- Modify: `README.md`

- [ ] **Step 1: Document the benchmark command**
Add a short README note showing how to run the mixed-load benchmark test and what phases it measures.

- [ ] **Step 2: Run full verification**
Run: `dotnet test .\TwitchHeists.sln`
Expected: the full solution test suite passes after the load harness and optimisation changes.

- [ ] **Step 3: Record the outcome**
Capture the before/after timing deltas from the mixed-load test output in the handoff summary so the optimisation is backed by measured evidence rather than guesswork.
