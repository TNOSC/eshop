# agents

Project-specific subagents — `.md` files with `name`, `description` and `tools` frontmatter. Each runs
in its own context window, so it starts cold: the definition must tell it which `CLAUDE.md` and rules
to read before it does anything.

| Agent | Tools | For |
|---|---|---|
| [`arch-auditor`](arch-auditor.md) | Read-only | Auditing a change against the layer, domain, handler and cache/outbox rules. Runs the architecture suite first, then reviews what the tests cannot see. Never edits. |
| [`slice-implementer`](slice-implementer.md) | Full | Building a vertical slice end to end from a short description, following the `add-feature` skill, finishing with build + tests. |
| [`test-backfiller`](test-backfiller.md) | Full | Finding untested handlers, aggregates and query handlers, and writing tests into the correct suite. |

## When to use one

A subagent is worth the cold start when the work is **large and self-contained** — a whole slice, a
full audit, a coverage sweep — and you want its intermediate reading kept out of the main
conversation. For a targeted change in files that are already in context, working directly is faster
and cheaper.

`arch-auditor` is the one that most often earns its keep: a review wants to read widely and report
narrowly, which is exactly what a separate context is good at.

## Conventions for writing one

- **State the reading list first.** A cold agent does not know this repo's rules; point it at the
  scoped `CLAUDE.md` files and `.claude/rules/` explicitly.
- **Constrain the tools.** A reviewer gets `Read, Grep, Glob, Bash` so it *cannot* edit — the
  guarantee is worth more than the convenience.
- **Say what it must not do.** Not editing, not relaxing an architecture test, not reporting a skipped
  suite as passed.
- **Require honest reporting.** Every agent here ends with an instruction to state what it could not
  verify. A subagent's report is the only thing that surfaces, so an over-confident one is worse than
  a partial one.
