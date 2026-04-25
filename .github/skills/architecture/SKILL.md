---
name: architecture
description: Reason about system design, define component boundaries, and record architectural decisions as ADRs.
triggers: architecture, system design, ADR, design decision, component boundary, how should the system, structure, scalability, technical decision
chains-to: plan, docs
sub-skills:
  - prompts/writing-adrs.md
---

# Architecture

## Project override handoff

Before following this core skill:
1. Check `.mimir/skills/architecture/SKILL.md`. If it exists, load it and follow it instead of this file.
2. Otherwise check `.mimir/skills/architecture/context.md`. If it exists, append it to this core skill before acting.
3. Only continue with this file when neither project override exists.

Think through system design decisions, define how components fit together, and record significant decisions as Architecture Decision Records so future contributors understand the reasoning.

## When to use

- Designing a new system or major subsystem from scratch
- Evaluating competing technical approaches for a significant decision
- Recording a decision that was made so it is not relitigated later
- Reviewing whether the current architecture still fits the requirements

## Entry checklist

Before designing:
1. Read `project-context.md` if present — understand current stack and constraints
2. Identify who the stakeholders are and what constraints are non-negotiable
3. Clarify the quality attributes that matter most (performance, availability, consistency, developer experience, cost)

## Process

**1. Understand the problem space**
Before proposing solutions, articulate the problem clearly:
- What decision needs to be made?
- What are the constraints (team size, existing systems, budget, timeline)?
- What quality attributes must the solution satisfy?

**2. Enumerate options**
List at least two viable approaches. For each:
- Describe what it is
- List its trade-offs (pros and cons relative to the problem)
- Estimate effort and reversibility

**3. Recommend and justify**
Pick the approach that best fits the constraints. Explain the reasoning. Call out what you are optimising for and what you are accepting as a trade-off.

**4. Define component boundaries**
For the chosen approach:
- Name each component and give it a single sentence of responsibility
- Draw the data flow between components inline where helpful
- Identify the contracts (APIs, message formats, event schemas) between components

**5. Record the decision**
For any significant decision: write an ADR. Load `prompts/writing-adrs.md`.

## On completion

- For a new system: invoke `mimirai:plan` to turn the architecture into an implementation plan
- For documentation: invoke `mimirai:docs`
