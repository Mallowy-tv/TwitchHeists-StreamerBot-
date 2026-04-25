# AI Agent Override Resolution Spec

> Make `.github\` portable across projects by moving `.mimir` override resolution into the AI Agent runtime instead of relying on repo-local compatibility hacks.

## Problem

Today, repo content can describe the intended override order, but the runtime may still surface `.github\skills\<name>\SKILL.md` first and only then rely on in-file fallback text.

That creates two problems:

1. **Portability is weak.** A `.github\` folder copied to another project still needs repo-specific compatibility content.
2. **Verification is ambiguous.** The visible skill source may show `.github` even when the effective behavior came from `.mimir`.

## Goal

When a skill is invoked in a repo that contains a `.mimir\` folder, the AI Agent runtime must resolve the effective skill content in a deterministic way:

1. Full project override
2. Partial project override
3. Core fallback

This must happen in the runtime **before** the skill context is emitted to the model.

## Non-goals

- Do not move project-specific architecture, build, debug, test, or QA guidance out of project repos.
- Do not remove `.mimir\` or `.github\` support.
- Do not depend on repo-specific `## Project Overrides` sections in `.github\agents\MímirAI.md` for normal operation.

## Required runtime behavior

### 1. Session-start preload

At session start, if `.mimir\` exists in the project root, preload these files into agent session state if they exist:

- `.mimir\core\constraints.md`
- `.mimir\core\project-context.md`
- `.mimir\core\blueprint.md`
- `.mimir\core\design-system.md`

These are session-level inputs, not per-skill best-effort reads.

### 2. Skill resolution order

When skill `<name>` is invoked, resolve it in this order:

1. `.mimir\skills\<name>\SKILL.md`
2. `.mimir\skills\<name>\context.md`
3. `.github\skills\<name>\SKILL.md`

Rules:

- If `.mimir\skills\<name>\SKILL.md` exists, it **fully replaces** the core skill.
- Else if `.mimir\skills\<name>\context.md` exists, load the core skill and append the project context.
- Else load the core skill only.

### 3. Sub-skill resolution

Sub-skills must resolve relative to the winning parent skill:

- If `.mimir\skills\architecture\SKILL.md` wins, then `prompts\*.md` for that skill must also resolve from `.mimir\skills\architecture\prompts\`.
- Only fall back to `.github\skills\architecture\prompts\` when the parent skill came from `.github`.

### 4. Effective source reporting

Skill metadata exposed to the model or UI must report the **resolved** source, not the first fallback candidate.

Minimum useful fields:

```json
{
  "skill_name": "architecture",
  "resolved_from": ".mimir\\skills\\architecture\\SKILL.md",
  "resolution_mode": "project_replace"
}
```

Valid `resolution_mode` values:

- `project_replace`
- `project_append`
- `core_default`

### 5. Prompt merge behavior

If `context.md` is used, the runtime must build one merged effective prompt:

1. core `.github\skills\<name>\SKILL.md`
2. appended `.mimir\skills\<name>\context.md`
3. session-preloaded `.mimir\core\*` context already present in model state

This must be a runtime merge, not an instruction telling the model to go read another file later.

## Reference pseudo-code

```text
on_session_start(project_root):
    session.core = {}

    if exists(project_root + "\\.mimir"):
        for each core_file in [
            ".mimir\\core\\constraints.md",
            ".mimir\\core\\project-context.md",
            ".mimir\\core\\blueprint.md",
            ".mimir\\core\\design-system.md"
        ]:
            if exists(project_root + "\\" + core_file):
                session.core[core_file] = read_file(project_root + "\\" + core_file)


