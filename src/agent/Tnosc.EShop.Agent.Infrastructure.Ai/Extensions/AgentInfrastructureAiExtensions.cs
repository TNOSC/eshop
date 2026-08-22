// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using Azure.AI.Projects;
using Azure.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Tnosc.EShop.Agent.Infrastructure.Ai.Options;
using Tnosc.Lib.Agent.Runtime;

namespace Tnosc.EShop.Agent.Infrastructure.Ai.Extensions;

/// <summary>
/// Registers the Microsoft Foundry model provider and the agent factory over it.
/// </summary>
public static class AgentInfrastructureAiExtensions
{
    /// <summary>
    /// Registers the Foundry project client and the factory that builds agents against it.
    /// </summary>
    /// <param name="services">The service collection to add to.</param>
    /// <param name="configuration">Configuration to bind <see cref="FoundryOptions"/> from.</param>
    /// <returns>The same service collection, so calls can be chained.</returns>
    public static IServiceCollection AddAgentInfrastructureAi(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOptions<FoundryOptions>()
            .Bind(config: configuration.GetSection(key: FoundryOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddSingleton(implementationFactory: static resolve =>
            resolve.GetRequiredService<IOptions<FoundryOptions>>().Value);

        services.AddSingleton(implementationFactory: static resolve =>
        {
            FoundryOptions options = resolve.GetRequiredService<FoundryOptions>();
            IHostEnvironment environment = resolve.GetRequiredService<IHostEnvironment>();

            // Ambient Azure identity rather than a key, so nothing secret has to be configured,
            // rotated, or kept out of a commit. Managed Identity is excluded in Development only: a
            // local dev box has no IMDS endpoint, and ManagedIdentityCredential's failure there is a
            // hard AuthenticationFailedException rather than the "unavailable, try the next one" kind
            // — it stops DefaultAzureCredential's chain before AzureCliCredential ever gets a turn, so
            // a developer's own `az login` session is never tried. A real deployment (Container Apps,
            // App Service, …) keeps Managed Identity available.
            DefaultAzureCredential credential = new(new DefaultAzureCredentialOptions
            {
                ExcludeManagedIdentityCredential = environment.IsDevelopment(),
            });

            return new AIProjectClient(
                endpoint: new Uri(uriString: options.Endpoint),
                tokenProvider: new AzureCredentialTokenProvider(
                    credential: credential,
                    defaultScope: AzureCredentialTokenProvider.DefaultAiScope));
        });

        // Singleton: AG-UI resolves the named agent from the root provider at endpoint-mapping time
        // (see AGUIEndpointRouteBuilderExtensions.MapAGUIServer), so the agent — and therefore the
        // factory that builds it — cannot be scoped. The caller-specific tool provider is instead
        // resolved per run from a fresh scope inside ToolInjectionMiddleware.
        services.AddSingleton<IAgentFactory, ChatClientAgentFactory>();

        return services;
    }
}
