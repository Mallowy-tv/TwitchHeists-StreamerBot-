---
name: test
description: Write and run tests using TDD. Always write a failing test before implementing. Use before and after any code change.
triggers: test, TDD, write tests, failing test, test coverage, unit test, integration test, spec
chains-to: qa
sub-skills:
  - prompts/test-driven-development.md
  - prompts/condition-based-waiting.md
---

# Test

## Project override handoff

Before following this core skill:
1. Check `.mimir/skills/test/SKILL.md`. If it exists, load it and follow it instead of this file.
2. Otherwise check `.mimir/skills/test/context.md`. If it exists, append it to this core skill before acting.
3. Only continue with this file when neither project override exists.

Write tests before implementation. A test that passes without ever failing proves nothing.

## When to use

- Before implementing any new behaviour (TDD)
- After fixing a bug — write a test that would have caught it
- When asked to add test coverage to existing code
- Before invoking `mimirai:qa`

## Entry checklist

1. Identify what behaviour needs to be tested — be specific
2. Check what test framework the project uses (look at `package.json`, `pyproject.toml`, etc.)
3. Look at existing tests for patterns and conventions — follow them

## Process

Load `prompts/test-driven-development.md` for the red-green-refactor cycle.

For async operations that require waiting for state: load `prompts/condition-based-waiting.md`.

## On completion

- Tests passing → invoke `mimirai:qa` → `verification-before-complete`
- Tests revealing new bugs → write to `todo.md` via `mimirai:track`, fix with `mimirai:debug`
