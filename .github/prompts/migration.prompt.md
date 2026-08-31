---
mode: agent
description: Add an EF Core migration with the correct two-context flags, then review the generated file before it is trusted
---

Add a migration and review what EF generated, following
[`.github/skills/migration/SKILL.md`](../skills/migration/SKILL.md) step by step. Full policy:
[`.github/instructions/migrations.instructions.md`](../instructions/migrations.instructions.md).

Migration name (`PascalCase_With_Underscores`, e.g. `Add_Reviews`): `${input:name}`

Two things that file is emphatic about:

- **`--context EShopWriteDbContext` is mandatory** — two contexts exist and only the write one has
  migrations.
- **Generating the file is not the end of the task.** Read it end to end for unintended
  `DropColumn`/`DropTable`/`RenameColumn`, and stop and ask before anything destructive.
