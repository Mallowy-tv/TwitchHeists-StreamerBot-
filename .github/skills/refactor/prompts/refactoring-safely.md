---
name: refactoring-safely
description: Refactor with confidence — get test coverage first, make small reversible steps, and verify after each change.
parent: refactor
---

# Refactoring Safely

The only safe refactor is one that has tests before you start. If behaviour breaks during refactoring, the tests will tell you immediately. Without tests, you cannot know whether you changed behaviour or not.

## The fundamental rule

> **Never refactor and change behaviour in the same step.**

One step changes the structure. A different step changes the behaviour. Never mix them. This makes reviews easier, bugs easier to isolate, and reversions safer.

## Step 1: Establish test coverage

Before changing any code, run the existing tests against the code you plan to refactor:

```bash
npm test -- --coverage   # or your equivalent
```

Check coverage for the specific files you will touch. If coverage is low (below ~80% of branches), add tests first. Do not skip this step.

**Writing characterisation tests (when there are no tests):**

If the code has no tests at all, write *characterisation tests* before touching anything:

1. Call the function or run the system under its current inputs
2. Record what it actually does (outputs, side effects, error conditions)
3. Write tests that assert this current behaviour — even if the behaviour is wrong
4. These tests will catch any accidental behaviour change during refactoring

You are not testing that the code is *correct*. You are testing that it does *the same thing before and after* the refactor.

## Step 2: Identify the smallest safe change

Do not try to refactor an entire module in one step. Break the refactor into the smallest unit that can be committed independently:

| Refactor type | Smallest unit |
|---|---|
| Rename variable/function | One rename per step |
| Extract function | One extracted function per step |
| Move file or module | Move first (no logic changes), logic changes in follow-up commits |
| Introduce abstraction | Introduce the abstraction, then migrate callers one by one |
| Split a large class | Extract one responsibility per step |
| Replace a pattern | Replace one call site, verify, then remaining call sites |

## Step 3: Make the change

Apply the single refactor step. Keep the change as mechanical as possible:

- **Rename:** use your editor's rename-symbol feature to rename everywhere at once
- **Extract function:** cut the code, paste into the new function, call the new function from the original location
- **Move:** move the file, update imports, run tests

Do not clean up anything you notice nearby — that is scope creep. Write it to `todo.md` and stay on task.

## Step 4: Verify immediately

After every step — before moving on to the next one — run the tests:

```bash
npm test
```

If tests fail:
- **Stop.** Do not make another change.
- Read the failure. Is it a legitimate behaviour change, or a test that needs updating due to a renamed import?
- If the failure is a real regression: undo the last refactor step and re-approach more carefully
- If the failure is a test updating to a renamed symbol: update the test import, then re-run

**Never accumulate failing tests and fix them at the end.** Keep tests green after every step.

## Step 5: Record progress

After each verified step, note what changed and what tests proved it safe. Small, well-described steps are easier to review and easier to undo manually if needed.

## Common safe refactors

### Extract function

Before:
```typescript
function processOrder(order: Order) {
  // validate
  if (!order.userId) throw new Error('Missing userId');
  if (order.items.length === 0) throw new Error('Empty order');
  if (order.total < 0) throw new Error('Negative total');

  // save
  db.orders.insert(order);
}
```

After (step 1 — extract, run tests):
```typescript
function validateOrder(order: Order) {
  if (!order.userId) throw new Error('Missing userId');
  if (order.items.length === 0) throw new Error('Empty order');
  if (order.total < 0) throw new Error('Negative total');
}

function processOrder(order: Order) {
  validateOrder(order);
  db.orders.insert(order);
}
```

### Rename for clarity

Before: `const d = new Date();`
After: `const createdAt = new Date();`

Use editor rename-symbol. Verify all usages updated. Run tests.

### Replace magic number with named constant

Before: `if (retryCount > 3)`
After:
```typescript
const MAX_RETRY_COUNT = 3;
// ...
if (retryCount > MAX_RETRY_COUNT)
```

### Inline unnecessary variable

Before:
```typescript
const result = computeTotal(items);
return result;
```
After:
```typescript
return computeTotal(items);
```

### Move file

```bash
# Move the file
mv src/utils/auth.ts src/auth/utils.ts

# Update all imports — use your IDE or:
# grep -r "from.*utils/auth" src/ to find them

# Run tests
npm test
```

## Refactors that are not safe without extra care

- **Changing a public API** — any rename or signature change in a function called by consumers outside this repo requires a deprecation strategy, not a direct rename
- **Changing a database schema** — this is a migration, not a refactor; use `mimirai:plan` to plan it separately
- **Deleting code** — confirm callers are removed first; dead code analysis tools help (`ts-prune`, `knip`)
- **Changing concurrency model** — async-to-sync or callback-to-promise changes can silently change error handling semantics; test with failure scenarios explicitly

## Done

A refactor is done when:
- [ ] All tests pass
- [ ] No new linter warnings
- [ ] The diff is only structural — no new features, no bug fixes mixed in
- [ ] Each recorded step is clearly described
- [ ] The `todo.md` debt item is resolved (if this was triggered by tracked debt)
