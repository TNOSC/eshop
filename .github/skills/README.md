# skills

Copilot CLI workflows — one directory per skill, each containing a `SKILL.md` with `name` and
`description` frontmatter. The CLI discovers them automatically and invokes one when the request
matches its description, or when asked for by name.

| Skill | Does |
|---|---|
| [`verify`](verify/SKILL.md) | The definition of done: build, then the architecture, unit and integration suites. Reports honestly, including skipped suites. Fixes nothing. |
| [`migration`](migration/SKILL.md) | Adds an EF migration with the right two-context flags, then reviews the generated file for unintended drops and renames. |
| [`fix-build`](fix-build/SKILL.md) | Triages a warnings-as-errors failure: groups by rule ID, fixes causes, suppresses only as a last resort. |

These three are also exposed to VS Code Copilot Chat as `/verify`, `/migration` and `/fix-build` via
[`../prompts/`](../prompts) — those files are thin wrappers that point back here, so the body is
written once. `.claude/commands/*.md` are stubs pointing here too.

**The repo's scaffolding skills are not here.** `add-feature`, `add-entity`, `add-context`,
`add-tests`, `add-blazor-component` and `ca-review` live in `.claude/skills/`, which the Copilot CLI
discovers natively alongside this directory. Duplicating them would break Claude Code for no gain —
run `copilot skill list` to see both sources resolved together.

## Adding one

Keep the shape: frontmatter, a one-line statement of intent, numbered **Steps**, and a **Report**
section saying what to output. Link to a `../instructions/*.instructions.md` file rather than
restating policy. Be explicit about what the skill must *not* do — `verify` not fixing, `migration`
not skipping the review step — since that is where a helpful agent tends to overstep.

Do not name a file in a scanned directory `README.md` if you do not want it registered as a skill:
Copilot's loader treats one as a skill manifest. This file is safe because it sits above the
per-skill directories, not inside one.
