---
name: dispatching-parallel-agents
description: Decompose independent tasks, dispatch one agent per task with explicit file boundaries, and integrate results.
parent: parallel
---

# Dispatching Parallel Agents

Run multiple agents simultaneously on independent tasks, then integrate the results. This is only safe when tasks are truly independent — no shared files, no ordering dependencies, no shared database state.

## Step 1: Identify independent tasks

Review the implementation plan. A task is safe to parallelise if:
- It writes to different files than all other parallel tasks
- It does not depend on the output of another parallel task
- It has a single, unambiguous done state

**Do not parallelise:**
- Tasks that modify the same file (even different functions)
- Tasks with a sequential dependency (`task B needs the interface defined in task A`)
- Database schema migrations (always sequential)
- Tasks that share in-memory state or a running server

If tasks do not meet the independence criteria, execute them sequentially with `mimirai:build`.

## Step 2: Assign a file boundary per task

For each independent task, define an explicit file boundary and ownership list. Every file that may be edited must belong to exactly one task.

```text
task-1 -> src/auth/*, tests/auth/*
task-2 -> src/billing/*, tests/billing/*
task-3 -> docs/api/*, openapi.yaml
```

If two tasks need the same file, they are not independent and must be merged into one task or run sequentially.

## Step 3: Write a task brief per agent

For each task, write a brief that includes everything the agent needs without requiring it to read the full plan:

```
## Task: <task name>

**Goal:** <one sentence describing what this task builds or changes>

**Files to create/modify:**
- `path/to/file.ts` — <what this file does>
- `path/to/other.ts` — <what this file does>

**Inputs available:**
- `path/to/interface.ts` already defines <X> — use it, do not modify it
- The pattern used in `path/to/existing.ts` is the model to follow

**Done when:**
- [ ] <concrete, verifiable criterion>
- [ ] <concrete, verifiable criterion>
- [ ] All tests pass

**Do not touch:**
- `path/to/shared-file.ts` — another agent is modifying this in a different task
```

## Step 4: Dispatch agents

Dispatch one agent per task. Provide each agent with:
1. The task brief (written in Step 3)
2. The exact files or directories it owns
3. The instruction to invoke `mimirai:build` to execute the task

Agents run concurrently. Do not coordinate between agents mid-flight — the whole point is independence.

## Step 5: Monitor and collect results

When each agent completes:
1. Review the changes the agent made
2. Confirm the done criteria from the brief are met
3. Run the task's relevant tests from the main working directory
4. If a task failed or is incomplete, fix it before merging

## Step 6: Integrate results

Once all agents have completed and their results are verified, integrate each task's changes back into the main working tree. Review the final combined set of changed files before moving on.

Conflicting edits should be rare if tasks were truly independent — if there are many conflicts, the task decomposition was incorrect.

## Step 7: Clean up temporary task state

After integration, remove any temporary task notes or scratch artifacts created for dispatch. If such files are needed during execution, keep them under `.github/session/parallel/`.

## Step 8: Final verification

Run the full test suite from the main working directory after all integration is complete. Do not skip this step — combining independent changes can still introduce failures.