resolve_skill(project_root, skill_name):
    project_skill = project_root + "\\.mimir\\skills\\" + skill_name + "\\SKILL.md"
    project_context = project_root + "\\.mimir\\skills\\" + skill_name + "\\context.md"
    core_skill = project_root + "\\.github\\skills\\" + skill_name + "\\SKILL.md"

    if exists(project_skill):
        return {
            resolved_from: project_skill,
            resolution_mode: "project_replace",
            content: read_file(project_skill),
            prompt_root: dirname(project_skill)
        }

    if exists(project_context):
        return {
            resolved_from: project_context,
            resolution_mode: "project_append",
            content: read_file(core_skill) + "\n\n" + read_file(project_context),
            prompt_root: dirname(core_skill),
            project_prompt_root: dirname(project_context)
        }

    return {
        resolved_from: core_skill,
        resolution_mode: "core_default",
        content: read_file(core_skill),
        prompt_root: dirname(core_skill)
    }


resolve_subskill(parent_resolution, relative_prompt_path):
    if parent_resolution.resolution_mode == "project_replace":
        return read_file(parent_resolution.prompt_root + "\\" + relative_prompt_path)

    if parent_resolution.resolution_mode == "project_append":
        project_candidate = parent_resolution.project_prompt_root + "\\" + relative_prompt_path
        core_candidate = parent_resolution.prompt_root + "\\" + relative_prompt_path

        if exists(project_candidate):
            return read_file(project_candidate)

        return read_file(core_candidate)

    return read_file(parent_resolution.prompt_root + "\\" + relative_prompt_path)
```

## Acceptance tests

### Case 1: Full override

Given:

- `.mimir\skills\architecture\SKILL.md` exists
- `.github\skills\architecture\SKILL.md` exists

Expect:

- runtime uses `.mimir\skills\architecture\SKILL.md`
- `resolution_mode = project_replace`
- sub-skills resolve from `.mimir\skills\architecture\prompts\`

### Case 2: Partial override

Given:

- `.mimir\skills\test\context.md` exists
- `.github\skills\test\SKILL.md` exists

Expect:

- runtime uses merged content
- `resolution_mode = project_append`
- resolved metadata clearly shows project append mode

### Case 3: Core fallback

Given:

- no `.mimir\skills\qa\*`
- `.github\skills\qa\SKILL.md` exists

Expect:

- runtime uses `.github\skills\qa\SKILL.md`
- `resolution_mode = core_default`

### Case 4: Session core preload

Given:

- `.mimir\core\constraints.md` exists

Expect:

- constraints are in session state before any skill is invoked

### Case 5: Source reporting

Given:

- `.mimir\skills\build\context.md` exists

Expect:

- reported source is not just `.github\skills\build\SKILL.md`
- metadata shows `project_append`

### Case 6: Multi-skill runtime parity

Given:

- `.mimir\skills\architecture\SKILL.md` exists
- `.mimir\skills\build\context.md` exists
- `.mimir\skills\debug\context.md` exists
- `.mimir\skills\test\context.md` exists
- `.mimir\skills\qa\context.md` exists

Expect:

- a fresh session can invoke `architecture`, `build`, `debug`, `test`, and `qa` with the correct resolved source on the first run
- runtime resolution works without repo-specific compatibility text under `.github\agents\MímirAI.md`
- no repo-level smoke-test reminder is required just to prove those overrides loaded

## Migration plan

Once the runtime supports this spec:

1. Remove repo-specific compatibility sections under `## Project Overrides` in `.github\agents\MímirAI.md`
2. Keep generic documentation of the override model
3. Keep project-specific behavior only in:
   - `.mimir\core\*`
   - `.mimir\skills\*`
   - `.github\skills\*`

## Temporary state in this repo

This repository currently contains a repo-local compatibility bridge in `.github\agents\MímirAI.md` because the runtime does not yet implement this spec fully.

That bridge should be treated as temporary and removable once runtime-level resolution is implemented.

Current repo-local compatibility bridge carried by that file:

- `architecture` is forced to prefer `.mimir\skills\architecture\SKILL.md`
- `build`, `debug`, `test`, and `qa` are forced to append their `.mimir\skills\<name>\context.md` files

That bridge exists only because runtime-level override resolution is still incomplete.
Once this spec is implemented, the bridge and any manual verification reminder for those five skills should be removable.
