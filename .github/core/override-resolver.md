---
name: override-resolver
description: Rules for resolving MímirAI skill overrides from .mimir/ project folders
type: core-rule
---

# Override Resolution Rules

## Resolution Order (highest priority wins)

When a skill is invoked, check in this order:

1. **Project replace** — `.mimir/skills/<name>/SKILL.md`
   - If this file exists: use it entirely. Stop here. No default content is loaded.

2. **Project append** — `.mimir/skills/<name>/context.md`
   - If this file exists: load the MímirAI default first, then append this file's content.
   - Used for adding project-specific rules, stack context, or extra constraints to a skill.

3. **Default** — `.github/skills/<name>/SKILL.md` (MímirAI core)
   - Used when no project override exists.

## Runtime fallback

If a core skill file is loaded before override resolution has been applied, that core skill must immediately:

1. Check `.mimir/skills/<name>/SKILL.md` and hand off to it if present
2. Otherwise check `.mimir/skills/<name>/context.md` and append it if present
3. Only continue with core instructions when neither project override file exists

## Core files (loaded before any skill)

These files in `.mimir/core/` are loaded at session start, not per-skill:

| File | Behaviour |
|---|---|
| `constraints.md` | Loaded first. Hard rules. Cannot be overridden by any skill. |
| `project-context.md` | Injected as background context into every skill. |
| `blueprint.md` | Notes the reference project. Applied during the build skill. |
| `design-system.md` | Component rules. Applied during UI-related planning and build work. |

## Sub-skill loading

Sub-skills in `.github/skills/<name>/prompts/` are loaded on demand — not automatically.

A sub-skill is loaded when:
- The task explicitly matches its domain (e.g. user says "use TDD" → load `.github/skills/test/prompts/test-driven-development.md`)
- The parent `SKILL.md` process section instructs the AI to load it for a specific step

## Copilot agent overrides

For Copilot users, overrides live inside `.github/agents/MímirAI.md` under `## Project Overrides` section headers — no `.mimir/` folder needed.
