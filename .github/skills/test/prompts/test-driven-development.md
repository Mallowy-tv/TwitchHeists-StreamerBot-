---
name: test-driven-development
description: Red-green-refactor TDD cycle — write a failing test first, implement minimal code to pass it, then clean up
parent: test
---

# Test-Driven Development

Write the test first. Implementation second. Always.

## The cycle

```
RED → GREEN → REFACTOR
```

1. **RED: Write a failing test**
2. **Run it — confirm it fails**
3. **GREEN: Write minimal code to make it pass**
4. **Run it — confirm it passes**
5. **REFACTOR: Clean up without changing behaviour**
6. **Run again — confirm still passing**
7. **Commit**

## Step 1: Write the failing test

Write a test that:
- Tests one specific behaviour
- Has a descriptive name that explains what it verifies
- Would fail for the right reason (not because of a typo or import error)

```typescript
// Good: tests specific behaviour with clear name
it('returns null when user is not found', async () => {
  const result = await getUser('nonexistent-id')
  expect(result).toBeNull()
})

// Bad: tests nothing specific
it('works', () => {
  expect(true).toBe(true)
})
```

## Step 2: Run it — confirm it FAILS

Run the test before writing any implementation. If it passes without implementation:
- The test is testing the wrong thing
- The behaviour already exists
- The assertion is wrong

A test that has never failed is not a test.

## Step 3: Write MINIMAL implementation

Write only what is needed to make the test pass. Nothing more.

- Do not add error handling for cases not tested yet
- Do not add features not yet tested
- Do not refactor existing code while making the test pass

## Step 4: Run it — confirm it PASSES

If it doesn't pass, fix the implementation. Do not change the test to make it pass (unless the test was wrong to begin with — in which case fix it and go back to Step 2).

## Step 5: Refactor

Now that behaviour is locked by the test, clean up:
- Remove duplication
- Improve naming
- Simplify logic

Run the test after every change. If it breaks, undo.

## Step 6: Record the completed change

Note what test and implementation now pass so the result is easy to review later.

## What makes a good test

- Tests behaviour, not implementation
- One assertion per test (or closely related assertions)
- Independent — does not depend on other tests running first
- Fast — if it's slow, it won't be run
- Readable — the test name and body explain what is being verified

## What not to test

- Implementation details (if you rename a private function, no test should break)
- Framework behaviour (trust that React renders, that Express routes)
- Trivial getters/setters with no logic
