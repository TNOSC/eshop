# `.claude/`

Agent configuration for Tnosc.EShop.

```
.claude/
├── settings.json          permissions + hook wiring (tracked, team-shared)
├── settings.local.json    personal overrides (untracked, gitignored)
├── agents/                subagents — delegated, self-contained jobs
├── commands/              /slash workflows — invoked deliberately
├── docs/                  how the pieces fit together
├── hooks/                 scripts that run automatically on events
├── rules/                 self-contained policies, linked to from the rest
└── skills/                build workflows — trigger on matching requests
```

**Start with [`docs/claude-workflow-guide.md`](docs/claude-workflow-guide.md)** — it explains what
each mechanism is for and when to reach for which.

## What's here

| | |
|---|---|
| **Skills** (5) | `add-feature`, `add-entity`, `add-context`, `add-tests`, `ca-review` |
| **Commands** (4) | `/verify`, `/migration`, `/fix-build`, `/plan-status` |
| **Agents** (3) | `arch-auditor` (read-only), `slice-implementer`, `test-backfiller` |
| **Rules** (5) | cache tags, analyzer suppressions, migrations, domain events, dependencies |
| **Hooks** (2, live) | C# convention check on write/edit; build reminder on stop — both advisory |

Each folder has its own `README.md` with the details and the conventions for adding more.

## Note on `CLAUDE.md`

**This repo deliberately has no `.claude/CLAUDE.md`.** The conventions live in a root `CLAUDE.md` plus
seven scoped ones next to the code they govern (see the workflow guide). A second top-level
instruction file would be a competing source of truth for the same rules.

## Committing

`settings.json` is tracked — the hooks and permissions are shared. `settings.local.json` is
gitignored; put personal permission grants there.
