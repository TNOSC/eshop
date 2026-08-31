---
name: verify
description: "Run the full definition-of-done gate — build plus the architecture, unit and integration suites — and report honestly"
---

**Arguments:** [optional: suite to limit to, e.g. "architecture" or "unit"]

Run this repo's definition of done and report what actually happened.

If the user named a suite (`build`, `architecture`, `unit`, `integration`), limit the run to it.
Otherwise run everything.

## Steps

1. **Build.** `dotnet build Tnosc.EShop.slnx`

   Warnings are errors here, so a warning *is* a failure. If it fails, stop and report — do not start
   the test suites against a stale build. Group failures by analyzer/compiler ID rather than listing
   every occurrence.

2. **Architecture tests.** `dotnet test tests/server/Tnosc.EShop.Server.Tests.Architecture`

   Run these before the others: they are fast and they catch design errors that make the rest
   meaningless. A failure here is a **blocker** — report the rule that broke and the offending type.
   Never suggest relaxing the test as the fix (`.github/instructions/analyzer-suppressions.instructions.md`).

3. **Unit tests.** `dotnet test tests/server/Tnosc.EShop.Server.Tests.Unit`

4. **Integration tests.** `dotnet test tests/server/Tnosc.EShop.Server.Tests.Integration`

   These need Docker (Testcontainers Postgres). Check first — `docker info` — and if it is not
   running, **say so explicitly and mark the suite as skipped, not passed.**

## Reporting

Give a short table: suite, pass/fail/skipped, and the count. Then the failures, each with the test
name and the assertion message, most important first.

Be exact about what did not run. "Integration tests skipped — Docker is not running" is the required
phrasing when that happens; never let a skipped suite read as a green one.

If everything passes, say so plainly in one line.

## Do not fix anything

This command reports. If there are failures, list them and stop — the user decides what to fix. The
exception is when the user explicitly asked for `/verify` *and* a fix in the same breath.
