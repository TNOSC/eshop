---
agent: agent
description: Review pending changes against the repository's Clean Architecture conventions using the ca-review skill
---

Review the change set, exactly as written in
[`.github/skills/ca-review/SKILL.md`](../skills/ca-review/SKILL.md). Read that file first and follow
it step by step.

Scope to review, if any: `${input:scope:files, path or feature — leave empty to review the working-tree diff}`

**Report only. Do not fix anything** unless the user asked for a fix in the same breath.
