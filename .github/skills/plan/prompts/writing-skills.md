---
name: writing-skills
description: Create new MímirAI skill files — entry SKILL.md and sub-skill prompts
parent: plan
---

# Writing Skills

Create new MímirAI skill files that follow the established format and integrate cleanly with the rest of the system.

## When to use

- User wants to add a new skill to MímirAI
- Existing skill needs a new sub-skill
- Customising a skill for a specific project

## Skill file format

Every `.github/skills/<name>/SKILL.md` must have:

```markdown
---
name: <skill-name>
description: <one-line description used for AI context-matching — be precise>
triggers: <comma-separated keywords that suggest this skill is relevant>
chains-to: <skill-names this naturally hands off to>
sub-skills:
  - prompts/<sub-skill-name>.md
---

# Skill Name

One paragraph: what this skill does and when to invoke it.

## When to use

Bullet list of specific conditions.

## Entry checklist

Steps the AI must complete before the skill begins.

## Process

Step-by-step instructions. Reference sub-skills by name when they apply.

## On completion

- What to do next (chain-to skills)
- When to write to `todo.md`
```

## Sub-skill file format

Every `.github/skills/<name>/prompts/<sub-skill>.md` must have:

```markdown
---
name: <sub-skill-name>
description: <one-line description>
parent: <skill-name>
---

# Sub-skill Name

What this sub-skill does and when it is loaded (on demand vs. automatic).

## Process
[Detailed instructions]
```

## Quality bar

- `description` field is the AI's search index for this skill — make it specific and accurate
- `triggers` are signal words, not hard rules — keep them broad enough to match natural language
- `chains-to` only lists skills that are a natural next step — not every possible follow-up
- Sub-skills stay focused: one sub-skill per concern, not one sub-skill per skill

## Testing a new skill

After writing a skill file:
1. Read it back as if you've never seen it — is the process clear without extra context?
2. Check: would an AI know exactly what to do at every step?
3. Check: is anything left vague or dependent on unstated assumptions?
4. If unsatisfied, rewrite the unclear sections before committing
