# rules

Self-contained policies that are too specific or too long to live in a `CLAUDE.md`, and that need to
be quotable from a skill, command or review.

`CLAUDE.md` files answer *what the conventions are*, at a glance, and are loaded automatically. These
answer *how to decide* in one narrow area, and are read when the topic comes up — a skill or command
links to the relevant one rather than restating it.

| Rule | Covers |
|---|---|
| [`cache-tags.md`](cache-tags.md) | `[CacheTag]` values are constants in `Server.Shared`, never literals — and the silent failure that follows if they aren't |
| [`idempotency.md`](idempotency.md) | `[Idempotent]` claims its key in the handler's own transaction; ambient `Idempotency-Key`, replay semantics, and the two constraints that make it work |
| [`analyzer-suppressions.md`](analyzer-suppressions.md) | When a `#pragma` or `.editorconfig` entry is acceptable; which suppressions are settled; what may never be suppressed |
| [`migrations.md`](migrations.md) | Two-context `dotnet ef` mechanics, reviewing the generated file, destructive changes, never editing an applied migration |
| [`domain-events.md`](domain-events.md) | `[DomainEventName]` as an immutable wire contract, event payload shape, at-least-once delivery and idempotency |
| [`dependencies.md`](dependencies.md) | Central Package Management, justifying a new package, layer discipline for package references |
| [`configuration-options.md`](configuration-options.md) | `IConfiguration`/`IOptions<T>` are touched once per settings class, in its own `AddXxx` extension method; every consumer takes the plain `TOptions` class directly, validated at startup |
| [`code-style.md`](code-style.md) | File header/layout, primary constructors, named arguments, one parameter per line past two, `Async` naming, error-code and `ErrorType` conventions |
| [`authorization.md`](authorization.md) | Coarse roles in Keycloak, fine-grained permissions as constants in code; the policy-provider chain behind `HasPermission`, and why ownership is structural rather than a check |
| [`blazor-client-mvvm.md`](blazor-client-mvvm.md) | Blazor client pages compose only; components own a colocated ViewModel + service; the shared `ClientValidation` helper that merges client- and server-side field errors through one code path |

## Adding a rule

Write one when a decision keeps needing the same explanation, and it is either too detailed for a
`CLAUDE.md` bullet or spans several projects. Keep the shape consistent: **the rule**, **why** (the
concrete failure it prevents), **how**, and a short checklist.

If a rule only ever applies inside one project, prefer that project's `CLAUDE.md`. If it is a
workflow with steps rather than a policy, it is a command or a skill instead.
