---
name: todo-tracker
description: Full rules for writing entries to todo.md — format, priority, sections, and when to write
parent: track
---

# todo.md Tracker

This sub-skill extends `.github/core/todo-tracker.md` with usage guidance for the track phase.

## The iron rule

When you find a bug, fault, or tech debt while working on something else:
1. Write it to `todo.md` at the project root
2. Stay on the current task
3. Do NOT stop. Do NOT fix it. Do NOT ask about it.

## File location

`todo.md` lives at the project root. Create it if it doesn't exist.

## Format

```markdown
# todo.md

## Bugs
- [ ] [HIGH] `file:line` — description <!-- found: YYYY-MM-DD, task: what you were doing -->

## Tech Debt
- [ ] [MED] `file` — description <!-- found: YYYY-MM-DD -->

## QA / Verification Needed
- [ ] [MED] `path/` — what needs testing <!-- found: YYYY-MM-DD -->

## Ideas / Future
- [ ] [LOW] description <!-- found: YYYY-MM-DD -->
```

## Priority rules

| Priority | Use for |
|---|---|
| `[HIGH]` | Security vulnerabilities, data loss risks, broken logic in production paths |
| `[MED]` | Missing tests for critical paths, tech debt affecting current work |
| `[LOW]` | Duplication, improvements, ideas, minor debt |

When in doubt: `[MED]`. Downgrade to `[LOW]` if it's clearly cosmetic. Upgrade to `[HIGH]` only if it could cause real harm.

## Entry format

```
- [ ] [PRIORITY] `file:line` — one-line description <!-- found: YYYY-MM-DD, task: context -->
```

- File reference is required when known — use `file:line` for specific locations, `path/` for directories
- The hidden comment records when it was found and what triggered it
- One line per item — never multi-line

## Marking as done

When an item is resolved, change `- [ ]` to `- [x]`:
```
- [x] [HIGH] `auth/session.ts:47` — token not invalidated on logout <!-- found: 2026-04-14, fixed: 2026-04-15 -->
```

## Session start reminder

If `todo.md` has open `[HIGH]` items at session start, MímirAI will surface them as an FYI. You are not blocked — they are informational.
