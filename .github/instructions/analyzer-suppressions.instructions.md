---
description: "When a #pragma or .editorconfig entry is acceptable; which suppressions are settled; what may never be suppressed"
applyTo: "**/*.cs,**/.editorconfig"
---

# Rule — analyzer suppressions

The build runs `TreatWarningsAsErrors` + `CodeAnalysisTreatWarningsAsErrors` with `AnalysisMode=All`
and four analyzer packages (Meziantou, SonarAnalyzer, Roslynator, xunit.analyzers). Every warning is
a build failure, which makes suppression tempting and therefore worth a policy.

## The existing suppressions are settled

`.editorconfig` already disables ~60 rules (`CA1707`, `CA1515`, `CA2007`, `CA1031`, `CA1034`,
`CA1812`, `MA0004`, `IDE0290`, and more). Those were deliberate decisions — several are load-bearing:

| Suppressed | Why it must stay off |
|---|---|
| `CA1034` (nested types) | The decorator pipeline is built from nested handler types |
| `CA1707` (underscores) | Test naming is `X_Should_Y_When_Z` |
| `CA2007` (`ConfigureAwait`) | Application code, not a library consumed by a sync context |
| `CA1515` (make internal) | Public surface is intentional in `lib/` |

**Do not re-enable one to "clean things up", and do not re-litigate them in a review.**

## Adding a new suppression

Order of preference, always:

1. **Fix the cause.** Most analyzer hits are pointing at something real — an unsealed class, a missing
   `static` on a lambda, a positional argument. Fix it.
2. **Narrow, local `#pragma`** with the reason on the same line, when the rule is wrong *here*
   specifically:

   ```csharp
   #pragma warning disable S2068 // Local-only default for `dotnet ef` design-time execution, not a real credential.
       private const string FallbackConnectionString = "Host=localhost;…";
   #pragma warning restore S2068
   ```

   Always restore. Never disable for a whole file when a two-line span will do.
3. **A new `.editorconfig` entry** only when the rule is wrong for the *whole solution*. Add it in the
   same grouped, commented style as the existing entries, and say so in the commit message.

## Never suppress

- **`CS1591`** (missing XML doc) anywhere under `lib/` — the five framework projects set
  `GenerateDocumentationFile=true` on purpose, and the docs are the framework's contract. Write the doc.
- **Nullability warnings** (`CS86xx`) to silence a genuine null path. `= null!` on an EF-materialised
  property is fine; using it to paper over a real nullable flow is not.
- Anything to make a **test** pass. A failing analyzer in a test project is still a real finding.

## Not the same thing

An **architecture test failure is not a suppressible warning.** `Tests.Architecture` encodes design
rules. If one fails, either the code is wrong, or the rule genuinely changed and the *test* should be
updated deliberately — with the reason stated. Never add an exclusion to make a red one go away.
