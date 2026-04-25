---
name: finish
description: Wrap up an implementation — run final QA, clean up the code, and prepare a clear handoff for review or release.
triggers: done, finish, wrap up, ready for review, ship, complete feature, final checks, handoff
chains-to: review
sub-skills:
  - prompts\finishing-implementation.md
---

# Finish

## Project override handoff

Before following this core skill:
1. Check `.mimir/skills/finish/SKILL.md`. If it exists, load it and follow it instead of this file.
2. Otherwise check `.mimir/skills/finish/context.md`. If it exists, append it to this core skill before acting.
3. Only continue with this file when neither project override exists.

The last phase before work is handed off. Run the final quality checks, clean up anything left over from development, and prepare a review-ready summary of what changed.

## When to use

- You believe the implementation is complete
- All planned tasks are done and tests pass
- You are ready to hand the work to a reviewer or treat it as complete

## Entry checklist

Before running the finish checklist:
1. All planned tasks from the implementation plan are checked off
2. You have not skipped any `mimirai:qa` gates during the build
3. There are no known blockers left unresolved for this work item

## Process

Load `prompts\finishing-implementation.md` for the full finish checklist.

## On completion

- If review is needed: hand off to `mimirai:review`
- If review is not needed: record the completion notes and stop
