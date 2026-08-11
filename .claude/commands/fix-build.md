---
description: Triage a failing build — group warnings-as-errors by rule, fix the cause rather than suppressing the symptom
argument-hint: "[optional: a specific rule ID or project to focus on, e.g. CA1852 or Server.Api]"
---

This solution builds with `TreatWarningsAsErrors`, `CodeAnalysisTreatWarningsAsErrors` and
`AnalysisMode=All`, across four analyzer packages. A single style nit fails the build, and a fresh
file typically trips several at once.

`$ARGUMENTS` optionally narrows the focus to one rule ID or project.

## Steps

1. **Build and capture.** `dotnet build Tnosc.EShop.slnx`

2. **Group by rule ID, not by file.** Twenty occurrences of one rule is one decision, not twenty.
   Report the grouping before fixing: rule ID, count, and one representative `file:line`.

3. **Fix the cause.** Most hits point at something real. The recurring ones here:

   | Symptom | Fix |
   |---|---|
   | `CS0246` / unknown type | A missing explicit `using` — `ImplicitUsings` is **off**, even `System` |
   | `CS1591` | Missing XML doc on a public member in `lib/`. Write the doc; never suppress this one |
   | Sealing / static suggestions | Actually seal the class, actually make the lambda `static` |
   | `IDE0007` / `var` | Use the explicit type; `var` only where the type is apparent |
   | Positional-argument hits | Name the arguments — the house style is names at every call site |
   | Nullability (`CS86xx`) | `= null!` is fine for EF-materialised properties; anywhere else, fix the flow |

4. **Suppress only as a last resort**, per `.claude/rules/analyzer-suppressions.md`: a narrow
   `#pragma` with the reason on the same line, always restored. A solution-wide `.editorconfig` entry
   only when the rule is wrong everywhere — and say so explicitly in the report.

   **Never** suppress `CS1591` in `lib/`, and never suppress anything to make a test pass.

5. **Rebuild** until clean, then run the tests — a fix that satisfies an analyzer can still break
   behaviour.

## Not in scope

An **architecture test failure is not a build warning.** If `Tests.Architecture` is red, the design
rule was broken — fix the code, or change the rule deliberately with a stated reason. Do not add an
exclusion to turn it green.

## Report

The grouping from step 2, what you changed for each group, and anything you suppressed with the
justification. If a fix was not obvious and you picked one of several options, say which and why.
