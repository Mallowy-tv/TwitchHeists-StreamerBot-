---
name: refactor
description: Improve code quality and pay down tech debt without changing observable behaviour — test coverage first, small steps, verify at each step.
triggers: refactor, clean up, tech debt, simplify, extract, rename, reorganise, improve code quality, code smell
chains-to: test, qa, review
sub-skills:
  - prompts/refactoring-safely.md
---

# Refactor

## Project override handoff

Before following this core skill:
1. Check `.mimir/skills/refactor/SKILL.md`. If it exists, load it and follow it instead of this file.
2. Otherwise check `.mimir/skills/refactor/context.md`. If it exists, append it to this core skill before acting.
3. Only continue with this file when neither project override exists.

Change the internal structure of code without changing its observable behaviour. Refactoring is only safe when you have tests that prove behaviour is preserved. Never refactor and add features in the same step.

## When to use

- Paying down tech debt tracked in `todo.md`
- A section of code is blocking a new feature because it is too tangled
- After a feature is complete and the implementation has rough edges worth smoothing
- Code review requested improvements that are structural rather than functional

## Entry checklist

Before refactoring:
1. Identify the specific code to refactor — do not refactor speculatively
2. Confirm there are tests that cover the behaviour you are changing
3. Confirm the current tests pass before you start
4. Keep the refactor scope dedicated to this refactor — do not mix with feature work

## Process

Load `prompts/refactoring-safely.md` for the step-by-step safe refactoring process.

## On completion

- Run the full test suite: if all tests pass, the refactor is done
- Invoke `mimirai:qa` for the verification gate
- Invoke `mimirai:review` if a formal review handoff is needed
- Update `todo.md` to close the debt item that triggered this work
