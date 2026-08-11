---
description: Report where the delivery plan actually stands — cross-check PLAN.md checkboxes against the code that exists
argument-hint: "[optional: a task id to detail, e.g. T11]"
---

`PLAN.md` tracks delivery as a checklist of tasks (T0–T15), each with a file under `plan/`. Checkboxes
are ticked by hand, so they drift from reality. This command reports what is **actually** built.

`$ARGUMENTS` optionally names one task to detail instead of surveying all of them.

## Steps

1. **Read `PLAN.md`** — the progress tables, the dependency column, and the bug table (B1–B6) in
   "Why Phase 1 comes first".

2. **Cross-check each task against the codebase.** Do not trust the checkbox. For each task, look for
   the artifact it was supposed to produce, for example:

   | Task | Evidence it is really done |
   |---|---|
   | T1 `Lib.Domain` | `ValueObject.cs`, `GuidEntityId.cs`, `Repositories/IRepository.cs` exist |
   | T2 `Lib.Application` | `HandlerMetadata.cs` unwraps the decorator chain; `AddApplication` registers the pipeline |
   | T4–T6 persistence | `Outbox/`, `Contexts/{Read,Write}DbContextBase.cs`, `Migrations/` |
   | T8 integration infra | `Tests.Integration/Infrastructure/{PostgresFixture,IntegrationTestBase}.cs` |
   | T9 architecture tests | `Tests.Architecture/*.cs`, including `NoBusinessBranchingTests` |
   | T10 Catalog | The full slice: domain, handlers, endpoints, migration, unit + integration tests |
   | T11+ contexts | A folder for that context in Domain/Application/Persistence/Api, and a migration |

   Report three states: **done and ticked**, **done but unticked**, **ticked but missing or partial**.
   The last one matters most — say exactly what is absent.

3. **Check the bug table.** B1–B6 describe specific defects Phase 1 was meant to fix. For any task
   marked done that owned one, confirm the fix is present (e.g. B2: decorator attributes are read
   through `HandlerMetadata`, not `innerHandler.GetType()`).

4. **Identify the next task** — the first unfinished one whose dependencies are all satisfied. Name
   its `plan/*.md` file so the user can open it.

## Report

- A short table: task, plan file, checkbox state, actual state.
- Any discrepancy, called out plainly.
- The next actionable task, its dependencies, and its file.
- If a checkbox is wrong, say so — but **do not edit `PLAN.md`** unless asked. Reporting the drift is
  the job; correcting the record is the user's call.
