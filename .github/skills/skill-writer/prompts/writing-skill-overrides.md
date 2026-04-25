---
name: writing-skill-overrides
description: Guide for writing project-level MímirAI skill overrides in .mimir/skills/ — when to use SKILL.md vs context.md, how to write the content, and how to test it.
parent: skill-writer
---

# Writing Project Skill Overrides

Project skill overrides live in `.mimir/skills/<name>/` and customise MímirAI's behaviour for a specific project. They do not modify the core MímirAI files in `.github/skills/`.

## Where project skills live

```
.mimir/
  skills/
    <skill-name>/
      SKILL.md        # Full replace — used instead of the core skill
      context.md      # Append — loaded after the core skill
      prompts/
        <sub-skill>.md
```

Both files are optional. You can have one, the other, or both.

## SKILL.md vs context.md — when to use which

**Use `SKILL.md` (full replace) when:**
- The core skill behaviour is wrong for this project and you want to replace it entirely
- The skill has completely different sub-skills for this project
- The skill needs a different process that conflicts with the core definition
- You are creating a brand-new skill that does not exist in MímirAI core

**Use `context.md` (append) when:**
- The core skill behaviour is correct but needs project-specific additions
- You want to add project conventions, tooling, or constraints without rewriting the whole skill
- You want to add a project-specific checklist item or note

**Rule of thumb:** start with `context.md`. Only upgrade to `SKILL.md` if you find yourself fighting against the core behaviour.

## Writing SKILL.md

The file must have valid YAML frontmatter followed by a Markdown body.

**Frontmatter fields:**

```yaml
---
name: <skill-name>          # must match the directory name
description: <one-line>     # short description for the skill list
triggers: <keywords>        # comma-separated, what user phrases invoke this
chains-to: <skills>         # comma-separated skill names this chains to
sub-skills:
  - prompts/<sub-skill>.md  # list of sub-skill files in this directory
---
```

**Body content:**

Structure the body the same way as core skills:
1. A short `## When to use` section — conditions that trigger this skill
2. An `## Entry checklist` — things to confirm before starting
3. A `## Process` section — the actual steps, referencing sub-skills by path
4. An `## On completion` section — what to do when the skill finishes

Keep the body focused. Do not repeat information from sub-skills — just reference them.

## Writing context.md

No frontmatter is required. Write plain Markdown that will be appended to the core skill instructions.

Good uses of `context.md`:
- "This project uses Vitest, not Jest. All test commands in this skill should use `vitest run`."
- "The deploy target is Railway. The deployment checklist section applies to Railway CLI."
- "This project follows a strict handoff-title format. All examples in this skill must follow it."

Keep context.md additions short and additive. If you are writing more than a page, consider whether `SKILL.md` is the right choice.

## Writing sub-skills (prompts/)

Sub-skills are loaded on demand — only when the relevant step of the skill is reached. They contain the detailed instructions for one specific task.

**Frontmatter:**
```yaml
---
name: <sub-skill-name>
description: <one-line>
parent: <skill-name>    # must match the parent SKILL.md name
---
```

**Body content:** focused, procedural, and actionable. Each sub-skill should answer: what exactly does the agent do during this step?

## Testing your skill

After writing the skill, test it by invoking it:

1. Start a new MímirAI session in the project with Copilot or another repo-agent-compatible tool
2. Type the trigger phrase or `mimirai:<skill-name>`
3. Confirm the agent loads the correct skill (project override, not core)
4. Walk through the entry checklist and first step — confirm the instructions match your intent
5. If the skill references sub-skills, trigger a sub-skill step and confirm it loads

**Signs the skill is working correctly:**
- The agent follows the project-specific process, not the generic core process
- Project tooling (correct test runner, deploy target, etc.) is used without being reminded
- The skill chains to the correct next skills on completion

**Signs the skill needs revision:**
- The agent falls back to generic advice not in the skill file
- Sub-skills are not loaded when their step is reached
- The frontmatter `name` does not match the directory name

## Common mistakes

| Mistake | Fix |
|---|---|
| `name` in frontmatter does not match directory name | Rename the directory or fix the frontmatter — they must match |
| Sub-skill path in frontmatter does not match the file | Check `prompts/` directory — paths are relative to the SKILL.md |
| context.md contradicts core skill instead of extending it | Rewrite as SKILL.md to replace the core behaviour entirely |
| SKILL.md references sub-skills that do not exist | Create the missing sub-skill files or remove them from the frontmatter list |
| Frontmatter chains-to references a skill that does not exist | Check the full skill list in `.github/agents/MímirAI.md` — use exact skill names |
