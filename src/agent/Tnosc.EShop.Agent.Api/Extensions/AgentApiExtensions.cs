// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using Microsoft.Extensions.DependencyInjection;
using Tnosc.Lib.Api.Extensions;

namespace Tnosc.EShop.Agent.Api.Extensions;

/// <summary>
/// Provides extension methods to register the agent host's endpoints.
/// </summary>
public static class AgentApiExtensions
{
    /// <summary>
    /// Registers every agent endpoint in this assembly.
    /// </summary>
    /// <param name="services">The service collection to register the endpoints with.</param>
    /// <returns>The updated service collection.</returns>
    public static IServiceCollection AddAgentEndpoints(this IServiceCollection services) =>
        services.AddApiEndpoints(assembly: AssemblyReference.Assembly);
}
