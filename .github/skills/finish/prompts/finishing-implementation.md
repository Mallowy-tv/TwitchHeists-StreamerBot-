---
name: finishing-implementation
description: Final checklist before handoff — QA, cleanup, and review-ready notes.
parent: finish
---

# Finishing an Implementation

Work through this checklist top to bottom. Do not hand off the work until every relevant item is checked.

## 1. Final QA gate

Run the full test suite from scratch:

```bash
# Clear any caches, then run all tests
npm run test -- --clearCache 2>/dev/null; npm test
```

(Adjust the command for your stack: `pytest`, `go test ./...`, `cargo test`, etc.)

All tests must pass. If any are failing:
- Fix them before continuing — do not hand off work with failing tests
- If a test was already failing before your changes, document it in your handoff notes under a "Known failures" section and file a `todo.md` entry

## 2. Linter and type-checker

```bash
npm run lint
npm run typecheck   # if applicable
```

Zero new warnings or errors. Pre-existing warnings that your changes did not introduce may be noted but do not block.

## 3. Change review

Review every file you changed. Check for:
- [ ] Debug logging removed (`console.log`, `print`, `fmt.Println`, etc.)
- [ ] Commented-out code removed
- [ ] TODO comments that were meant to be addressed before shipping are addressed
- [ ] No accidental whitespace-only changes in unrelated files
- [ ] Secrets, API keys, or credentials are not present in the changed files
- [ ] File permissions not unintentionally changed

## 4. Documentation check

For every public API, exported function, or new component introduced in this work:
- [ ] It has a doc comment or is covered in existing documentation
- [ ] If the change affects a README, API reference, or architectural decision: update the docs now

If documentation is substantial: invoke `mimirai:docs` to handle it.

## 5. Changelog / release notes (if applicable)

If the project maintains release notes:
- Add or update the appropriate entry in the project's chosen release-notes file or handoff log
- Format it using the convention already established in that project

## 6. Clean up completion notes

If the project expects a structured handoff, prepare a short completion summary:

```
## Summary

<What changed? Why was it needed?>

## Validation

- <Check or test run>
- <Check or test run>

## Known failures / follow-up

<Anything reviewers or future you should know>
```

## 7. Prepare review notes

Use the review summary template from `mimirai:review → requesting-review` if review is needed:

```
## Summary

<What does this work do? Why is it needed?>

## Changes

- <Main change 1>
- <Main change 2>

## How to test

1. <Step>
2. <Step> — expect <result>

## Notes for reviewer

<Non-obvious decisions, known limitations, follow-up tickets>
```

If formal review is needed, include:
- **Title:** short and specific
- **Reviewers:** at least one owner of the affected area, if applicable
- **Linked issues or todos:** any related identifiers the project uses

## 8. Post-handoff checks

After the handoff is shared:
- Confirm any required CI or automation checks pass
- If automation fails: fix immediately, do not leave a broken handoff for the reviewer
- Respond to any automated review tool comments before asking humans to review
