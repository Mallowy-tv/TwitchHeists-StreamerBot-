---
name: brainstorm
description: First-stop skill for show/render/preview/compare requests. Explore ideas, clarify requirements, and use the visual companion for browser-based option previews before building anything.
triggers: new feature, idea, design, what should, how should, build, create, add, explore, consider, show, render, preview, compare, mock up, mockup, wireframe, layout options, visual options
chains-to: plan
sub-skills:
  - prompts/visual-companion.md
---

# Brainstorm

## Project override handoff

Before following this core skill:
1. Check `.mimir/skills/brainstorm/SKILL.md`. If it exists, load it and follow it instead of this file.
2. Otherwise check `.mimir/skills/brainstorm/context.md`. If it exists, append it to this core skill before acting.
3. Only continue with this file when neither project override exists.

Explore ideas and turn them into approved designs through focused dialogue. Never start building without an approved design.

## When to use

- Starting any new feature or non-trivial change
- Requirements are unclear or not fully defined
- Multiple approaches are possible and trade-offs should be discussed
- The user says "what should", "how should", "I'm thinking of", or "can we"

## Entry checklist

Before asking questions:
1. Read the current project state — check relevant files and existing patterns
2. Check `.mimir/core/project-context.md` if present — understand the stack and conventions

## Process

**1. Explore context**
Read the codebase before asking questions. Understand what already exists, what patterns are in use, and what the change touches.

**2. Ask clarifying questions — one at a time**
- Prefer multiple-choice questions over open-ended ones
- One question per message — if a topic needs more depth, ask follow-ups in the next turn
- Focus on: purpose, constraints, success criteria, user expectations

**3. Assess scope**
Before refining details, check: is this one project or multiple independent subsystems? If it's too large for one plan, help decompose it. Each sub-project gets its own brainstorm → plan → build cycle.

**4. Propose 2–3 approaches**
Present approaches conversationally with trade-offs and a clear recommendation. Lead with the recommended option and explain why.

**5. Present design in sections**
Once you understand what's being built, present the design section by section:
- Architecture and component breakdown
- Data flow and interfaces
- Error handling approach
- Testing strategy

Ask after each section whether it looks right before moving on.

**6. Get explicit approval**
Do not proceed to planning until the user has approved the design. "Sounds good" counts. Silence does not.

## On completion

- Invoke `mimirai:plan` to write the implementation plan
- If debt or existing issues were discovered while exploring: write them to `todo.md` via `mimirai:track`

## Visual questions

For questions involving layouts, mockups, diagrams, previews, or side-by-side comparisons: load `prompts/visual-companion.md` and use the local browser server.

Requests phrased as **show**, **render**, **preview**, **mock up**, or **compare** are visual requests even when they could be answered conceptually in words. Prompts like "Show me three layout options for the Features section" must produce a browser render, not a prose-only list in the terminal.

Use terminal-only answers for conceptual design discussion only when the user is not asking to see the result. Minor design adjustments that can be explained clearly in chat may stay in chat.
For design alternatives, pair the browser preview with a short option summary in chat and tell the user to come back to chat and say which option they want.

When the visual companion assets exist, do **not** ask the user whether the server should be started or whether an HTML file should be created. Writes under `.github\.mimir-visual\` are pre-approved for this workflow. Write the screen first, then start and confirm the server.

If the prompt file is unavailable, follow this inline visual companion flow:

1. Ensure the repo has `.github/scripts/server/server.js`, `.github/scripts/server/ui.html`, and matching PowerShell start/stop scripts.
   - If they are missing, say so plainly and fall back to text.
2. Ensure `.github\.mimir-visual\screens\` exists and write a new HTML fragment there.
3. Start the server with `powershell -File .\.github\scripts\server\start-server.ps1 -ProjectDir .`.
4. Confirm the screen is available through the visual companion.
5. Tell the user the local URL only after the screen is ready.
6. In chat, outline each option briefly so the user has a text summary alongside the visual preview.
7. Tell the user to return to chat and state which option they want.
8. For layout questions, render actual wireframes or structural mockups of each option, not just prose cards.
9. Keep those mockups low-fidelity: skeletons, placeholder content, and simple icons are preferred over full builds or project-specific styling.
10. Use at most 4 options per screen and never reuse the same filename.

Hard rules for this flow:

- Do **not** create standalone preview files such as `preview.html`, `lumio-*.html`, or any `file://`-only artifact for visual requests.
- Do **not** create `render.js`, `package.json`, `npm` scaffolding, or install browser automation packages for this workflow.
- Do **not** fall back to screenshots generated from ad hoc HTML pages when the visual companion server can be used.
- Do **not** turn the mockup into a full implementation or try to mirror the entire live app just to show an option.
- The final user-facing handoff for a visual request must point to the running local server URL and/or the `screen_dir` output, not a standalone HTML file path.
