# hooks

Scripts that run automatically on Claude Code events. **Copying a script here does nothing** — a hook
is only live once wired in `.claude/settings.json` (team-shared, tracked) or
`.claude/settings.local.json` (personal, untracked).

Both hooks below are **wired in `settings.json`** and are **advisory**: they print and always exit 0,
so they never block a tool call or a turn.

## `posttooluse-csharp-conventions.sh`

Runs on `PostToolUse` with matcher `Write|Edit`. Reads the hook payload from stdin, pulls
`tool_input.file_path` (no `jq` dependency), and no-ops unless the file is a `.cs` that exists.
Skips generated code — `Migrations/`, `*.Designer.cs`, `obj/`, `bin/`.

Flags four things that either fail the build or fail silently:

| Check | Why it matters |
|---|---|
| Missing TNOSC copyright header | Every `.cs` file in the repo opens with it |
| Block-scoped `namespace` | `csharp_style_namespace_declarations = file_scoped:error` — a build error |
| Public members with no `///` under `lib/` | `CS1591` is a build **error** in the five framework projects |
| String-literal `[CacheTag("...")]` | Silent failure — build stays green, invalidation breaks (`../rules/cache-tags.md`) |

Windows paths in the payload (`C:\Projects\...`) are normalised, so it works under Git Bash.

**It is a fast tripwire, not the authority** — `dotnet build Tnosc.EShop.slnx` is. It catches the
handful of conventions worth knowing about a second after writing a file, rather than a minute later
at build time.

### Making it enforcing

Advisory by default because a false positive that blocks a write is far more costly than one that
prints a line. To make it block instead, change the final `exit 0` to `exit 2` and send the report to
stderr — on `PostToolUse`, exit code 2 feeds stderr back to Claude as a correction. Do this only once
you trust the checks in practice.

## `stop-build-reminder.sh`

Runs on `Stop`. If `git status --porcelain` shows changed `.cs` files, prints a reminder to build and
test, plus a note about reading the generated SQL when a migration changed. Silent when no C# changed.

It never runs a build itself: deciding when to spend that time is the user's call, and a `Stop` hook
that blocks can loop.

## Wire-up

Already present in `.claude/settings.json`:

```json
{
  "hooks": {
    "PostToolUse": [
      {
        "matcher": "Write|Edit",
        "hooks": [
          {
            "type": "command",
            "command": "bash .claude/hooks/posttooluse-csharp-conventions.sh",
            "timeout": 10
          }
        ]
      }
    ],
    "Stop": [
      {
        "hooks": [
          {
            "type": "command",
            "command": "bash .claude/hooks/stop-build-reminder.sh",
            "timeout": 10
          }
        ]
      }
    ]
  }
}
```

## Testing a hook without waiting for its event

```bash
echo '{"tool_input":{"file_path":"src/server/Tnosc.EShop.Server.Domain/Catalog/Products/Product.cs"}}' \
  | bash .claude/hooks/posttooluse-csharp-conventions.sh      # compliant file -> silent

printf '// no header\nnamespace Foo\n{\n}\n' > /tmp/bad.cs
echo '{"tool_input":{"file_path":"/tmp/bad.cs"}}' \
  | bash .claude/hooks/posttooluse-csharp-conventions.sh      # -> header + namespace warnings

bash .claude/hooks/stop-build-reminder.sh                      # -> reminder only if .cs changed
```

## Adding one

Keep them **fast** (sub-second), **dependency-free** (bash + coreutils; no `jq`, no network), and
**advisory unless there is a strong reason to block**. A hook runs on every matching event, so
anything slow or chatty becomes noise that gets ignored — which is worse than not having it.
