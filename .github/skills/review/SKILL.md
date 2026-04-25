---
name: review
description: Code review workflows — prepare a clean review handoff, or respond to reviewer feedback with rigour and clarity.
triggers: review, feedback, approve, changes requested, LGTM, address comments, review handoff
chains-to: finish, build
sub-skills:
  - prompts/requesting-review.md
  - prompts/responding-to-review.md
---

# Review

## Project override handoff

Before following this core skill:
1. Check `.mimir/skills/review/SKILL.md`. If it exists, load it and follow it instead of this file.
2. Otherwise check `.mimir/skills/review/context.md`. If it exists, append it to this core skill before acting.
3. Only continue with this file when neither project override exists.

Manage both sides of the code review loop: preparing work for review and responding to feedback once it arrives.

## When to use

- You have finished implementing a feature and need to prepare it for review
- A reviewer has left comments and you need to triage and address them
- You want to self-review your diff before requesting others

## Entry checklist

Before invoking any sub-skill:
1. Confirm the work is ready for review — no unresolved blockers, no failing tests
2. Identify which direction you are working: *requesting* (outbound) or *responding* (inbound)

## Process

**Requesting a review:** load `prompts/requesting-review.md`

**Responding to review feedback:** load `prompts/responding-to-review.md`

## On completion

- If all reviewer comments are resolved: invoke `mimirai:finish`
- If further build work is needed: invoke `mimirai:build`
- Log any discovered debt to `todo.md` via `mimirai:track`
