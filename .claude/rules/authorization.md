# Rule — roles live in Keycloak, permissions live in code

> **Moved.** The body of this rule now lives in
> [`.github/instructions/authorization.instructions.md`](../../.github/instructions/authorization.instructions.md),
> so Claude Code and GitHub Copilot read the same prose instead of two copies that drift.
> Edit that file — this one is a redirect.

Coarse roles live in Keycloak, fine-grained permissions live in code as constants; the policy-provider chain behind HasPermission.
