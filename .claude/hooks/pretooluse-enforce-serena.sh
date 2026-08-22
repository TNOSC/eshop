#!/usr/bin/env bash
# PreToolUse hook (matcher: Edit|Write) -- force C# edits through Serena MCP.
#
# Denies native Edit / Write on .cs files and tells the agent to use the Serena MCP
# tools instead (symbolic navigation + edits over the C# Roslyn LSP). Everything else
# (docs, json, markdown, scripts, .csproj, .editorconfig, ...) is left to the native
# tools untouched.
#
# Reads the PreToolUse event JSON on stdin; on a match, prints a deny decision as JSON
# on stdout and exits 0 (Claude Code reads hookSpecificOutput.permissionDecision from a
# PreToolUse hook's stdout -- exit code alone does not block this event). Fails open on
# any parse problem or missing path so a hook bug never hard-blocks the agent.

payload=$(cat)
[ -z "$payload" ] && exit 0

# Extract tool_input.file_path without requiring jq (same idiom as
# posttooluse-csharp-conventions.sh).
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

tool_name=$(printf '%s' "$payload" \
  | tr ',' '\n' \
  | grep -o '"tool_name"[[:space:]]*:[[:space:]]*"[^"]*"' \
  | head -1 \
  | sed 's/.*:[[:space:]]*"//; s/"$//')

reason="Native '${tool_name}' on .cs files is disabled in this project -- use the Serena MCP tools instead.\n- Overview / navigate: mcp__serena__get_symbols_overview, mcp__serena__find_symbol, mcp__serena__find_referencing_symbols.\n- Edit an existing symbol: mcp__serena__replace_symbol_body, mcp__serena__insert_after_symbol, mcp__serena__insert_before_symbol.\n- Small in-symbol edits: mcp__serena__replace_content.\n- Brand-new file Serena has never read: mcp__serena__replace_content in create mode, or ask the user to approve a one-off native Write.\nRe-do this change through the appropriate Serena tool."

printf '{"hookSpecificOutput":{"hookEventName":"PreToolUse","permissionDecision":"deny","permissionDecisionReason":"%s"}}\n' "$reason"
exit 0
