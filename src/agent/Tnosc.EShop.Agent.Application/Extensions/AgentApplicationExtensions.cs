// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using Microsoft.Extensions.DependencyInjection;
using Tnosc.EShop.Agent.Application.Catalog;
using Tnosc.EShop.Agent.Application.Running;
using Tnosc.Lib.Agent.Definitions;
using Tnosc.Lib.Agent.Runtime;

namespace Tnosc.EShop.Agent.Application.Extensions;

/// <summary>
/// Registers the agent application layer.
/// </summary>
public static class AgentApplicationExtensions
{
    /// <summary>
    /// Discovers every agent this host serves and registers the catalogue and runner over them.
    /// </summary>
    /// <param name="services">The service collection to add to.</param>
    /// <returns>The same service collection, so calls can be chained.</returns>
    /// <remarks>
    /// Agents are discovered by scanning rather than listed here, so adding one is a matter of
    /// declaring it — there is no registration list to forget to update, which is the same choice
    /// the API endpoints in this solution already make.
    /// </remarks>
    public static IServiceCollection AddAgentApplication(this IServiceCollection services) =>
        services
            .AddAgentDefinitions()
            .AddSingleton<IAgentCatalog, AgentCatalog>()
            .AddScoped<IAgentRunner, AgentRunner>();

    private static IServiceCollection AddAgentDefinitions(this IServiceCollection services)
    {
        services.Scan(action: selector => selector
            .FromAssemblies(assemblies: Domain.AssemblyReference.Assembly)
            .AddClasses(
                action: classes => classes.AssignableTo<IAgentDefinitionProvider>(),
                publicOnly: false)
            .As<IAgentDefinitionProvider>()
            .WithSingletonLifetime());

        return services;
    }
}
