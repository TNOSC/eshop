#!/usr/bin/env bash
# Copilot CLI agentStop hook -- builds and tests the solution when C# files have changed.

cd "$(git rev-parse --show-toplevel 2>/dev/null || pwd)" || exit 1

changed=$(git status --porcelain 2>/dev/null | grep -cE '\.cs$')
[ "${changed:-0}" -eq 0 ] && exit 0

migrations=$(git status --porcelain 2>/dev/null | grep -cE 'Migrations/.*\.cs$')

printf '%s C# file(s) changed; running verification.\n' "$changed"
if [ "${migrations:-0}" -gt 0 ]; then
  printf '  note: a migration changed -- read the generated SQL (.github/instructions/migrations.instructions.md)\n'
fi

printf 'Running solution build...\n'
dotnet build Tnosc.EShop.slnx
build_status=$?
if [ "$build_status" -ne 0 ]; then
  printf 'Solution build failed (exit %s); tests were not run.\n' "$build_status" >&2
  exit "$build_status"
fi

printf 'Build passed. Running full solution tests...\n'
dotnet test Tnosc.EShop.slnx --no-build
test_status=$?
if [ "$test_status" -ne 0 ]; then
  printf 'Solution tests failed (exit %s).\n' "$test_status" >&2
  exit "$test_status"
fi

printf 'Solution build and tests passed.\n'
