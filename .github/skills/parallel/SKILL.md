---
name: parallel
description: Decompose a plan into independent tasks and dispatch multiple agents to work on them simultaneously, then integrate the results.
triggers: parallel, concurrent, multiple agents, independent tasks, dispatch, fan out, split work
chains-to: build, review
sub-skills:
  - prompts/dispatching-parallel-agents.md
---

# Parallel

## Project override handoff

Before following this core skill:
1. Check `.mimir/skills/parallel/SKILL.md`. If it exists, load it and follow it instead of this file.
2. Otherwise check `.mimir/skills/parallel/context.md`. If it exists, append it to this core skill before acting.
3. Only continue with this file when neither project override exists.

Speed up large plans by running independent tasks concurrently across multiple agents. Only use this when tasks have no shared state and cannot interfere with each other.

## When to use

- An implementation plan has 3 or more tasks that do not depend on each other
- Build time is the bottleneck and tasks can be cleanly separated
- The plan explicitly calls for parallel execution

## Entry checklist

Before dispatching:
1. Confirm each task is truly independent — no shared files, no ordering dependencies
2. Each task must have a clear, self-contained scope with a defined done state
3. The current working tree must not contain overlapping in-progress edits for the same files

## Process

Load `prompts/dispatching-parallel-agents.md` for the full dispatch and integration process.

## On completion

- Integrate the completed agent changes back into the main working tree
- Resolve any conflicting edits before continuing
- Invoke `mimirai:review` if a formal review handoff is needed, or `mimirai:qa` to verify before continuing
- Log discovered debt to `todo.md` via `mimirai:track`
