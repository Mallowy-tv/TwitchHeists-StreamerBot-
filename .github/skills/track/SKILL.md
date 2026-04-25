---
name: track
description: Log discovered bugs, tech debt, and faults to todo.md without interrupting the current task.
triggers: todo, tech debt, bug found, noticed, should fix, debt, log this, remember this
sub-skills:
  - prompts/todo-tracker.md
---

# Track

## Project override handoff

Before following this core skill:
1. Check `.mimir/skills/track/SKILL.md`. If it exists, load it and follow it instead of this file.
2. Otherwise check `.mimir/skills/track/context.md`. If it exists, append it to this core skill before acting.
3. Only continue with this file when neither project override exists.

Log it and keep going. Never stop what you're doing to fix something you just noticed.

## When to use

- You spot a bug while working on something else
- You notice tech debt that isn't blocking current work
- You find a missing test for code you're reading
- You have an idea for a future improvement

## The rule

Write it to `todo.md`. Stay on the current task. Do not fix it. Do not ask about it.

## Process

Load `prompts/todo-tracker.md` for the format and priority rules.

**Quick format:**
```
- [ ] [PRIORITY] `file:line` — description <!-- found: YYYY-MM-DD, task: what you were doing -->
```

Add to the appropriate section in `todo.md` (`## Bugs`, `## Tech Debt`, `## QA / Verification Needed`, or `## Ideas / Future`).

## On completion

Return to the task that was interrupted. Do not change course.
