#!/usr/bin/env bash
# Copilot CLI preToolUse hook -- force C# edits through Serena MCP.
#
# Denies Copilot's native file-writing tools on .cs files and tells the agent to use the Serena MCP
# tools instead (symbolic navigation + edits over the C# Roslyn LSP). Everything else (docs, json,
# markdown, scripts, .csproj, .editorconfig, ...) is left to the native tools untouched.
#
# Reads the preToolUse event JSON on stdin; on a match, prints a FLAT deny decision as JSON on stdout
# and exits 0. Copilot reads permissionDecision from the top level of stdout -- unlike Claude Code,
# there is no hookSpecificOutput wrapper.
#
# IMPORTANT: Copilot preToolUse command hooks FAIL CLOSED -- exit 2, a crash, or any non-zero exit
# denies the tool call (only a timeout fails open). So every non-match path below must exit 0, or a
# bug in this script silently blocks all editing in the repo.

payload=$(cat)
[ -z "$payload" ] && exit 0

# Extract toolArgs.path without requiring jq. Copilot's file tools take `path`; Claude Code's took
# `tool_input.file_path`.
file_path=$(printf '%s' "$payload" \
  | tr ',' '\n' \
  | grep -oE '"path"[[:space:]]*:[[:space:]]*"[^"]*"' \
  | head -1 \
  | sed 's/.*:[[:space:]]*"//; s/"$//')

# Unescape the backslashes JSON adds to Windows paths, so "C:\\Projects\\x.cs" becomes
# "C:/Projects/x.cs". Use tr, not the obvious ${file_path//\\//}: that parameter expansion deletes
# every forward slash instead of replacing backslashes, which reduces every path to a bare filename.
# That bug is why the .claude/hooks/ version of the postToolUse check never fired.
file_path=$(printf '%s' "$file_path" | tr -s '\\' '/')

[ -z "$file_path" ] && exit 0
case "$file_path" in
  *.cs) ;;
  *) exit 0 ;;
esac

tool_name=$(printf '%s' "$payload" \
  | tr ',' '\n' \
  | grep -oE '"toolName"[[:space:]]*:[[:space:]]*"[^"]*"' \
  | head -1 \
  | sed 's/.*:[[:space:]]*"//; s/"$//')
[ -z "$tool_name" ] && tool_name="write"

reason="Native '${tool_name}' on .cs files is disabled in this project -- use the Serena MCP tools instead.\n- Overview / navigate: serena(get_symbols_overview), serena(find_symbol), serena(find_referencing_symbols).\n- Edit an existing symbol: serena(replace_symbol_body), serena(insert_after_symbol), serena(insert_before_symbol).\n- Small in-symbol edits: serena(replace_content).\n- Brand-new file Serena has never read: serena(replace_content) in create mode, or ask the user to approve a one-off native write.\nRe-do this change through the appropriate Serena tool."

printf '{"permissionDecision":"deny","permissionDecisionReason":"%s"}\n' "$reason"
exit 0
