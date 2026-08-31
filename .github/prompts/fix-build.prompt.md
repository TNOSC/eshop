---
mode: agent
description: Triage a failing build — group warnings-as-errors by rule, fix the cause rather than suppressing the symptom
---

Triage the failing build following
[`.github/skills/fix-build/SKILL.md`](../skills/fix-build/SKILL.md) step by step. Suppression policy:
[`.github/instructions/analyzer-suppressions.instructions.md`](../instructions/analyzer-suppressions.instructions.md).

Rule ID or project to focus on, if any: `${input:focus:e.g. CA1852 or Server.Api — leave empty for the whole solution}`

Two things that file is emphatic about:

- **Group by rule ID, not by file.** Twenty occurrences of one rule is one decision.
- **An architecture-test failure is not a build warning.** Fix the code or change the rule
  deliberately; never add an exclusion to turn it green.
