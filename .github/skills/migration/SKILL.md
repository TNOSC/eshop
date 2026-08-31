---
name: migration
description: "Add an EF Core migration with the correct two-context flags, then review the generated file before it is trusted"
---

**Arguments:** <MigrationName, PascalCase_With_Underscores — e.g. Add_Reviews>

Add a migration for `EShopWriteDbContext` and review what EF generated.
Full policy: `.github/instructions/migrations.instructions.md`.

The migration name is whatever the user supplied. If they gave none, ask what changed and propose one.

## Steps

1. **Confirm what changed.** Check `git status` / `git diff` for modified aggregates and
   `IEntityTypeConfiguration` classes. If nothing in the model changed, stop and say so — a migration
   with no model change means the configuration is not doing what was intended.

2. **Check the name.** `PascalCase_With_Underscores`, describing the change: `Add_Reviews`,
   `Catalog_Initial`. A context's first migration is `{Context}_Initial`.

3. **Generate.** Two contexts exist, so `--context` is mandatory; only the write context has migrations.

   ```bash
   dotnet ef migrations add <Name> --context EShopWriteDbContext \
     --project src/server/Tnosc.EShop.Server.Infrastructure.Persistence \
     --startup-project src/server/Tnosc.EShop.Server.Host
   ```

4. **Read the generated migration.** This is the point of the command — generating the file is not
   the end of the task. Open it and check:

   - `EnsureSchema` comes before the tables in that schema.
   - **No unintended `DropColumn` / `DropTable` / `RenameColumn`.** EF infers a rename as
     drop-plus-add, which discards data. If a rename was intended, replace it with
     `migrationBuilder.RenameColumn`.
   - No other bounded context's objects appear.
   - Column types, lengths and precision match the configuration (`HasPrecision(18, 2)`,
     `IsFixedLength()`, …); declared indexes are present with their declared names.
   - Table and column names are `snake_case`.

5. **Stop and ask if it is destructive.** Any `DropTable`, `DropColumn`, or narrowing type change
   loses data on an applied database. Report exactly what would be lost and wait — do not proceed on
   the assumption that the database is disposable.

6. **Verify.** `dotnet build Tnosc.EShop.slnx`, then
   `dotnet test tests/server/Tnosc.EShop.Server.Tests.Integration` (needs Docker) so the migration is
   exercised against a real Postgres.

## Report

What changed in the model, the migration file path, anything notable in the generated SQL, and the
build/test result. If you edited the generated file (e.g. to convert a drop-plus-add into a rename),
say so and show the change.
