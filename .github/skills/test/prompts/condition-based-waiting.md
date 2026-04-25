---
name: condition-based-waiting
description: Wait for an async condition to become true before proceeding, instead of using arbitrary sleep delays
parent: test
---

# Condition-Based Waiting

Wait for conditions, not for time. `sleep(2000)` is a guess. Waiting for a condition is precise.

## When to use

- Tests involving async operations (API calls, database writes, timers, events)
- Waiting for a UI element to appear
- Waiting for a background job to complete
- Waiting for a file to be written
- Any time you're tempted to use `sleep` or `setTimeout` in a test

## The pattern

Instead of:
```javascript
await sleep(2000) // hope it's done by now
expect(result).toBe('done')
```

Wait for the condition:
```javascript
await waitFor(() => result === 'done', { timeout: 5000 })
expect(result).toBe('done')
```

## Implementation

```typescript
/**
 * Polls a condition function until it returns true or timeout is reached.
 */
async function waitFor(
  condition: () => boolean | Promise<boolean>,
  options: { timeout?: number; interval?: number } = {}
): Promise<void> {
  const { timeout = 5000, interval = 100 } = options
  const deadline = Date.now() + timeout

  while (Date.now() < deadline) {
    if (await condition()) return
    await new Promise(resolve => setTimeout(resolve, interval))
  }

  throw new Error(`Condition not met within ${timeout}ms`)
}
```

## Usage in tests

```typescript
// Wait for a database record to appear
await waitFor(async () => {
  const user = await db.users.findOne({ id: userId })
  return user !== null
}, { timeout: 3000 })

// Wait for a DOM element
await waitFor(() => document.querySelector('.loaded') !== null)

// Wait for a value to change
let value = 'pending'
startAsyncOperation().then(v => { value = v })
await waitFor(() => value !== 'pending')
expect(value).toBe('completed')
```

## Timeout values

- Unit tests: 1000ms max — if it takes longer, the implementation may be wrong
- Integration tests: 5000ms — account for real I/O
- E2E tests: 10000ms — account for rendering and network

Always set an explicit timeout. Never rely on the default.

## When NOT to use

- Synchronous operations — just call them
- Operations with callbacks — use promisification instead
- Operations that should be instant — investigate why they're slow
