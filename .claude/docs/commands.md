# commands

Explicit workflows, invoked as `/name`. Each is a Markdown file with `description` and
`argument-hint` frontmatter; the body is the instructions, with `$ARGUMENTS` for what the user typed.

| Command | Does |
|---|---|
| [`/verify`](verify.md) | The definition of done: build, then the architecture, unit and integration suites. Reports honestly, including skipped suites. Fixes nothing. |
| [`/migration`](migration.md) | Adds an EF migration with the right two-context flags, then reviews the generated file for unintended drops and renames. |
| [`/fix-build`](fix-build.md) | Triages a warnings-as-errors failure: groups by rule ID, fixes causes, suppresses only as a last resort. |

## Commands vs skills

Both live in `.claude/`, and the line between them is *how they start*:

- **Skills** (`.claude/skills/`) are picked up automatically when a request matches their
  description — "add an endpoint to discontinue a product" triggers `add-feature` with no `/`. They
  describe how to *build* something, and carry code templates.
- **Commands** are only ever invoked deliberately. They are operational: verify, migrate, triage,
  report. You would not want them firing because a sentence looked similar.

If a workflow produces code from a description, it is a skill. If it runs a process and reports, it is
a command.

## Adding one

Keep the shape: frontmatter, a one-line statement of intent, numbered **Steps**, and a **Report**
section saying what to output. Link to a `.claude/rules/*.md` rather than restating policy. Be
explicit about what the command must *not* do — `/verify` not fixing, `/migration` not skipping the
review step — since that is where a helpful agent tends to overstep.

## Also available to GitHub Copilot

These three workflows are mirrored for Copilot in two places, because Copilot splits the job across
two surfaces:

- **`.github/skills/<name>/SKILL.md`** — the Copilot CLI's equivalent of a command. This is where the
  body lives; the CLI discovers it automatically.
- **`.github/prompts/<name>.prompt.md`** — VS Code Copilot Chat, invoked as `/verify` etc. A thin
  wrapper that points at the skill file.

So a change to one of these workflows means editing `.claude/commands/<name>.md` **and**
`.github/skills/<name>/SKILL.md`. This file is named `about-commands.md` rather than `README.md`
on purpose: Copilot's skill loader treats a `README.md` in a scanned directory as a skill manifest,
and `.claude/commands/` was being registered as a bogus skill named `README`.
