// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.ServiceDiscovery;
using ModelContextProtocol.Client;
using Tnosc.EShop.Agent.Infrastructure.Mcp.Options;
using Tnosc.Lib.Agent.Definitions;
using Tnosc.Lib.Agent.Runtime;

namespace Tnosc.EShop.Agent.Infrastructure.Mcp;

/// <summary>
/// Supplies an agent's tools from the Model Context Protocol server, filtered by its allow-list.
/// </summary>
/// <remarks>
/// <para>
/// This is the only type in the solution that knows the agent host talks to a tool server at all.
/// Everything upstream sees tools, not a protocol.
/// </para>
/// <para>
/// It is scoped, and connects per run, because the outgoing request carries the calling user's own
/// token. That is what makes the tool server's per-tool authorization mean something: the agent has
/// exactly the reach of whoever is talking to it, never more. The cost is a connection and a tool
/// listing per run — a real cost, and the right one to pay. If it ever needs reducing, cache the tool
/// <em>schema</em> across callers while still invoking under the per-caller connection; do not
/// promote this to a shared singleton, which would silently give every caller the same authority.
/// </para>
/// </remarks>
internal sealed class McpAgentToolProvider(
    IHttpClientFactory httpClientFactory,
    McpServerOptions options,
    ServiceEndpointResolver serviceEndpointResolver,
    ILoggerFactory loggerFactory,
    ILogger<McpAgentToolProvider> logger) : IAgentToolProvider, IAsyncDisposable
{
    /// <summary>
    /// The name of the configured <see cref="HttpClient"/> that carries the caller's token.
    /// </summary>
    public const string HttpClientName = "mcp-tools";

    private HttpClientTransport? _transport;
    private McpClient? _client;

    /// <inheritdoc />
    public async ValueTask<IReadOnlyList<AITool>> GetToolsAsync(
        AgentDefinition definition,
        CancellationToken cancellationToken = default)
    {
        HttpClient httpClient = httpClientFactory.CreateClient(name: HttpClientName);

        Uri endpoint = await ResolveEndpointAsync(cancellationToken: cancellationToken);

        HttpClientTransportOptions transportOptions = new()
        {
            Endpoint = endpoint,
            Name = options.ServiceName,
        };

        // The transport and client are kept alive as instance fields rather than disposed at the
        // end of this method: the AITools returned below wrap this same McpClient and are invoked
        // later, mid-run, once the model decides to call one. Disposing eagerly here tears down the
        // session before that invocation happens, so a real tool call fails instantly with a
        // TaskCanceledException instead of ever reaching the server. This type is registered scoped
        // and resolved from the per-run scope in ToolInjectionMiddleware, so the container disposes
        // it — and therefore the transport/client below — only when that run's scope ends.
        //
        // ownsHttpClient stays false: the client came from the factory, which owns its lifetime and
        // the delegating handler chain that puts the caller's token on every request.
        HttpClientTransport transport = new(
            transportOptions: transportOptions,
            httpClient: httpClient,
            loggerFactory: loggerFactory,
            ownsHttpClient: false);
        _transport = transport;

        McpClient client = await McpClient.CreateAsync(
            clientTransport: transport,
            clientOptions: null,
            loggerFactory: loggerFactory,
            cancellationToken: cancellationToken);
        _client = client;

        IList<McpClientTool> available = await client.ListToolsAsync(cancellationToken: cancellationToken);

        return Filter(definition: definition, available: available, logger: logger);
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_client is not null)
        {
            await _client.DisposeAsync();
        }

        if (_transport is not null)
        {
            await _transport.DisposeAsync();
        }
    }

    /// <summary>
    /// Resolves the tool server's logical service name to a concrete, absolute HTTP(S) endpoint.
    /// </summary>
    /// <param name="cancellationToken">A token to observe while resolving.</param>
    /// <returns>The resolved endpoint, with <see cref="McpServerOptions.Path"/> appended.</returns>
    /// <remarks>
    /// <see cref="HttpClientTransportOptions.Endpoint"/> rejects any scheme other than
    /// <c>http</c>/<c>https</c>, so Aspire's <c>https+http://</c> composite scheme cannot be handed to
    /// it directly the way it can to an <see cref="HttpClient"/>'s <c>BaseAddress</c> — the transport
    /// validates the scheme eagerly, before the service-discovery handler in the HTTP pipeline ever
    /// gets a chance to rewrite it. Resolving through <see cref="ServiceEndpointResolver"/> up front
    /// yields the same "prefer https, fall back to http" behaviour, just resolved before the value
    /// reaches the transport instead of during the request.
    /// </remarks>
    private async ValueTask<Uri> ResolveEndpointAsync(CancellationToken cancellationToken)
    {
        ServiceEndpointSource source = await serviceEndpointResolver.GetEndpointsAsync(
            serviceName: $"https+http://{options.ServiceName}",
            cancellationToken: cancellationToken);

        if (source.Endpoints.Count == 0 || source.Endpoints[0].EndPoint is not UriEndPoint resolved)
        {
            throw new InvalidOperationException(
                message: $"No endpoint could be resolved for the MCP tool server '{options.ServiceName}'.");
        }

        return new Uri(baseUri: resolved.Uri, relativeUri: options.Path);
    }

    /// <summary>
    /// Narrows the server's tools to those the agent's allow-list permits.
    /// </summary>
    /// <param name="definition">The agent whose allow-list applies.</param>
    /// <param name="available">Every tool the server exposes.</param>
    /// <returns>The permitted tools.</returns>
    /// <remarks>
    /// A pure function over the server's answer — static, so the filtering rule is testable with no
    /// <see cref="McpAgentToolProvider"/> instance to construct, and therefore no server.
    /// </remarks>
    internal static IReadOnlyList<AITool> Filter(
        AgentDefinition definition,
        IEnumerable<McpClientTool> available,
        ILogger logger)
    {
        List<McpClientTool> tools = [.. available];

        if (definition.Tools.IsUnrestricted)
        {
            return [.. tools];
        }

        HashSet<string> exposed = [.. tools.Select(selector: static tool => tool.Name)];

        foreach (string permitted in definition.Tools.Names)
        {
            if (!exposed.Contains(item: permitted))
            {
                // Not fatal: a tool may legitimately be retired or renamed ahead of the agents that
                // name it. Loud, though — the alternative is an agent quietly losing a capability and
                // answering from memory instead, which looks like a hallucination rather than a
                // configuration drift.
                logger.LogWarning(
                    message: "Agent {AgentName} allows tool {ToolName}, which the server does not expose",
                    definition.Name.Value,
                    permitted);
            }
        }

        return [.. tools.Where(predicate: tool => definition.Tools.Permits(toolName: tool.Name))];
    }
}
