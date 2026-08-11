# Working with Claude Code in this repo

Six mechanisms carry the conventions. They differ in **how they load** and **what they're for**, and
picking the wrong one is why agent config rots — the same rule ends up restated in four places and
they drift.

| Mechanism | Loads | Answers |
|---|---|---|
| `CLAUDE.md` (root + 7 scoped) | Automatically — root at launch, scoped when files in that tree are touched | *What are the conventions?* |
| `.claude/rules/*.md` | On reference — a skill, command or agent links to one | *How do I decide, in this one narrow area?* |
| `.claude/skills/` | Automatically when a request matches, or `/name` | *How do I build this?* |
| `.claude/commands/` | Only when invoked as `/name` | *Run this process and report* |
| `.claude/agents/` | Only when delegated to | *Do this large, self-contained job in its own context* |
| `.claude/hooks/` | Automatically on an event | *Catch this mistake the moment it happens* |

**One rule, one home.** Everything else links to it. When a convention changes, update the file that
owns it — the others already point there.

## The `CLAUDE.md` hierarchy

```
CLAUDE.md                                          stack, build constraints, structure, style, commands
lib/CLAUDE.md                                      framework invariants, mandatory XML docs
src/server/Tnosc.EShop.Server.Domain/CLAUDE.md     rich-domain rules, aggregate shape
…Server.Application/CLAUDE.md                      orchestration, no-business-branching, decorators
…Server.Infrastructure.Persistence/CLAUDE.md       write vs read side, raw SQL, migrations
…Server.Api/CLAUDE.md                              endpoint shape, Result → HTTP
…Server.Shared/CLAUDE.md                           cross-project constants (cache tags)
tests/CLAUDE.md                                    suite split, naming, fixtures
```

The root file is kept **under 100 lines** deliberately: it loads every session, so it holds only what
you need before the first keystroke — explicit `using`s, named arguments, Central Package Management,
the file header. Depth lives in the scoped files, which arrive exactly when they're relevant.

Nested loading is best-effort across tools and versions, so the root file also *names* the scoped
paths. If auto-loading doesn't fire, the pointer is still in context.

## Typical loops

**Build a feature**
```
/add-feature discontinue a product      → or just describe it; the skill triggers on its own
/verify                                 → build + all suites, reported honestly
/ca-review                              → convention audit before committing
```

**Change the schema**
```
… edit the aggregate and its EF configuration …
/migration Add_Reviews                  → generates AND reviews the SQL for unintended drops
/verify
```

**Fix a red build**
```
/fix-build                              → groups by rule ID, fixes causes, not symptoms
```

**Find out where things stand**
```
/plan-status                            → PLAN.md checkboxes vs. what's actually in the code
```

## Skill or command?

If it produces code from a description, it's a **skill** — it should fire without a `/`. If it runs a
process and reports, it's a **command** — you wouldn't want it firing because a sentence looked
similar. `/verify` triggering itself would be maddening; `add-feature` triggering itself is the point.

## When to delegate to an agent

A subagent starts cold and re-derives context, so it earns its cost when the work is **large and
self-contained** and you want its reading kept out of the main conversation: a full slice, an audit, a
coverage sweep. For a targeted change to files already in context, working directly is faster.

`arch-auditor` is the most reliable win — reviews read widely and report narrowly.

## Extending this setup

1. **Is it a convention?** → the `CLAUDE.md` that owns that tree.
2. **A narrow policy needing a "why"?** → `.claude/rules/`.
3. **A build workflow?** → a skill (with templates under `references/`).
4. **A process to run on demand?** → a command.
5. **A big delegated job?** → an agent.
6. **A mistake worth catching instantly?** → a hook. Keep it fast, dependency-free, and advisory.

Then **delete what you replaced.** Two files describing the same rule is how this stops being useful.

## Things that will bite

- **Warnings are errors.** A missing `using` or an unnamed argument fails the build like a type error.
- **`ImplicitUsings` is off** — even `System` is explicit.
- **`CS1591` is an error under `lib/`** — every public member needs XML docs.
- **Integration tests need Docker.** A skipped suite is not a passing suite; say which it was.
- **Architecture tests are not style checks.** A failure means the design rule broke — fix the code,
  or change the rule deliberately. Never suppress one to get green.
