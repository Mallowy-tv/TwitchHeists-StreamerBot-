---
name: plan
description: Write a step-by-step implementation plan from an approved design or clear requirements. Use after brainstorming or when given a spec.
triggers: write a plan, implementation plan, how to implement, plan this, spec, requirements
chains-to: build
sub-skills:
  - prompts/writing-plans.md
  - prompts/writing-skills.md
---

# Plan

## Project override handoff

Before following this core skill:
1. Check `.mimir/skills/plan/SKILL.md`. If it exists, load it and follow it instead of this file.
2. Otherwise check `.mimir/skills/plan/context.md`. If it exists, append it to this core skill before acting.
3. Only continue with this file when neither project override exists.

Turn an approved design into a detailed, bite-sized implementation plan. No building until the plan exists and is approved.

## When to use

- After brainstorming produces an approved design
- When given a spec or clear requirements
- Before starting any non-trivial implementation
- When the user says "write a plan", "plan this out", or "how should we implement"

## Entry checklist

1. Confirm there is an approved design or clear spec to work from
2. Read the existing codebase to understand patterns, conventions, and what already exists
3. Check `.mimir/core/project-context.md` and `.mimir/core/blueprint.md` if present

## Process

Load `prompts/writing-plans.md` for the full plan-writing process.

**Quick summary:**
1. Map all files to be created or modified before writing any tasks
2. Write tasks as bite-sized steps (2–5 minutes each) with actual code in every step
3. No placeholders — every step shows exactly what to write or run
4. Save the plan to `.github/session/plan.md` unless the user asked for a different location
5. Ask the user to review the plan before proceeding

## On completion

- Present the plan and get user approval
- Offer execution via `mimirai:build` (subagent-driven or inline)
- If creating a new MímirAI phase skill: load `prompts/writing-skills.md` in place of `prompts/writing-plans.md`
