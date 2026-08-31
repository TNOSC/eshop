# .github — GitHub Copilot configuration

This repo is configured for two assistants. Claude Code reads `.claude/` and the `CLAUDE.md` files;
GitHub Copilot — both the CLI and VS Code Copilot Chat — reads this directory plus `AGENTS.md`.
**This tree is authoritative**: where a policy exists in both places, the body lives here.

```
.github/
├── copilot-instructions.md         always loaded — the non-negotiables, kept short on purpose
├── instructions/                   path-scoped policy, loaded when a matching file is touched
├── agents/                         custom agents (subagents) — *.agent.md
├── prompts/                        VS Code Copilot Chat slash commands — *.prompt.md
├── skills/                         Copilot CLI workflows — <name>/SKILL.md
├── hooks/                          Copilot CLI lifecycle hooks — tnosc.json + the scripts
└── copilot/settings.json           repo-level permissions (settings.local.json is git-ignored)
```

MCP servers are **not** here: `.mcp.json` at the repo root is what Copilot actually reads (it takes
precedence over `.github/mcp.json`), and Claude Code reads the same file. One file, both tools.
`.vscode/mcp.json` carries the same three servers for VS Code.

## Which construct for which job

The four surfaces are not interchangeable, and the difference is *when they load*:

| | Loads | Use it for |
|---|---|---|
| **`copilot-instructions.md`** | Every session, always | The handful of rules that apply everywhere. Keep it short — it costs context on every turn. |
| **`instructions/*.instructions.md`** | When a file matching its `applyTo` glob is touched | Per-project and per-topic policy. This is where almost everything belongs. |
| **`skills/<name>/SKILL.md`** | On request, or when the description matches | A *workflow with steps* — verify, migrate, triage. Produces a process and a report. |
| **`agents/*.agent.md`** | Delegated as a subagent | Large, self-contained work you want in its own context window, or work you want to run with *fewer* tools than you have. |
| **`hooks/*.json`** | At a lifecycle moment | Mechanical guardrails and observability — things that must happen whether or not the model remembers to. |

Two distinctions worth holding on to:

- **Instructions describe; skills act.** If it is a policy the model should already know while editing,
  it is an instructions file. If it is a procedure someone invokes, it is a skill.
- **An agent's value is often its *missing* tools.** `arch-auditor` has no write tools, so "it will
  not edit anything" is a guarantee rather than a hope. That is worth more than the convenience.

## The `applyTo` glob is the whole mechanism

An instructions file is only loaded when Copilot touches a file its `applyTo` matches. A missing or
wrong glob means the rule silently never applies — the same failure mode as a misspelled cache tag:
nothing errors, the build stays green, and the convention just quietly stops being followed.

```markdown
---
description: Cache tags are constants from Server.Shared/<Context>/CacheTags.cs, never string literals
applyTo: "src/server/**/*.cs"
---
```

Comma-separate multiple patterns: `applyTo: "**/*.csproj,**/Directory.Packages.props"`.

## Keeping the two trees in step

| Content | Lives in | The other tree |
|---|---|---|
| The ten narrow policies | `instructions/*.instructions.md` | `.claude/rules/*.md` are stubs pointing here |
| The three command workflows | `skills/<name>/SKILL.md` | `.claude/commands/*.md` are stubs pointing here |
| Per-project conventions | `instructions/*.instructions.md` | **duplicated** in each scoped `CLAUDE.md` |
| Repo-wide overview | `AGENTS.md` | **duplicated** in `CLAUDE.md` |
| Scaffolding skills | `.claude/skills/` | *not* duplicated — Copilot discovers `.claude/skills/` natively |
| MCP servers | `.mcp.json` (repo root) | same file for both; `.vscode/mcp.json` mirrors it |

Only the last two rows are free. **The two duplicated rows must be changed together** — there is no
check that enforces it.

## Verifying a change here

```bash
copilot skill list          # every skill loads, no failures, no stray README entry
copilot mcp list            # aspire, serena, fluent-ui-blazor
```

Then inside `copilot`: `/instructions` lists the discovered instruction files and lets you toggle
them, `/agent` lists the custom agents, and **`/env` shows instructions, MCP servers, skills, agents
and hooks in one view** — that last one is the fastest way to confirm a new file registered.

Hooks have no listing command; test one by piping a payload into it:

```bash
echo '{"toolName":"str_replace","toolArgs":{"path":"src/server/Foo.cs"}}' \
  | bash .github/hooks/pretooluse-enforce-serena.sh
```

## Writing a hook: the one thing that will bite you

**`preToolUse` command hooks fail closed.** Exit 2, a crash, an unbound variable, a syntax error —
all of them deny the tool call. Only a timeout fails open. A bug in a `preToolUse` script does not
degrade gracefully; it blocks every edit in the repo until someone notices.

So every path that is *not* a deliberate denial must `exit 0`, including the ones that give up early
(empty payload, no path, unparseable JSON). The scripts here are written that way on purpose.
