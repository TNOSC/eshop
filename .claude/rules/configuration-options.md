# Rule — configuration is read once, into a narrow `Options` class

> **Moved.** The body of this rule now lives in
> [`.github/instructions/configuration-options.instructions.md`](../../.github/instructions/configuration-options.instructions.md),
> so Claude Code and GitHub Copilot read the same prose instead of two copies that drift.
> Edit that file — this one is a redirect.

Configuration is read once, into a narrow Options class; every consumer takes the plain TOptions, never IConfiguration or IOptions<T>.
