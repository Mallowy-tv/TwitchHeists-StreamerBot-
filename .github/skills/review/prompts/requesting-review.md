---
name: requesting-review
description: Prepare code for review — self-review the changed files, write a clear review summary, and involve the right reviewers.
parent: review
---

# Requesting a Review

Before asking someone else to review your work, make it as easy to review as possible. This means a clean set of changed files, a clear description, and a self-review pass.

## Step 1: Self-review the changed files

Read every changed line as if you were the reviewer seeing it for the first time. Check for:

- Dead code, commented-out blocks, or debug logging left in
- Inconsistent naming compared to the surrounding codebase
- Missing error handling for new failure paths
- Functions that grew too large during the implementation
- Anything you would flag if reviewing someone else's code

Fix anything you find before requesting review. Write newly discovered debt to `todo.md` rather than letting it block the review.

## Step 2: Verify the work is ready

Confirm all of the following before handing the work to a reviewer:

- [ ] All tests pass (`npm test`, `pytest`, or equivalent)
- [ ] Linter passes with no new warnings
- [ ] Any new environment variables or config are documented
- [ ] Migration scripts (if any) are included and tested

## Step 3: Write the review description

A good review description answers three questions: *what* changed, *why* it changed, and *how* to verify it. Use this template:

```
## Summary

<1–3 sentences describing the change. What does this work do? Why is it needed?>

## Changes

- <Bullet list of the main changes. One item per logical change.>
- <Include component names, function names, or file paths where helpful.>

## How to test

<Step-by-step instructions for a reviewer to verify the change works.>
1. Open the changed files in the workspace
2. Run <command>
3. Do <action> — expect <result>

## Notes for reviewer

<Optional: highlight areas that need close attention, explain non-obvious decisions, call out known limitations, or list follow-up tickets.>
```

Write the description for the reviewer, not for yourself. Assume they have project context but not task context.

## Step 4: Choose reviewers

- Assign at least one reviewer who owns or deeply knows the affected area
- If the change is cross-cutting, assign one reviewer per affected area
- Do not assign more than 3 reviewers — diffused ownership means less thorough review

## Step 5: Link related work items

- Link the relevant issue, ticket, or `todo.md` item in the description if the project uses them
- Add any categorisation notes the project expects

## What not to do

- Do not ask for review before the work is actually ready
- Do not include unrelated changes in the same handoff
- Do not send a review handoff with no description
