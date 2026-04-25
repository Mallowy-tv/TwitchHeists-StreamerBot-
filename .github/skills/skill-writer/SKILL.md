---
name: skill-writer
description: Create new MímirAI skill override files for this project — write .mimir/skills/ overrides that customise or extend .github/skills core skills.
triggers: skill writer, new phase, custom phase, override phase, write skill, project skill, extend mimirai
chains-to: brainstorm
sub-skills:
  - prompts/writing-skill-overrides.md
---

# Skill Writer

## Project override handoff

Before following this core skill:
1. Check `.mimir/skills/skill-writer/SKILL.md`. If it exists, load it and follow it instead of this file.
2. Otherwise check `.mimir/skills/skill-writer/context.md`. If it exists, append it to this core skill before acting.
3. Only continue with this file when neither project override exists.

Write project-specific skill overrides that customise MímirAI's behaviour for this codebase. These live in `.mimir/skills/` and are loaded instead of (or alongside) the core skills.

## When to use

- A core skill does not fit this project's workflow
- A skill needs project-specific context (e.g., the build skill needs to know this project uses Nx, or the test skill must always use a custom runner)
- A new skill is needed that does not exist in MímirAI core

## Entry checklist

Before writing a skill:
1. Confirm the skill name — does a core skill already exist for this need?
2. Decide the override strategy: full replace (`SKILL.md`) or append (`context.md`)
3. Gather the project-specific information the skill needs to contain

## Process

Load `prompts/writing-skill-overrides.md` for the complete authoring guide.

## On completion

- Test the skill by invoking it and confirming the correct behaviour
- If the skill replaces a core skill entirely, confirm that all sub-skills referenced in `SKILL.md` frontmatter exist in `.mimir/skills/<name>/prompts/`
- Record the new skill files so they are easy to review later
