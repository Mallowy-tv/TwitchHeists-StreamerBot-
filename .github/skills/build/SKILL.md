---
name: build
description: Execute a written implementation plan. Use when a plan exists and it's time to implement.
triggers: execute, implement, build, code, write the code, start implementing, do it
chains-to: test, qa
sub-skills:
  - prompts/executing-plans.md
  - prompts/subagent-driven-development.md
---

# Build

## Project override handoff

Before following this core skill:
1. Check `.mimir/skills/build/SKILL.md`. If it exists, load it and follow it instead of this file.
2. Otherwise check `.mimir/skills/build/context.md`. If it exists, append it to this core skill before acting.
3. Only continue with this file when neither project override exists.

Execute a written implementation plan. Never build without a plan.

## When to use

- A written implementation plan exists and is approved
- The user says "do it", "implement this", "start building", or "execute the plan"
- Following up from `mimirai:plan`

## Entry checklist

1. Confirm a written plan exists (`.github/session/plan.md` or provided inline)
2. Read the plan fully before starting — understand all tasks and dependencies
3. Check `.mimir/core/constraints.md` — any constraints that affect how code is written?
4. Check `.mimir/core/design-system.md` — any component rules for UI work?

## Process

Choose execution approach based on task complexity and user preference:

**Subagent-driven (recommended for plans with 3+ tasks):**
Load `prompts/subagent-driven-development.md`
- Fresh subagent per task
- Two-stage review after each task (spec compliance, then code quality)
- Best for parallel-safe, independent tasks

**Inline execution:**
Load `prompts/executing-plans.md`
- Execute tasks in sequence in this session
- Checkpoint reviews between tasks
- Best for tightly coupled tasks or quick plans

## On completion

- Invoke `mimirai:qa` → `verification-before-complete` before claiming done
- If bugs or debt were found during build: write to `todo.md` via `mimirai:track`
- Invoke `mimirai:test` if tests need to be written or run
