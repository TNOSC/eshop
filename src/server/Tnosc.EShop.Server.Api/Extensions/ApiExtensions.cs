// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using Microsoft.Extensions.DependencyInjection;
using Tnosc.Lib.Api.Extensions;

namespace Tnosc.EShop.Server.Api.Extensions;

/// <summary>
/// Provides extension methods to register API endpoints for the EShop server application.
/// </summary>
public static class ApiExtensions
{
    /// <summary>
    /// Registers API endpoints for the EShop server application using the specified assembly reference.
    /// </summary>
    /// <param name="services"> The service collection to register the API endpoints with.</param>
    /// <returns> The updated service collection. </returns>
    public static IServiceCollection AddApiEndpoints(this IServiceCollection services) =>
        services.AddApiEndpoints(assembly: AssemblyReference.Assembly);
}
