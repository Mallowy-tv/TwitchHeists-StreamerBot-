---
name: owasp-review
description: OWASP Top 10 checklist applied concretely to the codebase — one actionable check per category.
parent: security
---

# OWASP Top 10 Review

Work through each category. For each, perform the concrete check against the actual codebase — do not mark pass without verifying.

## A01: Broken Access Control

**Check:** Search for every route, API endpoint, and server action. For each one, confirm:
- Authentication is required where it should be (unauthenticated users cannot reach protected resources)
- Authorisation is enforced (user A cannot access user B's data by changing an ID in the URL or request body)
- No "security by obscurity" — hidden endpoints are not a substitute for access checks

**How to test:** Attempt to access a resource belonging to another user by substituting IDs. Attempt to access admin routes without an admin session. Expect a 401 or 403, not a 200.

## A02: Cryptographic Failures

**Check:**
- Passwords are hashed with a modern adaptive algorithm: bcrypt, argon2, or scrypt. Never MD5 or SHA-1.
- Sensitive data at rest (PII, payment data, tokens) is encrypted, not stored in plaintext
- TLS is enforced for all connections — no HTTP fallback in production
- Secrets and keys are not hardcoded in source code or committed to the repository

**How to test:** Search the codebase for `md5`, `sha1`, `base64` used on passwords. Search the workspace for hardcoded API keys, passwords, or tokens using code search patterns or a local secret-scanning tool that runs inside the workspace.

## A03: Injection

**Check:**
- All database queries use parameterised queries or an ORM that prevents raw string interpolation
- No `eval()`, `exec()`, `shell_exec()`, or equivalent with user input
- No raw SQL constructed by string concatenation
- HTML output is escaped — no `dangerouslySetInnerHTML` or `innerHTML` with user data

**How to test:** Find every place where user input reaches a query or shell command. Attempt a basic injection: `' OR '1'='1` in a text field used in a database query.

## A04: Insecure Design

**Check:**
- Rate limiting is applied to authentication endpoints (login, password reset, OTP)
- Account enumeration is prevented — login errors do not reveal whether an email exists
- Password reset flows use short-lived, single-use tokens
- Sensitive operations require re-authentication (e.g., changing email, deleting account)

**How to test:** Attempt to brute-force a login endpoint — does it rate-limit after N attempts? Attempt to reuse a password reset link after it has been used once.

## A05: Security Misconfiguration

**Check:**
- Default credentials are not in use (admin/admin, test/test)
- Error responses do not expose stack traces, internal paths, or framework versions to clients
- CORS is configured strictly — `Access-Control-Allow-Origin: *` is not used on authenticated endpoints
- Security headers are set: `Content-Security-Policy`, `X-Frame-Options`, `X-Content-Type-Options`, `Strict-Transport-Security`
- Debug mode or verbose logging is disabled in production

**How to test:** Check response headers with `curl -I https://your-domain.com`. Trigger a 500 error and inspect the response body.

## A06: Vulnerable and Outdated Components

See `dependency-audit.md` for the full process. At minimum:
- [ ] `npm audit` / `yarn audit` / `pnpm audit` returns no critical vulnerabilities
- [ ] No dependency is more than two major versions behind its latest release
- [ ] Transitive dependencies with known CVEs are overridden or replaced

## A07: Identification and Authentication Failures

**Check:**
- Session tokens are sufficiently random and long (128+ bits of entropy)
- Sessions are invalidated on logout — the token cannot be reused after logout
- Sessions expire after a reasonable idle period
- Multi-factor authentication is available for privileged accounts
- "Remember me" functionality uses a separate, rotatable long-lived token — not the session token

**How to test:** Log out and attempt to reuse the session cookie. Check token length and entropy in the cookie store.

## A08: Software and Data Integrity Failures

**Check:**
- CI/CD pipeline does not allow unsigned or unverified artifacts to be deployed
- Dependencies are pinned to exact versions or verified with a lockfile
- No untrusted CDN scripts are loaded without Subresource Integrity (SRI) hashes
- Deserialisation of user-supplied data (JSON, XML, pickle, etc.) does not execute code

**How to test:** Check `<script>` tags for external resources — do they have `integrity` attributes? Review CI/CD for steps that download and execute remote scripts without verification.

## A09: Security Logging and Monitoring Failures

**Check:**
- Authentication events (login, logout, failed login, password reset) are logged with timestamp and IP
- High-value transactions are logged (e.g., payment, account deletion, role change)
- Logs do not contain sensitive data (passwords, tokens, PII)
- Alerts exist for anomalous patterns (many failed logins, unusual data export volume)

**How to test:** Perform a login and check that the event appears in the log. Perform a failed login and verify it is logged with the IP address.

## A10: Server-Side Request Forgery (SSRF)

**Check:**
- Any feature that fetches a URL provided by the user (webhooks, link previews, file import from URL) validates the destination
- Internal network addresses (10.x, 172.16.x, 192.168.x, 169.254.x, localhost) are blocked
- Cloud metadata endpoints (169.254.169.254) are explicitly blocked
- URL schemes are restricted to `https://` — `file://`, `gopher://`, `dict://` are blocked

**How to test:** If a URL input field exists, attempt to fetch `http://169.254.169.254/latest/meta-data/` (AWS metadata endpoint). Expect a blocked or error response.

## Review output

| Category | Status | Severity | Finding |
|---|---|---|---|
| A01 Broken Access Control | Fail | High | Admin routes accessible without role check |
| A03 Injection | Pass | — | All queries use parameterised statements |

Pass findings to `mimirai:build` for remediation, or fix inline for straightforward issues.
