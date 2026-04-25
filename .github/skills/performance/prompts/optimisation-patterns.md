---
name: optimisation-patterns
description: Common optimisation patterns with concrete before/after examples — apply after profiling has identified the bottleneck.
parent: performance
---

# Optimisation Patterns

Apply these patterns only after `profiling.md` has identified the specific bottleneck. Each pattern includes a before/after example and a measurement step.

## Pattern 1: Memoisation (cache expensive computation)

**When to use:** A pure function is called repeatedly with the same arguments and its result does not change.

**Before:**
```js
function getReport(userId) {
  return heavyComputation(userId); // runs every call
}
```

**After:**
```js
const cache = new Map();
function getReport(userId) {
  if (cache.has(userId)) return cache.get(userId);
  const result = heavyComputation(userId);
  cache.set(userId, result);
  return result;
}
```

**Measure:** Call the function 1000 times with the same argument before and after. Compare `console.time` output.

**Watch out for:** Cache invalidation — set a TTL or invalidate on data change, otherwise stale results are returned.

## Pattern 2: Lazy loading (defer work until needed)

**When to use:** A resource (module, image, data) is loaded upfront but only needed later or conditionally.

**Before (JS):**
```js
import { HeavyChart } from './heavy-chart'; // loaded at startup
```

**After (JS):**
```js
const HeavyChart = React.lazy(() => import('./heavy-chart')); // loaded on render
```

**Before (image):**
```html
<img src="/below-fold.webp" />
```

**After (image):**
```html
<img src="/below-fold.webp" loading="lazy" />
```

**Measure:** Compare bundle size (`npx webpack-bundle-analyzer`) and Time to Interactive in Lighthouse before and after.

## Pattern 3: Query optimisation (eliminate N+1 and add indexes)

**When to use:** Database queries are the bottleneck — too many round trips, or a query does a full table scan.

**N+1 before:**
```js
const users = await db.user.findMany();
for (const user of users) {
  user.posts = await db.post.findMany({ where: { userId: user.id } }); // N queries
}
```

**N+1 after:**
```js
const users = await db.user.findMany({ include: { posts: true } }); // 1 query
```

**Missing index before:**
```sql
SELECT * FROM orders WHERE customer_email = 'x@y.com'; -- Seq Scan
```

**Missing index after:**
```sql
CREATE INDEX idx_orders_customer_email ON orders(customer_email);
-- Now uses Index Scan
```

**Measure:** Run `EXPLAIN ANALYZE` before and after. Compare actual execution time and rows scanned.

## Pattern 4: Caching (HTTP and server-side)

**When to use:** The same response is computed repeatedly for different users but the data changes infrequently.

**HTTP cache headers (static assets):**
```
Cache-Control: public, max-age=31536000, immutable
```

**HTTP cache headers (dynamic but stable content):**
```
Cache-Control: public, s-maxage=60, stale-while-revalidate=300
```

**Server-side cache with Redis:**
```js
const cached = await redis.get(cacheKey);
if (cached) return JSON.parse(cached);
const result = await expensiveDbQuery();
await redis.setex(cacheKey, 300, JSON.stringify(result)); // 5-minute TTL
return result;
```

**Measure:** Compare p50/p99 API latency with and without cache using `ab` or `k6`:
```
k6 run --vus 50 --duration 30s load-test.js
```

## Pattern 5: Preloading critical resources

**When to use:** LCP element is an image or font that is discovered late in the parse.

**Before:** Browser discovers the hero image after parsing the full HTML and CSS.

**After:**
```html
<link rel="preload" as="image" href="/hero.webp" fetchpriority="high" />
```

For fonts:
```html
<link rel="preload" as="font" type="font/woff2" href="/font.woff2" crossorigin />
```

**Measure:** Compare LCP in Lighthouse before and after. Target < 2.5s.

## Pattern 6: Reducing main thread work

**When to use:** Profiling shows long tasks (> 50ms) blocking the main thread in the browser.

- Move heavy computation to a Web Worker
- Break large synchronous loops into smaller chunks using `setTimeout(fn, 0)` or `scheduler.yield()`
- Avoid synchronous `localStorage` access in hot paths — it blocks the main thread
- Debounce or throttle event handlers that fire frequently (scroll, resize, input)

**Measure:** Compare Long Tasks count and Total Blocking Time in Chrome DevTools Performance tab.

## After each optimisation

1. Re-run the same profiling tool used to identify the bottleneck
2. Confirm the metric improved — record before and after numbers
3. Confirm no regressions — run the test suite
4. If the improvement is less than 20%, reconsider whether this was the real bottleneck
