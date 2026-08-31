---
description: "Two-context dotnet ef mechanics, reviewing the generated file, destructive changes, never editing an applied migration"
applyTo: "src/server/Tnosc.EShop.Server.Infrastructure.Persistence/**"
---

# Rule — EF Core migrations

Migrations are the one artifact here that can destroy data, and the one the compiler cannot check.

## Mechanics

Two `DbContext`s exist, so **`--context` is always required**. Only `EShopWriteDbContext` has
migrations — `EShopReadDbContext` maps read models over the same tables and never writes.

```bash
dotnet ef migrations add <Name> --context EShopWriteDbContext \
  --project src/server/Tnosc.EShop.Server.Infrastructure.Persistence \
  --startup-project src/server/Tnosc.EShop.Server.Host
```

`dotnet ef` runs outside Aspire, so it takes its connection string from
`EShopWriteDbContextFactory` — env `ConnectionStrings__eshopdb`, falling back to a local default.

Naming is `PascalCase_With_Underscores`: `Catalog_Initial`, `Add_Reviews`. One migration per logical
change; a context's first is `{Context}_Initial`.

## Always read the generated migration

`dotnet ef` generating a file is not the end of the task. Open it and check:

- **The schema is created before its tables** (`migrationBuilder.EnsureSchema`).
- **No unintended `DropColumn` / `DropTable` / `RenameColumn`.** EF infers a rename as drop-plus-add,
  which silently discards the column's data. If you meant a rename, replace it with
  `migrationBuilder.RenameColumn`.
- **No other context's objects appear.** A migration for `basket` touching `catalog.*` means a
  configuration is mis-scoped.
- Column types and lengths match the configuration (`HasPrecision(18, 2)`, `IsFixedLength()`, …).
- Indexes you declared are present, and named as declared (`ux_products_sku`).

## Destructive changes need explicit confirmation

Any `DropTable`, `DropColumn`, or a type change that narrows (`text` → `varchar(n)`, `bigint` → `int`)
loses data on an applied database. **Ask before generating one**, state what would be lost, and
prefer an additive path — add the new column, backfill, drop later — when the database is not
disposable.

## Never edit an applied migration

Once a migration has run anywhere that matters, it is immutable: its checksum is recorded in
`__EFMigrationsHistory`. Fix forward with a new migration. Editing one that only ever ran on your own
throwaway container is fine — say so when you do it.

## Local development

The Aspire Postgres resource uses `WithDataVolume()`, so data survives restarts and a schema change
may require dropping the volume before the next run picks it up. Migrations apply on startup only
when `Persistence:ApplyMigrationsOnStartup` is `true`.

Integration tests run against a Testcontainers Postgres that is migrated from scratch each session —
a migration that works there has *not* been proven against existing data.

## Checklist

- [ ] `--context EShopWriteDbContext` passed; correct `--project` / `--startup-project`.
- [ ] Name is `PascalCase_With_Underscores` and describes the change.
- [ ] Generated file read end to end; no unintended drop or rename.
- [ ] Schema created before its tables; no cross-context objects.
- [ ] `dotnet build Tnosc.EShop.slnx` clean; integration tests green.
