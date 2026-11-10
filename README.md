# EF Core query guardrails

A small .NET 10 example of deterministic checks for LINQ and EF Core code, including code written by an AI agent. Reviewing a query by reading it, whether a person or a model does it, can miss things. These checks look at the commands EF Core is about to execute against the database and fail the test run when they change.

## Run it

```
dotnet test
```

Requires the .NET 10 SDK. The tests use SQLite in memory, so there is no server to install and nothing to configure.

## What is checked

Each read use case in `src/Shop/OrderQueries.cs` has three tests in `tests/Shop.Tests/OrderQueryTests.cs`:

| Test | Fails when |
|---|---|
| `*_sql_matches_snapshot` | The SQL text differs from the approved file in `tests/Shop.Tests/Snapshots/`. |
| `*_stays_within_query_budget` | The use case issues a number of commands other than its budget (1 here). |
| `*_tracks_nothing` | The read leaves entities in the change tracker. |

The commands are captured by `QueryCounter`, a `DbCommandInterceptor` that records every reader, scalar and non-query command EF Core is about to execute, sync or async. It hooks `ReaderExecuting`, `ScalarExecuting` and `NonQueryExecuting`, which EF Core calls before the command runs, and it keeps only `CommandText`.

Each of these tests first asserts that the query returned rows. The count does not depend on the size of the table, but it does depend on the page having rows: an N+1 sends one command per row, so on an empty page it sends one command and tracks nothing, and the budget and tracking tests would pass. The test database seeds 120 orders so every page the tests ask for is full.

Fixed rules live in `ShopContext.ApplyRules` and in `PageRequest`. They throw before any SQL is sent:

- `Skip`/`Take` without `OrderBy` throws (`RowLimitingOperationWithoutOrderByWarning` turned into an error).
- `First`/`FirstOrDefault` without `OrderBy` and without a filter throws (`FirstWithoutOrderByAndFilterWarning`). The warning does not fire when there is a predicate: `FirstAsync(c => c.IsActive)` passes even if many rows match, and returns whichever one the database gives first. A predicate does not guarantee a single row; in review, check that the filter identifies one record, or add an `OrderBy`.
- A filter EF Core cannot translate to SQL throws. That is EF Core's default since 3.0; `RuleTests` keeps it covered.
- List methods take a `PageRequest`: page 1 or more, size 1 to 100, and an overflowing offset throws. Nothing forces a new method to take one; that is review.

## Approving a snapshot

When the SQL changes, the test writes `<name>.received.sql` next to `<name>.sql` and fails with the number of commands on each side and the first line that differs. A person compares both files. If the new SQL is right, they rename `<name>.received.sql` to `<name>.sql` and commit it. `*.received.sql` is in `.gitignore`.

A snapshot checks the text of the SQL, not its parameters. `QueryCounter` keeps only `CommandText`, so parameter values, types, sizes and other parameter properties are not compared. The snapshot verifies the shape of the query; tests of the results, like `OrderTotalsTests`, and specific checks cover the rest.

Snapshot inputs are fixed on purpose. The SQL text can depend on parameter values: with EF Core 10.0.14, `Skip(10).Take(10)` produces `LIMIT @p OFFSET @p`, while `Skip(20).Take(10)` produces `LIMIT @p1 OFFSET @p`.

## The agent branch

Branch `agent/order-line-totals` shows the checks catching a real N+1. The commits on this branch were written by hand to imitate a typical agent change; no agent ran on this repository.

1. `43544d8` adds a line count and total to the recent orders list. It loads the page, then runs one query per order. The new test in that commit, which checks the values, passes. Three guardrails fail:

   ```
   SQL for ListRecent changed. Approved 1 command(s), received 11. ...
   Assert.Equal() Failure: Values differ
   Expected: 1
   Actual:   11
   Assert.Empty() Failure: Collection was not empty
   Failed!  - Failed:     3, Passed:    11, Skipped:     0, Total:    14
   ```

2. `569ebe7` computes the count and the total inside the same query. The budget and tracking tests pass. The snapshot still fails, because the SQL did change; after review the new snapshot is approved in the same commit:

   ```
   Passed!  - Failed:     0, Passed:    14, Skipped:     0, Total:    14
   ```

