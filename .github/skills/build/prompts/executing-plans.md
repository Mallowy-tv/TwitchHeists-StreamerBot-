---
name: executing-plans
description: Execute an implementation plan step-by-step in the current session with checkpoint reviews
parent: build
---

# Executing Plans

Execute a plan in the current session, task by task, with review checkpoints.

## When to use

- Plans with fewer than 5 tasks, or tasks that are tightly coupled (prefer inline even if 3–4 tasks)
- Tasks that are tightly coupled and must be done in sequence
- User prefers to stay in one session without subagent dispatch

## Before starting

1. Read the full plan — do not start until you understand all tasks and their order
2. Create a task list in your working memory: pending → in_progress → completed
3. Confirm the working directory and task scope

## Per-task process

For each task in order:

1. **Mark in progress** — state which task you are starting
2. **Read the task fully** — understand all steps before beginning any of them
3. **Execute each step** — follow the plan exactly. If a step says to run a command, run it. If it shows code, write that code.
4. **Verify** — run the verification step. Do not skip. If it fails, fix before moving on.
5. **Record progress** — note task completion and any follow-up items
6. **Self-review** — did you implement everything in this task? Any gaps?
7. **Report** — briefly state what was done and whether anything unexpected came up

## Checkpoint reviews

After every 2–3 tasks, pause and:
- Confirm the completed tasks still match the plan
- Run any existing test suite to check for regressions
- Ask the user if they want to continue or adjust course

## Rules

- Never skip a step because it "seems obvious"
- Never implement something not in the plan — if you think something is missing, note it and ask
- If a step fails, do not silently move on — stop, diagnose, fix, then continue
- If a task turns out to be much larger than the plan suggests, stop and report before continuing

## On completion

All tasks complete → invoke `mimirai:qa` → `verification-before-complete`.
