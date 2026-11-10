---
name: ef-query-guardrails
description: Run the EF Core query guardrails before handing over any change to LINQ queries, DbContext configuration, entities or anything under src/Shop. Use when you add or edit a query, a navigation, an Include, a projection or paging.
---

# EF Core query guardrails

Before you say the change is done:

1. Run `dotnet test` from the repository root.
2. If everything passes, report the summary line as printed (for example `Passed!  - Failed:     0, Passed:    15, ...`).
3. If a test fails, do not edit the test to make it pass. Report which guardrail failed and why:
   - `*_sql_matches_snapshot`: the SQL changed. Show the diff between `tests/Shop.Tests/Snapshots/<name>.sql` and `<name>.received.sql`. Never rename a `.received.sql` file yourself; a person approves new SQL.
   - `*_stays_within_query_budget`: the use case now sends a different number of commands. A count that grows with the page size is an N+1. Rewrite the query so the database does the work in one command; do not raise the budget constant.
   - `*_tracks_nothing`: a read now tracks entities. Project to a DTO or add `AsNoTracking()`.
   - `RuleTests`: a fixed rule broke (Skip/Take without OrderBy, First without OrderBy or filter, a filter EF Core cannot translate, a page outside 1..100).
4. A new use case needs the same three tests, data that fills the page each test asks for, and a snapshot a person has reviewed.

This skill is a request, not a lock: the agent may skip it. This repository also runs the same `dotnet test` in CI (`.github/workflows/test.yml`) and in a Claude Code `Stop` hook of type `command` that exits with code 2 when the tests fail (`.claude/settings.json`), so the check does not depend on the agent. A person reviews every change to `Snapshots/` and to the budget constants.
