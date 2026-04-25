---
name: profiling
description: Find real performance bottlenecks before optimising — measure first with the right tools for the environment.
parent: performance
---

# Profiling

You cannot optimise what you have not measured. Profiling tells you where time is actually spent, not where you think it is spent. Run the appropriate profiling tool for the environment, then identify the top bottleneck before writing any optimisation code.

## Rule: measure before touching code

Do not assume. Do not "obviously this is the slow part". Profile first. The real bottleneck is almost never where you expect.

## Browser profiling

**Identify LCP, CLS, INP, and long tasks:**

1. Open Chrome DevTools → Performance tab
2. Click Record, perform the interaction you want to measure, click Stop
3. Look for: long tasks (red bars), layout shifts, forced reflows, blocking scripts
4. Use the Timings row to find LCP and FID markers

**Lighthouse for overall score:**
```
npx lighthouse https://your-url.com --view
```

**PageSpeed Insights for real-user data (CrUX):**
Visit https://pagespeed.web.dev/ — use field data over lab data when both are available.

**Bundle size analysis:**
```
npx webpack-bundle-analyzer stats.json
# or for Next.js:
npx @next/bundle-analyzer
```

## Node.js profiling

**CPU profiling with the built-in profiler:**
```
node --prof server.js
# run your load or scenario
node --prof-process isolate-*.log > processed.txt
```

**CPU profiling with Clinic.js (recommended):**
```
npx clinic doctor -- node server.js
npx clinic flame -- node server.js   # flamegraph for CPU
npx clinic bubbleprof -- node server.js  # async I/O bottlenecks
```

**Memory leak detection:**
```
node --inspect server.js
# Open chrome://inspect → Memory tab → Take heap snapshot before and after the scenario
# Compare snapshots to find objects that grew but were not collected
```

## Database query analysis

**PostgreSQL:**
```sql
EXPLAIN ANALYZE SELECT ...;
```
Look for: Seq Scan on large tables (missing index), high actual rows vs estimated rows (stale statistics), nested loop joins on large datasets.

**MySQL / MariaDB:**
```sql
EXPLAIN FORMAT=JSON SELECT ...;
SHOW PROFILE FOR QUERY 1;
```

**MongoDB:**
```
db.collection.find({...}).explain("executionStats")
```
Look for `COLLSCAN` (collection scan) — this means a missing index.

**ORM query logging:**
Enable query logging in your ORM (Prisma: `log: ['query']`, Sequelize: `logging: console.log`) and run the scenario. Look for N+1 queries — many small queries where one join would do.

## Serverless / edge profiling

- Use the platform's built-in tracing: AWS X-Ray, Vercel Speed Insights, Cloudflare Workers Analytics
- Measure cold start time separately from warm invocation time
- Identify if the bottleneck is compute, a downstream API call, or database connection overhead

## Output

After profiling, record:

| Environment | Tool used | Bottleneck identified | Baseline metric |
|---|---|---|---|
| Browser | Lighthouse | LCP image not preloaded | LCP: 4.2s |
| Node.js | Clinic Flame | Sync crypto in request path | p99 latency: 820ms |

Hand this to `optimisation-patterns.md` with the bottleneck clearly named.
