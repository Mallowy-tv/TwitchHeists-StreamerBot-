---
name: writing-plans
description: Full process for writing bite-sized implementation plans from approved designs
parent: plan
---

# Writing Plans

Write implementation plans assuming the person executing has zero project context. Document everything: which files to touch, what to write, how to verify it works.

## The standard

Every task must be executable by someone who has never seen this codebase. That means:
- Exact file paths (never "the config file" — write `src/config/database.ts`)
- Complete content in every step — if a step creates a file, show the full file
- Exact commands with expected output
- No steps that say "implement X" without showing what X looks like

**These are plan failures — never write them:**
- "TBD", "TODO", "fill in later"
- "Add appropriate error handling"
- "Write tests for the above" (without the actual test code)
- "Similar to Task N" (copy the content — reader may be doing tasks out of order)
- Steps that describe what to do without showing how

## File map first

Before writing any tasks, list every file being created or modified:

```markdown
| Action | Path | Responsibility |
|---|---|---|
| Create | `src/auth/login.ts` | Login handler, token generation |
| Modify | `src/routes/index.ts` | Register /login route |
| Create | `tests/auth/login.test.ts` | Login flow tests |
```

This is where scope gets locked in. Each file should have one clear responsibility.

## Task format

```markdown
### Task N: Component Name

**Files:**
- Create: `exact/path/to/file.ts`
- Modify: `exact/path/to/existing.ts`

- [ ] **Step 1: [Action]**
[Content — code block, file content, or command]

- [ ] **Step 2: Verify**
Run: `command`
Expected: output

- [ ] **Step 3: Record progress**
Note what changed and how it was verified
```

## Granularity

Each step is one action (2–5 minutes):
- Write a specific file → one step
- Run a specific command → one step
- Verify specific output → one step
- Record progress → one step

Tasks that touch 3+ files should be split into smaller tasks.

## TDD order

For any feature with tests:
1. Write the failing test
2. Run it — confirm it fails
3. Write minimal implementation
4. Run it — confirm it passes
5. Record progress

## Plan header

Every plan starts with:
```markdown
# Feature Name Implementation Plan

**Goal:** One sentence.
**Architecture:** 2–3 sentences on approach.
**Tech Stack:** Key technologies.
```

## Saving the plan

Save to `.github/session/plan.md` unless the user asked for a different location.

## Self-review before presenting

1. Spec coverage — can you point to a task for every requirement?
2. Placeholder scan — any TBDs, vague steps, or missing code?
3. Consistency — do function names, types, and paths match across all tasks?

Fix issues before showing the plan to the user.

## After the plan

Present the plan and ask the user to review it. Once approved, offer:
1. **Subagent-driven** (recommended) — fresh subagent per task, reviewed between tasks
2. **Inline** — execute in this session with checkpoints

Load `mimirai:build` → appropriate sub-skill based on their choice.
