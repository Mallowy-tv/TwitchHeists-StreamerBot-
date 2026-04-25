---
name: dependency-audit
description: Audit and update vulnerable dependencies — scan, triage by severity, and remediate critical and high findings.
parent: security
---

# Dependency Audit

Vulnerable dependencies are one of the most common sources of real-world breaches. Audit them before every release and after every significant CVE disclosure.

## Step 1: Run the appropriate scanner

**Node.js (npm):**
```
npm audit
npm audit --json > audit-results.json
```

**Node.js (pnpm):**
```
pnpm audit
pnpm audit --json > audit-results.json
```

**Node.js (yarn):**
```
yarn audit
yarn audit --json > audit-results.json
```

**Python:**
```
pip install pip-audit
pip-audit
pip-audit --output json > audit-results.json
```

**Rust:**
```
cargo install cargo-audit
cargo audit
```

**Go:**
```
go install golang.org/x/vuln/cmd/govulncheck@latest
govulncheck ./...
```

**Ruby:**
```
gem install bundler-audit
bundle audit check --update
```

## Step 2: Triage findings by severity

Do not attempt to fix everything at once. Triage first:

| Severity | Action | Timeline |
|---|---|---|
| **Critical** | Fix immediately. Block the release if not resolved. | Before any deploy |
| **High** | Fix before handing off this work. Acceptable to ship a targeted fix. | This sprint |
| **Moderate / Medium** | Schedule for the next maintenance window. Log to `todo.md` with `[HIGH]`. | Within 2 weeks |
| **Low / Informational** | Log to `todo.md`. Fix in a future cleanup pass. | Next quarter |

For each critical or high finding, confirm:
1. Is the vulnerable code path actually reachable in this application?
2. Is there a patched version available?
3. Is upgrading the dependency a breaking change?

## Step 3: Remediate

**Upgrade to a patched version:**
```
npm update <package>
npm install <package>@<patched-version>
```

**If upgrading breaks the API:** check the changelog for the breaking change. If the breakage is minor, fix it. If it is large, consider whether a temporary `overrides` / `resolutions` pin is acceptable while the upgrade is scheduled.

**Override a transitive dependency (npm):**
```json
// package.json
{
  "overrides": {
    "vulnerable-transitive-package": ">=2.3.1"
  }
}
```

**Override a transitive dependency (yarn):**
```json
// package.json
{
  "resolutions": {
    "vulnerable-transitive-package": "2.3.1"
  }
}
```

**If no patch is available:**
1. Assess exploitability in your specific use case
2. Consider replacing the dependency with an actively maintained alternative
3. If neither option is viable, document the accepted risk in `todo.md` with the CVE ID and the date it was triaged

## Step 4: Verify the fix

After upgrading:
```
npm audit   # should return zero critical/high findings
```

Run the test suite to confirm nothing broke:
```
npm test
```

## Step 5: Automate ongoing monitoring

- Enable an automated dependency update tool such as Renovate if the project uses one
- Configure the CI pipeline to fail on `npm audit --audit-level=high`
- Schedule a monthly manual audit review for any findings that require human judgement

## Audit output

Record findings before and after remediation:

| Package | CVE | Severity | Path | Status |
|---|---|---|---|---|
| lodash | CVE-2021-23337 | High | my-app > lodash | Fixed: upgraded to 4.17.21 |
| minimist | CVE-2021-44906 | Critical | jest > minimist | Fixed: npm override to 1.2.6 |
