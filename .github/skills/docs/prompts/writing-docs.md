---
name: writing-docs
description: Documentation standards — structure, tone, and process for READMEs, API references, guides, and changelogs.
parent: docs
---

# Writing Documentation

Good documentation has one job: help the reader do what they need to do. Write for the reader's goal, not for completeness.

## The four types of documentation

Every piece of documentation fits one of four types. Match the type to what the reader needs:

| Type | Reader's goal | What it contains | Example |
|---|---|---|---|
| **Tutorial** | Learn by doing | Step-by-step walkthrough with a concrete outcome | "Build your first API endpoint" |
| **How-to guide** | Accomplish a specific task | Steps to complete a real-world task, assumes some knowledge | "How to add a new database migration" |
| **Reference** | Look up specific information | Accurate, complete description of an API, CLI, or config | `README.md` API section, JSDoc |
| **Explanation** | Understand why | Concepts, background, trade-offs | Architecture overview, ADR |

Do not mix types in one document. If a README is trying to be a tutorial *and* a reference at the same time, split it.

## README structure

A project README should answer four questions in order:

```markdown
# Project Name

> One-sentence tagline: what it does and who it is for.

## Quick start

<The shortest possible path from "just cloned" to "working example".>
<3–5 commands maximum. If setup takes more than 5 steps, something is wrong.>

## Usage

<How to use the project once it is running. Show the most common use cases first.>
<Code examples are better than prose. Show input → output wherever possible.>

## Configuration

<All configuration options in a table: name, type, default, description.>

| Variable | Type | Default | Description |
|---|---|---|---|
| `PORT` | number | `3000` | Port the server listens on |
| `DATABASE_URL` | string | — | PostgreSQL connection string (required) |

## Development

<How to run tests, lint, and build. One command per task.>

```bash
npm test         # run all tests
npm run lint     # lint and type-check
npm run build    # build for production
```

## Contributing

<Link to CONTRIBUTING.md, or 2–3 sentences if the project is small.>

## License

<License name and link.>
```

## API reference

For each function, method, endpoint, or CLI command, document:

**Function/method template:**
```
### functionName(param1, param2)

<One sentence: what it does.>

**Parameters:**
| Name | Type | Required | Description |
|---|---|---|---|
| `param1` | `string` | Yes | The user's email address |
| `param2` | `options` | No | Configuration object (see below) |

**Returns:** `Promise<User>` — the created user record, or throws `ValidationError` if input is invalid.

**Example:**
```typescript
const user = await createUser('alice@example.com', { role: 'admin' });
```
```

**HTTP endpoint template:**
```
### POST /auth/login

Authenticate a user and return a session token.

**Request body:**
```json
{
  "email": "alice@example.com",
  "password": "hunter2"
}
```

**Response `200 OK`:**
```json
{
  "token": "eyJhbGci...",
  "expiresAt": "2026-04-15T12:00:00Z"
}
```

**Errors:**
| Status | Code | Meaning |
|---|---|---|
| `401` | `INVALID_CREDENTIALS` | Email/password combination not found |
| `429` | `RATE_LIMITED` | Too many attempts — retry after `Retry-After` seconds |
```

## Writing style

- **Use second person:** "You can configure…" not "The user can configure…"
- **Use active voice:** "Run `npm install`" not "Dependencies can be installed by running…"
- **Lead with verbs:** "Add a route", "Configure the database", not "Route addition", "Database configuration"
- **One idea per sentence.** If a sentence has more than one clause, split it.
- **Show, don't tell:** code examples beat prose descriptions every time
- **Do not over-explain the obvious.** Trust the reader to be a developer.

## Keeping docs up to date

Documentation rots the moment code changes without a corresponding docs update. To prevent this:

1. When a change updates a public API or CLI: updating the docs is part of the change, not a follow-up
2. When a change adds a new config option: add it to the reference table immediately
3. If you notice a doc is stale while working on something else: write it to `todo.md` via `mimirai:track` — do not fix it mid-task unless it is a one-line correction

## Changelog format

Follow [Keep a Changelog](https://keepachangelog.com) conventions:

```markdown
# Changelog

## [Unreleased]

### Added
- Short description of a new feature (#123)

### Changed
- Short description of a change to existing behaviour (#124)

### Fixed
- Short description of a bug fix (#125)

### Removed
- Short description of a removed feature (#126)

## [1.2.0] — 2026-03-01

### Added
- User profile photos (#98)
```

Entry format: `- <description of change>`

Types: `Added`, `Changed`, `Fixed`, `Removed`, `Deprecated`, `Security`

## Inline documentation (code comments)

Write a doc comment for every:
- Exported function or method
- Public class or interface
- Non-obvious constant or configuration value
- Complex algorithm or business rule

Do **not** write doc comments for:
- Private implementation details that are clear from the code
- Getters and setters that simply return/set a field with an obvious name

**Good doc comment (explains why and what, not how):**
```typescript
/**
 * Hashes a password using bcrypt with a cost factor of 12.
 * Cost factor 12 is chosen to keep login time under 300ms on current hardware
 * while remaining infeasible to brute-force.
 *
 * @param plaintext - The raw password from the user
 * @returns The bcrypt hash, safe to store in the database
 */
async function hashPassword(plaintext: string): Promise<string>
```

**Bad doc comment (restates the code):**
```typescript
/**
 * Hash password function.
 * Takes a plaintext password and returns a hash.
 */
async function hashPassword(plaintext: string): Promise<string>
```
