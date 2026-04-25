---
name: verification-before-complete
description: Evidence-based completion checklist — run commands and confirm output before claiming any task is done
parent: qa
---

# Verification Before Complete

Run these checks before claiming done. Show the output, not just the command.

## The checklist

**1. Tests pass**
Run the test suite. Show the output.
```bash
# Example — use whatever the project's test command is
npm test
# or
pytest
# or
go test ./...
```
Required output: all tests passing, zero failures.

If there is no test suite: write at least one test that verifies the core behaviour you just built or fixed.

**2. The specific behaviour works**
Demonstrate the thing you built or fixed actually works:
- If it's a function: call it with real inputs, show the output
- If it's an API endpoint: make a real request, show the response
- If it's a UI change: describe the interaction that verifies it
- If it's a bug fix: reproduce the original bug scenario, confirm it no longer occurs

**3. No regressions in adjacent areas**
Run any tests that cover code you touched. If you modified `auth/login.ts`, run the auth tests — not just the login tests.

**4. The changed files are reviewed**
Review the files you changed:
- No debug logs left in
- No TODO comments added (use `todo.md` instead)
- No hardcoded values that should be configurable
- No sensitive data (passwords, tokens, keys)

## Reporting completion

Do not say "done" or "complete" without showing evidence. Instead:

```
✅ Tests: 42 passing, 0 failing (npm test)
✅ Behaviour: POST /api/login returns 200 with token for valid credentials
✅ No regressions: auth test suite all passing
✅ Reviewed: changed files checked for debug code, TODOs, and sensitive data
```

## If something fails

- Tests failing → fix with `mimirai:debug`, re-verify
- Behaviour wrong → fix, re-verify
- New issues found → write to `todo.md` via `mimirai:track`, continue with current verification
