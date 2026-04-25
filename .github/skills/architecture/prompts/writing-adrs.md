---
name: writing-adrs
description: Write Architecture Decision Records that capture what was decided, why, and what was rejected.
parent: architecture
---

# Writing Architecture Decision Records (ADRs)

An ADR is a short document that records a significant architectural decision: the context that made it necessary, the options that were considered, and the reasoning behind the choice. ADRs are written once and rarely updated — if the decision changes, write a new ADR that supersedes the old one.

## When to write an ADR

Write an ADR when:
- A decision is hard to reverse (choosing a database engine, a message broker, an auth strategy)
- A decision will surprise future contributors without explanation
- The team debated multiple options and a record of the rejected options has value
- A decision has significant trade-offs that future maintainers need to understand

Do not write an ADR for:
- Implementation details that can be understood from the code
- Decisions that are obviously correct with no real alternatives
- Minor style or tooling choices

## Where to store ADRs

Store ADRs in `.github/docs/adr/` (create the directory if it does not exist). Name files sequentially:

```
.github/docs/adr/0001-use-postgresql-as-primary-database.md
.github/docs/adr/0002-use-redis-for-session-storage.md
.github/docs/adr/0003-adopt-event-sourcing-for-audit-log.md
```

## ADR template

```markdown
# ADR-<number>: <Title — short, imperative phrase>

**Date:** YYYY-MM-DD
**Status:** Proposed | Accepted | Deprecated | Superseded by ADR-<number>
**Deciders:** <Names or roles of the people who made this decision>

---

## Context

<Describe the situation, problem, or requirement that forced this decision. What is the
 system trying to do? What constraints exist? Why does this decision need to be made now?>

## Decision

<State the decision clearly and directly. Start with "We will..." or "We have decided to..."
 One or two sentences. The detail belongs in the options section.>

## Options considered

### Option A: <Name>
<Describe the approach.>

**Pros:**
- <pro>
- <pro>

**Cons:**
- <con>
- <con>

### Option B: <Name>
<Describe the approach.>

**Pros:**
- <pro>

**Cons:**
- <con>

### Option C: <Name> *(if applicable)*
...

## Rationale

<Explain why Option X was chosen over the others. What quality attributes does it optimise
 for? What trade-offs are being accepted? What assumptions does this decision rely on?>

## Consequences

**Positive:**
- <Expected benefit>
- <Expected benefit>

**Negative / trade-offs:**
- <Accepted cost or limitation>
- <Accepted cost or limitation>

**Risks:**
- <What could go wrong, and how it will be mitigated>

## References

- <Link to relevant documentation, RFC, blog post, prior discussion>
```

## Example: completed ADR

```markdown
# ADR-0001: Use PostgreSQL as the Primary Database

**Date:** 2026-01-15
**Status:** Accepted
**Deciders:** Engineering lead, backend team

---

## Context

We are building a multi-tenant SaaS product that stores user accounts, subscriptions, and
event history. We need a database that supports complex queries, ACID transactions, and
has strong operational tooling. The team has experience with relational databases.

## Decision

We will use PostgreSQL as the sole primary database for all persistent application data.

## Options considered

### Option A: PostgreSQL
Mature relational database with strong ACID guarantees, rich JSON support, and excellent
ecosystem tooling (pgAdmin, Metabase, pg_dump, RDS, etc.).

**Pros:**
- Full ACID transactions across tables
- JSON/JSONB columns for flexible schemas where needed
- Mature tooling and hosting options (RDS, Supabase, Neon)
- Team familiarity

**Cons:**
- Vertical scaling requires more planning than NoSQL alternatives
- Schema migrations require coordination

### Option B: MongoDB
Document database with flexible schema and horizontal scaling.

**Pros:**
- Schema flexibility during early development
- Horizontal sharding built-in

**Cons:**
- No multi-document ACID transactions (pre-4.0 behaviour still common in drivers)
- Team has limited MongoDB operations experience
- Joins require application-level logic or `$lookup` pipelines

## Rationale

PostgreSQL's ACID guarantees and relational model fit the subscription and billing data
model well. Schema migrations are manageable at our scale. The team's existing knowledge
reduces operational risk. MongoDB's schema flexibility is not needed — our data model is
well-understood.

## Consequences

**Positive:**
- Consistent, reliable transactions for billing logic
- Familiar tooling reduces operational overhead

**Negative / trade-offs:**
- Schema migrations must be written and deployed carefully
- Horizontal scaling will require read replicas or partitioning if volume grows beyond ~10M rows in hot tables

**Risks:**
- Migration complexity at scale — mitigated by adopting zero-downtime migration patterns from the start

## References

- https://www.postgresql.org/docs/current/
- Internal RFC: "Database selection for v1" (Notion, 2026-01-10)
```

## Updating an ADR

ADRs are immutable records of *past* decisions. If the decision changes:
1. Update the original ADR's status to `Superseded by ADR-<N>`
2. Write a new ADR that references the old one in its Context section
3. Do not edit the body of the old ADR
