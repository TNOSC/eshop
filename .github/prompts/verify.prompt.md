---
mode: agent
description: Run the full definition-of-done gate — build plus the architecture, unit and integration suites — and report honestly
---

Run this repository's definition of done, exactly as written in
[`.github/skills/verify/SKILL.md`](../skills/verify/SKILL.md). Read that file first and follow it
step by step.

Suite to limit the run to, if any: `${input:suite:build | architecture | unit | integration — leave empty to run everything}`

The two things that file is emphatic about, repeated here because they are where this goes wrong:

- **Warnings are errors.** If the build fails, stop and report; do not run the suites against a stale
  build.
- **A skipped suite is not a green one.** If Docker is not running, say
  "Integration tests skipped — Docker is not running" in those words.

**Report only. Do not fix anything** unless the user asked for a fix in the same breath.
