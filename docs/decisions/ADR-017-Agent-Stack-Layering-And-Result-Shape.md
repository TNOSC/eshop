# ADR-017: Agent stack layering, and `AgentResult` as a value inside `Result<T>`

## Status

Accepted

## Date

2026-08-22

## Context

`src/agent/Tnosc.EShop.Agent.Host` existed as a bare Web SDK template. The goal was to grow it into a
host that instantiates agents with the Microsoft Agent Framework, exposes them over the AG-UI
protocol, and reaches the catalogue only through the existing MCP server — while staying testable
without a live model and leaving room for multi-agent workflows later.

Three forces shaped the result:

- The repository already draws a hard line between a reusable framework (`lib/`) and application code
  (`src/`), and enforces layer purity with architecture tests. A new stack has to fit that or it
  quietly becomes an exception.
- Tools are discovered at run time from the MCP server, under the *calling user's* token, because
  that is what makes each tool's permission check meaningful end to end.
- The Agent Framework packages are preview and their published documentation lags the assemblies.
  Several published API names turned out to be wrong when checked against 1.18.

## Decision

**Seven projects.** `lib/Tnosc.Lib.Agent` (pure) and `lib/Tnosc.Lib.Agent.Runtime` (the ports) in the
framework; `Agent.Domain`, `Agent.Application`, `Agent.Infrastructure.Ai`,
`Agent.Infrastructure.Mcp` and `Agent.Api` in the application, over the existing `Agent.Host`.

**An agent is validated data.** `AgentDefinition` and its value objects live in `lib/`, built through
`Create` factories returning `Result<T>`; concrete agents live in `Agent.Domain`. Behaviour lives in
middleware, never in a branch inside a definition.

**`AgentResult` is the success value carried inside the existing `Result<T>`**, not a parallel result
type. An enclosing error means the run could not happen or finish; anything the agent said —
including a tool refusing the caller — is content.

**A contract earns a place in `lib/` only if a second host would plausibly implement it differently.**
`IAgentRunner`, `IAgentFactory` and `IAgentToolProvider` qualify; `IAgentCatalog` does not and stays
in `Agent.Application`.

**Conversation isolation is mandatory**, wired through the agent builder so the isolating wrapper is
applied.

## Rationale

**Why a Domain project here when `src/mcp/` has none.** MCP is a protocol adapter over someone else's
API and owns no policy, so a Domain project there would have been empty ceremony. The agent stack
does own policy: an agent's instructions, its tool allow-list and its model bounds *are* the business
decision. By the repository's own split — Application orchestrates, Domain owns business decisions —
that content belongs in a Domain layer, and giving it one is what let the invariants become value
objects with error catalogues instead of comments.

**Why two `lib/` projects instead of one.** `AgentDefinition` is framework-free; `IAgentFactory`
returns an `AIAgent` and `IAgentToolProvider` returns an `AITool`. A single project would have to
reference `Microsoft.Agents.AI`, which `Agent.Domain` then inherits transitively — and the purity
assertion that makes definitions testable with nothing loaded could not be written at all. The
precedent for a two-project slice already exists in `Tnosc.Lib.Web` / `Tnosc.Lib.Web.Bff`.

**Why `IAgentCatalog` is excluded.** It has one shape — scan the assembly, freeze a dictionary — one
implementation, and a single consumer in its own assembly. A port with those properties is an
indirection, not a seam. Naming the test out loud ("would a second host implement this differently?")
matters more than the individual answer: it gives the next person a rule instead of a precedent to
guess at.

**Why `AgentResult` composes `Result<T>` rather than replacing it.** A parallel result type would
mean two error vocabularies in one codebase and would break every existing mapping — `ToHttp()`,
`CustomResults.Problem`, `ToToolResult()`. Composing keeps one. The harder question was where to draw
the line between failure and content, and the answer follows `Mcp.Tool`'s `ToolResult<T>`: a tool
returning 403 is the system working correctly, so reporting it as a failed run would turn a correctly
enforced permission into a 500 and stop the agent explaining what happened.

**Why `AgentRunMetadata` carries tool calls and usage.** Without it, the only evidence a tool ran is
the model's prose, and a test that pattern-matches wording breaks the first time the model rephrases
an answer. That is a flaky test dressed up as a behavioural one.

**Why tools are injected per run rather than registered.** `IHostedAgentBuilder.WithAITool` looks like
the natural home and is not: it throws on a captive dependency — a singleton agent may not hold
scoped tools — and its factory is synchronous while MCP tool discovery is asynchronous. The tool
provider must be scoped because it carries the caller's forwarded token. `ToolInjectionMiddleware`
supplies tools through the run options instead, which is also the framework's own composition idiom
(`AIAgentBuilder.Use`), matching this repository's decorator chain.

**Why conversation isolation is not optional.** `MapAGUIServer`'s own documentation states that the
AG-UI `threadId` arrives from the wire and is a chain-resume identifier, not an authorization token,
and that the session store contract carries no owner dimension. In a multi-user host that means any
caller who guesses another's `threadId` resumes their conversation. Two lines now — an isolation
provider plus attaching the store through the builder — make "in-memory today, durable later" safe by
construction. Registering an `AgentSessionStore` straight into the container looks equivalent, starts
cleanly, and silently reopens the leak.

**Why Foundry is configured rather than provisioned in the AppHost.** Adding an Azure resource would
make `dotnet run` on the AppHost require a subscription and a successful provisioning pass before any
resource starts — a poor trade in a repository where most work never touches an agent. The endpoint
is passed through from configuration instead; leave it empty and every other resource still comes up
while the agent host fails its own options validation with the missing setting named.

## Consequences

**Easier:**

- Agent definitions are unit-testable with no container, no HTTP and no model — the value objects
  need nothing loaded at all.
- Any project can invoke an agent through `IAgentRunner` without knowing Foundry, MCP or SSE exist;
  AG-UI becomes one presentation rather than the only way in.
- Multi-agent workflows are a second `IAgentFactory` implementation selected by `AgentKind`. Nothing
  downstream — runner, result, endpoints, host — changes.
- Swapping conversation storage to Microsoft Foundry replaces one call, and the isolation wrapper
  keeps applying.
- A new agent is a definition class, a name constant, a route and an endpoint. The DI scan finds it.

**Harder:**

- Seven projects for one shipped agent is a lot of structure up front, justified by the seams rather
  than by today's line count.
- Every agent run pays an MCP connection and tool listing, because the token is per-caller. Reducing
  it means caching the tool *schema* while still invoking under the per-caller connection — never
  promoting the provider to a shared singleton, which would give every caller the same authority.
- `Tnosc.Lib.Agent` may take no package, so anything needing one has to be split into
  `Tnosc.Lib.Agent.Runtime`. That constraint is deliberate and is enforced by a test.
- The Agent Framework packages are preview and must be bumped together; a version skew makes the
  `AIAgent` type identity diverge between core and hosting assemblies, which fails at run time rather
  than at restore.
- `Azure.AI.Projects` authenticates through `System.ClientModel` while Azure identities implement
  `Azure.Core.TokenCredential`, and no adapter ships in either package.
  `AzureCredentialTokenProvider` is ours to maintain until the SDKs bridge it.
