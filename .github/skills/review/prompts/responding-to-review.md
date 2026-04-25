---
name: responding-to-review
description: Triage reviewer feedback, address comments systematically, and close the review loop cleanly.
parent: review
---

# Responding to a Review

Reviewer feedback is a gift. Treat every comment as an opportunity to improve the work, even when you disagree. Close every comment — never leave threads dangling.

## Step 1: Read all comments before changing any code

Read the entire review before making a single change. This prevents:
- Fixing the same underlying issue multiple times in different places
- Addressing symptoms when a comment is pointing at a deeper problem
- Missing a comment that changes the approach entirely

## Step 2: Triage each comment

Categorise every comment as one of:

| Category | Meaning | Action |
|---|---|---|
| **Must fix** | Correctness, security, data loss, broken contract | Fix before re-requesting review |
| **Should fix** | Code quality, consistency, clarity | Fix unless there is a clear reason not to |
| **Suggestion** | Style preference or alternative approach | Use your judgement; acknowledge the comment either way |
| **Question** | Reviewer needs clarification | Answer in the thread, then decide if code should change |
| **Nitpick** | Minor style, formatting, naming | Fix if trivial; decline gracefully if not worth the churn |

## Step 3: Address each comment

For each comment, in order from most critical to least:

1. **Understand what the reviewer is pointing at** — if unclear, ask a clarifying question in the thread before changing code
2. **Make the change** (if fixing) — keep it scoped to what the comment asked for; do not refactor nearby code opportunistically
3. **Reply in the thread** — always reply, even if you made no change. Examples:
   - Fixed: "Done — moved validation to the service layer."
   - Declined (with reason): "Kept this as-is because X. Happy to revisit if you feel strongly."
   - Answered: "This runs after the DB write because Y — if the email fails, we retry via the job queue."
4. **Resolve the thread** — only mark a thread resolved if you own the review thread and the comment has been addressed. Do not resolve threads opened by others unless they ask you to.

## Step 4: Push back correctly

You may disagree with feedback. That is fine. The right way to push back:

- Acknowledge the concern first: "I see why this looks odd — "
- Explain the constraint or reasoning: "— it's this way because the SDK requires X."
- Offer an alternative if one exists: "We could do Y instead — happy to try that if you prefer."
- Never silently ignore a comment, and never just say "no" without a reason.

If a disagreement cannot be resolved between the two of you, escalate to a third person rather than leaving the thread open indefinitely.

## Step 5: Re-request review

Once all must-fix and should-fix items are addressed:

1. Leave a summary comment in the review system: "Addressed all comments. Main changes: X, Y, Z. One item I left as-is — see thread on line 42."
2. Re-request review from the original reviewer(s)
3. Keep the updated changes easy to follow — do not rewrite the history of the review discussion mid-stream

## What not to do

- Do not mark comments resolved without actually addressing them
- Do not make large unrelated changes in a review iteration
- Do not declare the review complete without approval from required reviewers
- Do not ghost a reviewer's question — always reply
