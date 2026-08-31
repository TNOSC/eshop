#!/usr/bin/env bash
# Copilot CLI postToolUse hook -- advisory C# convention check for Tnosc.EShop.
#
# Flags the conventions that break a `TreatWarningsAsErrors` build or that no analyzer catches:
#   - missing TNOSC copyright header
#   - block-scoped namespace (file-scoped is enforced at error severity in .editorconfig)
#   - public members with no XML docs under lib/ (CS1591 is a build ERROR there)
#   - string-literal cache tags (.github/instructions/cache-tags.instructions.md)
#
# Advisory only: always exits 0. Findings are returned to the model as `additionalContext` -- a
# postToolUse hook's bare stdout is not shown to it, unlike Claude Code's PostToolUse.

payload=$(cat)

# Copilot's file tools take `path`; Claude Code's took `tool_input.file_path`.
file_path=$(printf '%s' "$payload" \
  | tr ',' '\n' \
  | grep -oE '"path"[[:space:]]*:[[:space:]]*"[^"]*"' \
  | head -1 \
  | sed 's/.*:[[:space:]]*"//; s/"$//')

# Unescape the backslashes JSON adds to Windows paths, so "C:\\Projects\\x.cs" becomes
# "C:/Projects/x.cs". Use tr, not the obvious ${file_path//\\//}: that parameter expansion deletes
# every forward slash instead of replacing backslashes, which reduces every path to a bare filename
# and makes the -f test below always fail. That bug is why the .claude/hooks/ version never fired.
file_path=$(printf '%s' "$file_path" | tr -s '\\' '/')

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
  warnings+=("string-literal [CacheTag(\\\"...\\\")] -- use a const from Server.Shared/<Context>/CacheTags.cs (.github/instructions/cache-tags.instructions.md)")
fi

[ ${#warnings[@]} -eq 0 ] && exit 0

context="C# conventions -- ${file_path}\n"
for w in "${warnings[@]}"; do
  context="${context}  - ${w}\n"
done
context="${context}  (advisory; \`dotnet build Tnosc.EShop.slnx\` is the authority)"

printf '{"additionalContext":"%s"}\n' "$context"
exit 0