`main` includes both commits. To see the failure yourself: `git checkout 43544d8 && dotnet test`. Later commits on `main` add a test for the `First` rule, so `main` runs 15 tests.

## The skill asks; CI and the hook enforce

`.claude/skills/ef-query-guardrails/SKILL.md` asks an agent such as Claude Code to run `dotnet test` before it hands over a change, and to report failures instead of editing tests or approving snapshots. A skill is an instruction the model may or may not follow. The enforcement is the test run itself, and this repository ships both places where it runs:

- **CI:** `.github/workflows/test.yml` runs `dotnet test -p:ContinuousIntegrationBuild=true` on every push and pull request, with the SDK from `global.json`. A failing run blocks a merge only if branch protection requires this check to pass; set that in the repository settings on GitHub.
- **Claude Code `Stop` hook:** `.claude/settings.json` runs the tests every time Claude tries to finish a turn in this project:

  ```
  cd "$CLAUDE_PROJECT_DIR" && dotnet test 1>&2 || exit 2
  ```

  The exit code matters. `dotnet test` returns 1 when a test fails, and for a [Claude Code hook](https://code.claude.com/docs/en/hooks) any exit code other than 0 or 2 is a non-blocking error: Claude stops anyway. Exit code 2 keeps Claude from stopping, and the hook's stderr, here the test output, becomes the reason Claude sees. Claude Code ends the turn anyway after stop hooks have continued it eight times in a row; the count resets each time Claude calls a tool, so while it keeps editing, the hook keeps it working until the tests pass. Review changes to tests as well as to `Snapshots/` and the budgets. The command uses bash syntax. On Windows, Claude Code runs hooks with Git Bash when it is installed; without Git Bash it uses PowerShell, where this command fails and does not block. The hook runs the whole suite at every stop, even when nothing in the code changed; remove it from `.claude/settings.json` if that is too slow for you.

A person still reviews changes to `Snapshots/` and to the budget constants.

## What this does not catch

- **Slow SQL.** One command can still be slow. The snapshot shows the SQL, not its execution plan.
- **Client evaluation in the final `Select`.** EF Core allows it and nothing throws. The snapshot catches it only when it changes the SQL, for example when the method takes the whole entity; then the entity is tracked and the tracking test fails too.
- **Explicit client evaluation.** `AsEnumerable()` before `Where` and `Take` sends one command that reads the whole table. No rule throws and the budget passes; the snapshot fails, and so does the tracking test when the query loads entities without `AsNoTracking`.
- **Commands outside EF Core.** A `DbCommand` created from the same connection is not seen by the interceptor.
- **Provider differences.** SQLite is not your production database. The same LINQ can produce different SQL and different results on SQL Server or PostgreSQL. Run the snapshots against the provider you ship.
- **Someone approving bad SQL.** The checks are only as good as the review of `Snapshots/` and of the budgets.
- **A new test without data.** The full-page assertion covers the tests in this repository. A new guard test that asks for an empty page, or a project whose test database starts empty, loses the budget and tracking checks for an N+1.

## Why the snapshot is hand-written

`SqlSnapshot` is 67 lines, comments included, with no dependency; `SnapshotAssert` adds the xUnit call. [Verify](https://github.com/VerifyTests/Verify) (MIT) does this and more, and has an EF Core integration, but it adds `Verify`, `Argon`, `DiffEngine` and `SimpleInfoName` to the test project. For one text file per use case, comparing strings was enough. If you need diff tools, scrubbers or snapshots of other kinds, Verify is the better choice.

## Layout

```
src/Shop/                    entities, ShopContext with the fixed rules, PageRequest, OrderQueries
tests/Shop.Tests/            OrderQueryTests, RuleTests, OrderTotalsTests
tests/Shop.Tests/Guardrails/ QueryCounter, SqlSnapshot, SnapshotAssert, TestDatabase
tests/Shop.Tests/Snapshots/  approved SQL, one file per use case
.claude/skills/              the agent skill
.claude/settings.json        the Stop hook that runs the tests
.github/workflows/test.yml   the CI run
```

## License

MIT. See [LICENSE](LICENSE).
