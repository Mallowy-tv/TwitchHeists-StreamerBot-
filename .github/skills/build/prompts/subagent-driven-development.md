---
name: subagent-driven-development
description: Execute a plan by dispatching a fresh subagent per task with two-stage review after each
parent: build
---

# Subagent-Driven Development

Dispatch a fresh subagent per task. Two-stage review after each: spec compliance first, then code quality. Preserves your context for coordination.

## When to use

- Plans with 3+ independent tasks
- Tasks that don't share mutable state
- You want review checkpoints without manual intervention

## Before starting

1. Read the full plan once — extract all tasks with their complete text
2. Create a todo list with all tasks (pending)
3. Note the current task boundary and expected files

## Per-task loop

```
Mark task in_progress
→ Dispatch implementer subagent (fresh, no prior context)
→ Implementer: implement, test, self-review, report status
→ If NEEDS_CONTEXT: provide context, re-dispatch
→ If BLOCKED: assess — provide more context, use stronger model, or break task down
→ If DONE or DONE_WITH_CONCERNS: dispatch spec compliance reviewer
→ Spec reviewer reads actual code, verifies against requirements
→ If spec issues: implementer fixes → spec reviewer re-reviews
→ When spec passes: dispatch code quality reviewer
→ If quality issues: implementer fixes → quality reviewer re-reviews
→ When quality passes: mark task completed, move to next
```

## Implementer subagent prompt

Provide the subagent with:
- The full task text from the plan (paste it — do not make them read the file)
- Scene-setting context: what the project is, what this task builds, what came before
- The working directory
- The expected files and task boundary

Instruct them to report: `DONE | DONE_WITH_CONCERNS | NEEDS_CONTEXT | BLOCKED`

## Spec compliance reviewer prompt

Provide:
- Full task requirements (what was asked for)
- What the implementer claims to have built
- Instruction to read the actual code, not trust the report

Expected output: `✅ Spec compliant` or `❌ Issues: [list with file:line]`

## Code quality reviewer prompt

Provide:
- What was implemented
- The expected files and the implemented files
- Focus areas: file responsibilities, test coverage, naming, no over-engineering

Expected output: Strengths, Issues (Critical/Important/Minor), Assessment

## Model selection

- 1–2 file tasks with clear specs → fast model
- Multi-file tasks with integration concerns → standard model
- Architecture, judgment, or debugging tasks → most capable model

## Rules

- Never dispatch multiple implementer subagents in parallel (conflicts)
- Never skip spec review — it prevents over/under-building
- Never skip quality review — both reviews are required
- Never start quality review before spec passes
- If implementer reports concerns, read them before proceeding to review
- Fresh subagent = no context from this session — provide everything they need

## On completion

All tasks reviewed and passing → invoke `mimirai:qa` → `verification-before-complete`.
