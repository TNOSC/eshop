# Rule — `[Idempotent]` and the one-transaction claim

> **Moved.** The body of this rule now lives in
> [`.github/instructions/idempotency.instructions.md`](../../.github/instructions/idempotency.instructions.md),
> so Claude Code and GitHub Copilot read the same prose instead of two copies that drift.
> Edit that file — this one is a redirect.

[Idempotent] claims its key in the handler own transaction; ambient Idempotency-Key, replay semantics, and the two constraints behind it.
