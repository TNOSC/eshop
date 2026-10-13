# .github — GitHub Copilot configuration

This repository configures GitHub Copilot for the CLI and VS Code Copilot Chat. `.github/copilot-instructions.md` provides
repository-wide guidance; this directory contains path-scoped instructions, reusable workflows,
custom agents and hooks.

```
.github/
├── copilot-instructions.md         always loaded — repository-wide guidance and non-negotiables
├── instructions/                   path-scoped policy, loaded when a matching file is touched
├── agents/                         custom agents (subagents) — *.agent.md
├── prompts/                        VS Code Copilot Chat slash commands — *.prompt.md
├── skills/                         Copilot CLI workflows — <name>/SKILL.md
├── hooks/                          Copilot CLI lifecycle hooks — tnosc.json + the scripts
└── copilot/settings.json           repo-level permissions (settings.local.json is git-ignored)
```

MCP servers are configured in `.mcp.json` at the repository root; `.vscode/mcp.json` carries the same
servers for VS Code.

## Which construct for which job

The surfaces are not interchangeable, and the difference is *when they load*:

| | Loads | Use it for |
|---|---|---|
| **`copilot-instructions.md`** | Every session, always | Repository-wide guidance and non-negotiables that apply everywhere. Keep it focused; scoped policy belongs in instructions files. |
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

## Authoritative sources

| Content | Lives in |
|---|---|
| Repository-wide overview | `.github/copilot-instructions.md` |
| Scoped and cross-cutting policies | `instructions/*.instructions.md` |
| Workflows | `skills/<name>/SKILL.md` |
| Custom agent definitions | `agents/*.agent.md` |
| MCP servers | `.mcp.json` at the repository root; `.vscode/mcp.json` mirrors it |

## Adding an instruction policy

Use a scoped instruction file for a cross-cutting policy or a project-specific convention that needs
more detail than `.github/copilot-instructions.md`. Keep one authoritative copy in
`instructions/<name>.instructions.md`, with an `applyTo` glob, and explain the rule, why it matters,
how to follow it, and a short checklist. Update the mapping in `.github/copilot-instructions.md` when adding a new project
scope; do not duplicate policy between files.

## Writing a custom agent

State the files and policies the agent must read before acting, keep its tool access to the minimum
needed, specify what it must not do, and require an honest report of incomplete checks. A reviewer
should not receive write tools; its read-only guarantee should come from its tool list.

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
