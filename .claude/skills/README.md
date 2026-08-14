# Tnosc.EShop Agent Skills

A skill pack that teaches Claude Code the conventions of **this** repository — rich DDD aggregates,
CQRS with the command/query handler split across projects, `Result`-based error handling, the
decorator pipeline, the outbox, and the test split. They are the executable version of the
`CLAUDE.md` files.

## What's inside

| Skill | Invoke with | What it does |
|---|---|---|
| **add-feature** | `/add-feature discontinue a product` | Scaffolds a full vertical slice: command/query, handler, validator, endpoint, and unit + integration tests. |
| **add-entity** | `/add-entity Review with a rating and author` | Adds an aggregate end to end: typed id, value objects, error catalog, domain events, repository contract, EF configuration, read model, migration. |
| **add-context** | `/add-context Basket` | Bootstraps a new bounded context: folders across all five projects, Postgres schema, routes, first slice, migration, test scaffolding. |
| **add-tests** | `/add-tests UpdateProductPriceCommandHandler` | Backfills domain, handler and query-handler tests into the right suite. |
| **ca-review** | `/ca-review` | Reviews pending changes against the layer, domain, handler, persistence and test rules. |
| **fluentui-blazor-usage** | automatic | Provides accurate coding patterns for Blazor with Fluent UI v5 — covers setup, theming, layout, dialogs, forms, data grid, icons, and common pitfalls. |

You don't have to invoke them explicitly — once installed, Claude Code picks the right skill when you
say things like "add an endpoint to discontinue a product".

## Installation

**These skills are not active where they currently sit.** Claude Code discovers project skills in
`.claude/skills/`. Copy the folders there:

```
Tnosc.EShop/
└── .claude/
    └── skills/
        ├── add-feature/
        ├── add-entity/
        ├── add-context/
        ├── add-tests/
        ├── ca-review/
        └── fluentui-blazor-usage/
```

```bash
mkdir -p .claude/skills && cp -r skills/*/ .claude/skills/
```

Verify with `/skills` — all six should be listed.

## How they relate to the `CLAUDE.md` files

`CLAUDE.md` files are **passive** context: the root one loads at launch, and the scoped ones
(`lib/`, each `src/server/*` project, `tests/`) load when files in that tree are touched. They say
what the rules are.

Skills are **active** workflows: ordered steps, file templates, and verification commands for one
kind of task. They say how to do the job. Each skill points back at the relevant `CLAUDE.md` rather
than restating it, so the rules have one home.

## Conventions these skills encode

- Rich domain — aggregates, factories, value objects and strategies own every business decision.
- **No business branching in handlers** — enforced by a Roslyn architecture test, not by review.
- Commands go through a repository contract + `IUnitOfWork`; queries take `EShopReadDbContext` and
  their handlers live in `Infrastructure.Persistence`.
- `Result`/`Result<T>` for expected failures; exceptions only for unexpected infrastructure errors.
- No manual DI registration — everything is discovered by Scrutor/assembly scan.
- Unit-test the domain and command handlers; integration-test query handlers against real Postgres.

## Customizing

Each skill is a plain Markdown file (`SKILL.md`, plus templates under `references/`). Change a
convention, edit the template once, and every future feature follows suit. Keep the skills and the
`CLAUDE.md` files in step — when a rule changes, update the `CLAUDE.md` that owns it and the skill
that applies it.
