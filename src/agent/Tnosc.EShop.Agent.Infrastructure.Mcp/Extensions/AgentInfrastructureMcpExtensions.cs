// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Tnosc.EShop.Agent.Infrastructure.Mcp.Options;
using Tnosc.Lib.Agent.Runtime;

namespace Tnosc.EShop.Agent.Infrastructure.Mcp.Extensions;

/// <summary>
/// Registers the Model Context Protocol tool source.
/// </summary>
public static class AgentInfrastructureMcpExtensions
{
    /// <summary>
    /// Registers the tool provider and the HTTP client it reaches the tool server through.
    /// </summary>
    /// <param name="services">The service collection to add to.</param>
    /// <param name="configuration">Configuration to bind <see cref="McpServerOptions"/> from.</param>
    /// <returns>
    /// The builder for the tool server's HTTP client, so the composition root can attach the handler
    /// that forwards the caller's token — which this layer must not know about.
    /// </returns>
    public static IHttpClientBuilder AddAgentInfrastructureMcp(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOptions<McpServerOptions>()
            .Bind(config: configuration.GetSection(key: McpServerOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddSingleton(implementationFactory: static resolve =>
            resolve.GetRequiredService<IOptions<McpServerOptions>>().Value);

        services.AddScoped<IAgentToolProvider, McpAgentToolProvider>();

        IHttpClientBuilder builder = services.AddHttpClient(name: McpAgentToolProvider.HttpClientName);
        builder.AddStandardResilienceHandler();

        return builder;
    }
}
