---
name: docs
description: Write and maintain project documentation — READMEs, API references, guides, and changelogs — with consistent structure and tone.
triggers: docs, documentation, readme, guide, reference, changelog, document, write up, explain, how to
chains-to: architecture
sub-skills:
  - prompts/writing-docs.md
---

# Docs

## Project override handoff

Before following this core skill:
1. Check `.mimir/skills/docs/SKILL.md`. If it exists, load it and follow it instead of this file.
2. Otherwise check `.mimir/skills/docs/context.md`. If it exists, append it to this core skill before acting.
3. Only continue with this file when neither project override exists.

Write documentation that developers actually read: clear, structured, and kept up to date with the code.

## When to use

- Writing or updating a README for a project or module
- Documenting a public API or CLI interface
- Writing a setup guide, tutorial, or how-to
- Updating a changelog after shipping a feature
- Adding inline documentation (JSDoc, docstrings, etc.)

## Entry checklist

Before writing:
1. Identify the audience — first-time user, experienced contributor, or internal team?
2. Identify the doc type — reference, tutorial, how-to, or explanation
3. Check what already exists — update rather than duplicate

## Process

Load `prompts/writing-docs.md` for documentation standards and the writing process.

For agent-authored internal docs, prefer `.github/docs/` unless the user explicitly asks for a different in-workspace location.

## On completion

- If the docs cover a major architectural decision: link to or create an ADR via `mimirai:architecture`
- If docs were written as part of finishing a feature: return to `mimirai:finish`
