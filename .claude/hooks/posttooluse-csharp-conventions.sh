#!/usr/bin/env bash
# PostToolUse hook (matcher: Write|Edit) — advisory C# convention check for Tnosc.EShop.
#
# Flags the conventions that break a `TreatWarningsAsErrors` build or that no analyzer catches:
#   - missing TNOSC copyright header
#   - block-scoped namespace (file-scoped is enforced at error severity in .editorconfig)
#   - public members with no XML docs under lib/ (CS1591 is a build ERROR there)
#   - string-literal cache tags (see .claude/rules/cache-tags.md)
#
# Advisory only: always exits 0, never blocks. To make it enforcing, change the final
# `exit 0` to `exit 2` and write the report to stderr instead of stdout -- see hooks/README.md.

payload=$(cat)

# Extract tool_input.file_path without requiring jq.
file_path=$(printf '%s' "$payload" \
  | tr ',' '\n' \
  | grep -o '"file_path"[[:space:]]*:[[:space:]]*"[^"]*"' \
  | head -1 \
  | sed 's/.*:[[:space:]]*"//; s/"$//')

# Unescape the backslashes JSON adds to Windows paths.
file_path=${file_path//\\\\//}
file_path=${file_path//\\//}

[ -z "$file_path" ] && exit 0
case "$file_path" in
  *.cs) ;;
  *) exit 0 ;;
esac
[ -f "$file_path" ] || exit 0

# Generated code is not ours to style.
case "$file_path" in
  */Migrations/*|*.Designer.cs|*/obj/*|*/bin/*) exit 0 ;;
esac

warnings=()

# 1. TNOSC copyright header.
if ! head -6 "$file_path" | grep -q 'Tunisian .NET Open Source Community'; then
  warnings+=("missing TNOSC copyright header (every .cs file opens with it)")
fi

# 2. File-scoped namespace. csharp_style_namespace_declarations = file_scoped:error
if grep -qE '^[[:space:]]*namespace[[:space:]]+[A-Za-z_][A-Za-z0-9_.]*[[:space:]]*$' "$file_path" \
   || grep -qE '^[[:space:]]*namespace[[:space:]]+[A-Za-z_][A-Za-z0-9_.]*[[:space:]]*\{' "$file_path"; then
  warnings+=("block-scoped namespace -- .editorconfig requires file-scoped (namespace X;)")
fi

# 3. XML docs on public members under lib/ -- CS1591 is a build error there.
case "$file_path" in
  */lib/*|lib/*)
    if grep -qE '^[[:space:]]*public[[:space:]]' "$file_path" && ! grep -q '///' "$file_path"; then
      warnings+=("public members with no /// docs under lib/ -- CS1591 is a build ERROR in these projects")
    fi
    ;;
esac

# 4. String-literal cache tags.
if grep -qE '\[CacheTag\("' "$file_path"; then
  warnings+=("string-literal [CacheTag(\"...\")] -- use a const from Server.Shared/<Context>/CacheTags.cs (.claude/rules/cache-tags.md)")
fi

if [ ${#warnings[@]} -gt 0 ]; then
  printf 'C# conventions -- %s\n' "$file_path"
  for w in "${warnings[@]}"; do
    printf '  - %s\n' "$w"
  done
  printf '  (advisory; `dotnet build Tnosc.EShop.slnx` is the authority)\n'
fi

exit 0
