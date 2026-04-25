---
name: performance
description: Profile, benchmark, and optimise — measure first, then fix the right things.
triggers: performance, slow, latency, bottleneck, profiling, benchmark, optimise, optimize, memory, CPU, LCP, FPS, throughput, query, cache
chains-to: build, test, qa
sub-skills:
  - prompts/profiling.md
  - prompts/optimisation-patterns.md
---

# Performance

## Project override handoff

Before following this core skill:
1. Check `.mimir/skills/performance/SKILL.md`. If it exists, load it and follow it instead of this file.
2. Otherwise check `.mimir/skills/performance/context.md`. If it exists, append it to this core skill before acting.
3. Only continue with this file when neither project override exists.

Make the system measurably faster. Never optimise before measuring — guessing wastes time and often makes things worse.

## When to use

- Users or monitoring report slowness
- A metric (LCP, API latency, query time, memory usage) is above its target threshold
- A new feature needs a performance baseline before shipping
- Preparing for a load test or capacity review

## Entry checklist

Before any optimisation work:
1. Define the metric and its target — what is "fast enough"?
2. Confirm you have a reproducible way to measure before and after
3. Identify the environment: browser, Node.js, serverless, database, mobile

## Process

**Step 1: Profile** — load `prompts/profiling.md` to find the real bottleneck. Do not skip this step.

**Step 2: Optimise** — load `prompts/optimisation-patterns.md` to apply the right pattern for the bottleneck found.

## On completion

- Run the same measurement from Step 1 to confirm the improvement is real
- Verify with `mimirai:qa` before merging
- If changes touch critical paths, invoke `mimirai:test` to ensure no regressions
- Log any discovered technical debt to `todo.md` via `mimirai:track`
