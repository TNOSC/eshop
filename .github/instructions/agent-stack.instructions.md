---
description: "The AI shopping-assistant agent stack: domain, runtime, infrastructure and API"
applyTo: "src/agent/**"
---

# Tnosc.EShop.Agent.*

The agent host: instantiates agents with the **Microsoft Agent Framework**, exposes them over
**AG-UI**, and gets every tool from `src/mcp/` rather than defining any of its own.

```
lib/Tnosc.Lib.Agent/          definitions, value objects, AgentResult. NO package reference at all.
lib/Tnosc.Lib.Agent.Runtime/  IAgentRunner · IAgentFactory · IAgentToolProvider
src/agent/
  Agent.Domain/               AgentNames + the concrete agents. No package reference.
  Agent.Application/          IAgentCatalog + AgentCatalog, AgentRunner, the DI scan
  Agent.Infrastructure.Ai/    Foundry client, ChatClientAgentFactory, agent middleware
  Agent.Infrastructure.Mcp/   McpAgentToolProvider — the only type that knows MCP exists
  Agent.Api/                  AG-UI endpoints, one IApiEndpoint per agent
  Agent.Host/                 composition root, auth, token forwarding
```

`Domain ← Application ← {Infrastructure.Ai, Infrastructure.Mcp, Api} ← Host`, enforced by
`AgentLayerDependencyTests`.

## An agent is data, not code

A definition is a validated value object — name, instructions, tool allow-list, model bounds, output
contract — built through `AgentDefinition.Create` and rejected at startup if malformed. **Nothing in
a definition branches at run time.** Behaviour that applies to every run belongs in middleware
(`Infrastructure.Ai/Middleware/`), which is this stack's version of the decorator chain wrapped
around command handlers.

Adding an agent: a class in `Agent.Domain/Agents/<Name>/` implementing `IAgentDefinitionProvider`, a
constant in `AgentNames`, a route in `AgentRoutes`, and an `IApiEndpoint`. The DI scan finds it; there
is no registration list.

## What earns a place in `lib/`

**Would a second host plausibly implement this differently?** `IAgentFactory` yes — a different model
provider. `IAgentToolProvider` yes — a different tool source. `IAgentRunner` yes, and it exists
precisely so projects that cannot see `Agent.Application` can still invoke an agent.

`IAgentCatalog` **no**: one shape, one implementation, consumed inside its own assembly. It lives in
`Agent.Application`, and is an interface only so the runner is testable.

`Tnosc.Lib.Agent` takes **no package**. That is what lets `Agent.Domain` reference it and still pass
the purity assertion — anything naming an Agent Framework type goes in `Tnosc.Lib.Agent.Runtime`
instead. Both are `lib/` projects, so `CS1591` is a build error: XML-doc every public member.

## `AgentResult` is a value inside `Result<T>`, not a parallel result type

One error vocabulary, so `ToHttp()`, `CustomResults.Problem` and `ToToolResult()` keep working.

| | |
|---|---|
| **Outer `Result` error** | The run could not happen or finish: unknown agent, provider unreachable, timeout, cancelled, output would not bind. See `AgentErrors`. |
| **Inside a successful `AgentResult`** | The agent ran and answered. **A tool refusing the caller, or the agent declining, is content** — reporting it as a failure would turn a correctly enforced permission into a 500. |

`AgentRunMetadata.ToolCalls` is what makes a run assertable without reading the model's prose. Assert
on it, never on wording.

## Tools

Only from MCP, only through `IAgentToolProvider`, filtered by the definition's allow-list, resolved
**per run** with the caller's forwarded token. That last part is why the provider is scoped and why
`ToolInjectionMiddleware` exists: `IHostedAgentBuilder.WithAITool` cannot express it — it throws on a
captive dependency (a singleton agent may not hold scoped tools) and its factory is synchronous while
tool discovery is not.

A tool-discovery failure must surface. An empty tool set on error produces an agent answering from
memory, which reads as a hallucination rather than the outage it is.

## Things that will bite

- **The AG-UI extension is `MapAGUIServer`, not `MapAGUI`.** The published docs and the older API
  reference disagree; 1.18's assembly ships `MapAGUIServer`. Verified — do not "correct" it.
- **Name the arguments.** `MapAGUIServer(agentName:, pattern:)` takes two strings, agent name first —
  the opposite order from the sibling overload taking an `AIAgent`. Positional arguments compile,
  start, and 404.
- **Conversation isolation is mandatory.** The AG-UI `threadId` arrives from the wire and is a
  resume identifier, never a credential. `UseClaimsBasedAgentIsolation` must be registered, and the
  session store must be attached **through the agent builder** (`.WithInMemorySessionStore()`) —
  only that path applies the isolating wrapper. Putting an `AgentSessionStore` straight into the
  container looks equivalent, starts cleanly, and lets any caller resume anyone's conversation.
- **`AgentResponse.Text` can be null** when a run produced only tool traffic. `AgentRunner` maps it
  to an empty answer.
- **Foundry is configured, not provisioned.** Set `Foundry:Endpoint` in the AppHost's user secrets.
  Adding an Azure resource to the AppHost would make `dotnet run` require a subscription before any
  resource starts.
- **A tool name left to derive from the C# method name is not usable.** With no explicit `Name`, the
  MCP SDK snake_cases the method name including the `Async` suffix — `ListProductsAsync` becomes
  `list_products_async`, not `ListProducts`. Every tool an agent allow-lists must be named explicitly
  via `Server.Shared.Catalog.McpToolNames` (or the equivalent for another context), set on both
  `[McpServerTool(Name = ...)]` in `Mcp.Tool` and the agent's `ToolAllowList` — never a literal on
  either side. A mismatch here is not a build error; it is an agent that silently answers with zero
  tools, which reads like a bad prompt rather than a missing capability.

## Authorization

`.RequireAuthorization()` on the AG-UI endpoint — any authenticated caller. Fine-grained enforcement
happens where the tools are: the forwarded token is checked per MCP tool, so a `customer` asking the
agent to create a product gets a 403 from the MCP server and the agent reports it as content.
`Permissions.Agent.*` is a deliberate follow-up, not an oversight — do not add it speculatively.

## Commands

```bash
dotnet build Tnosc.EShop.slnx
dotnet test tests/agent/Tnosc.EShop.Agent.Tests.Unit
dotnet test tests/server/Tnosc.EShop.Server.Tests.Architecture   # includes AgentLayerDependencyTests
dotnet run --project aspire/Tnosc.EShop.AppHost
```
