#!/usr/bin/env bash
# Copilot CLI agentStop hook -- reminds that changed C# needs a build, since warnings are errors in
# this solution and a style nit fails it as hard as a type error.
#
# Advisory only: prints and exits 0. It never runs a build itself -- deciding when to spend the time
# is the user's call, and a stop hook that blocks can loop.

cd "$(git rev-parse --show-toplevel 2>/dev/null || pwd)" || exit 0

changed=$(git status --porcelain 2>/dev/null | grep -cE '\.cs$')
[ "${changed:-0}" -eq 0 ] && exit 0

migrations=$(git status --porcelain 2>/dev/null | grep -cE 'Migrations/.*\.cs$')

printf '%s C# file(s) changed and not yet verified.\n' "$changed"
printf '  dotnet build Tnosc.EShop.slnx   (TreatWarningsAsErrors -- a warning fails it)\n'
printf '  dotnet test  Tnosc.EShop.slnx   (integration suite needs Docker)\n'
printf '  or run the `verify` skill\n'

if [ "${migrations:-0}" -gt 0 ]; then
  printf '  note: a migration changed -- read the generated SQL (.github/instructions/migrations.instructions.md)\n'
fi

exit 0
