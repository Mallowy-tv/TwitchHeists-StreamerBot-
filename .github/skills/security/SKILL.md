---
name: security
description: Vulnerability review, OWASP Top 10 checklist, and dependency audit — find and fix security issues before they ship.
triggers: security, vulnerability, CVE, OWASP, injection, XSS, auth, authentication, authorisation, authorization, dependency, audit, secrets, exploit
chains-to: build, test, qa, finish
sub-skills:
  - prompts/owasp-review.md
  - prompts/dependency-audit.md
---

# Security

## Project override handoff

Before following this core skill:
1. Check `.mimir/skills/security/SKILL.md`. If it exists, load it and follow it instead of this file.
2. Otherwise check `.mimir/skills/security/context.md`. If it exists, append it to this core skill before acting.
3. Only continue with this file when neither project override exists.

Identify and fix vulnerabilities before they reach production. Security reviews are not optional — run them before any release touching authentication, data handling, or external input.

## When to use

- Before any release or deployment
- After adding authentication, authorisation, or new data input paths
- When a CVE is disclosed for a dependency you use
- During a periodic security review cycle
- After any third-party integration is added

## Entry checklist

Before starting:
1. Identify the threat model — what is being protected, who the attackers are, and what the impact of a breach would be
2. Confirm the scope: full codebase review, or a specific feature or dependency?
3. Check if there are existing security findings in `todo.md` that should be resolved first

## Process

**Step 1: OWASP review** — load `prompts/owasp-review.md` to check the codebase against the OWASP Top 10.

**Step 2: Dependency audit** — load `prompts/dependency-audit.md` to scan and triage vulnerable packages.

## Severity definitions

| Severity | Meaning | Action |
|---|---|---|
| Critical | RCE, auth bypass, data exfiltration | Fix before any deploy. Block the release. |
| High | Privilege escalation, significant data exposure | Fix in the current sprint. Do not merge until resolved. |
| Medium | Limited data exposure, requires user interaction | Fix within two weeks. Log to `todo.md` with `[HIGH]`. |
| Low / Info | Defence in depth, minor hardening | Fix in a future maintenance window. Log to `todo.md`. |

## On completion

- Run `mimirai:qa` to verify fixes before merging
- Update `todo.md` with any findings not fixed in this session
- Chain to `mimirai:finish` once security review is clear
