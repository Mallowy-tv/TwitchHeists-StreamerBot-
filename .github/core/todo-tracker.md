---
name: todo-tracker
description: Rules for writing to todo.md when bugs, debt, or faults are found mid-task
type: core-rule
---

# todo.md Tracker Rules

## The Iron Rule

When you find a bug, fault, or tech debt while working on something else:
1. Write it to `todo.md` at the project root
2. Stay on the current task
3. Do NOT stop. Do NOT fix it. Do NOT ask about it.

## todo.md Format

The file lives at `<project-root>/todo.md`. Create it if it does not exist.

```markdown
# todo.md

## Bugs
- [ ] [HIGH] `file:line` — description <!-- found: YYYY-MM-DD, task: what you were doing -->

## Tech Debt
- [ ] [MED] `file` — description <!-- found: YYYY-MM-DD -->

## QA / Verification Needed
- [ ] [MED] `path/` — what needs testing

## Ideas / Future
- [ ] [LOW] description
```

## Priority Rules

| Level | Use for |
|---|---|
| `[HIGH]` | Bugs, security issues, data loss risks — anything that could break production |
| `[MED]` | Missing tests for important paths, notable tech debt affecting current work |
| `[LOW]` | Improvements, ideas, minor duplication, nice-to-haves |

## Entry Format

```
- [ ] [PRIORITY] `file:line` — one-line description <!-- found: YYYY-MM-DD, task: context -->
```

- Always include a file reference when known (`file:line` or `path/`)
- Always include the hidden comment with date and triggering task
- One line per item — no multi-line entries

## What triggers a write

| Situation | Section | Priority |
|---|---|---|
| Security vulnerability found | Bugs | HIGH |
| Data loss or corruption risk | Bugs | HIGH |
| Broken logic in existing code | Bugs | HIGH |
| Missing tests for critical path | QA / Verification Needed | MED |
| Duplicated code that should be abstracted | Tech Debt | MED or LOW |
| Outdated pattern spotted | Tech Debt | LOW |
| Good idea for future improvement | Ideas / Future | LOW |

## Session Start

If `todo.md` has open `[HIGH]` items at session start, surface a one-line summary as an FYI, then continue. Do not wait for a response.
