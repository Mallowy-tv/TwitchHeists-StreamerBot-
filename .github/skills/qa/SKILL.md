---
name: qa
description: Verify work is complete and correct before claiming done. Evidence required — assertions are not enough.
triggers: done, finished, complete, verify, check, QA, review, before merging, is this ready
chains-to: track
sub-skills:
  - prompts/verification-before-complete.md
---

# QA

## Project override handoff

Before following this core skill:
1. Check `.mimir/skills/qa/SKILL.md`. If it exists, load it and follow it instead of this file.
2. Otherwise check `.mimir/skills/qa/context.md`. If it exists, append it to this core skill before acting.
3. Only continue with this file when neither project override exists.

Verify before claiming done. "I think it works" is not evidence. Running the command and seeing it work is evidence.

## When to use

- Before saying any task is complete
- Before handing off a fix
- Before sending work for review
- When asked "is this ready?" or "can we ship this?"

## The rule

You must run verification commands and see passing output before claiming anything is done. No exceptions.

## Process

Load `prompts/verification-before-complete.md` for the full checklist.

## On completion

- All checks pass → state what was verified with the actual output
- Issues found → write to `todo.md` via `mimirai:track` if non-blocking, or fix immediately if blocking
