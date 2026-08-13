// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Scalar.AspNetCore;
using Tnosc.EShop.Server.Api.Extensions;
using Tnosc.EShop.Server.Application.Extensions;
using Tnosc.EShop.Server.Host.Extensions;
using Tnosc.EShop.Server.Infrastructure.Persistence.Extensions;
using Tnosc.Lib.Api.Extensions;
using Tnosc.Lib.Host.Extensions;
using Tnosc.Lib.Host.Middleware;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args: args);

builder.AddServiceDefaults();

builder.Services.AddUserContext();
builder.Services.AddGlobalExceptionHandling();
builder.Services.AddHybridCache();
builder.Services.AddOpenApi();

builder.AddKeycloakAuthentication();

builder.Services.AddApiEndpoints();
builder.Services.AddApplication();
builder.AddInfrastructurePersistence();


WebApplication app = builder.Build();

app.MapDefaultEndpoints();

app.UseHttpsRedirection();

app.UseExceptionHandler();
app.UseAuthentication();
app.UseAuthorization();

// After UseAuthorization, deliberately: the middleware puts IUserContext.UserId into its logging
// scope, and that is null for every request if it runs before the principal has been established.
// The knock-on is intended too — UnitOfWork's audit columns stop reading "system" and start carrying
// the Keycloak subject.
app.UseMiddleware<RequestContextMiddleware>();

app.MapEndpoints();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(configureOptions: options => options.WithTitle(title: "Tnosc EShop API"));
}

await app.RunAsync();

/// <summary>
/// Exposes the top-level program as a named type so <c>WebApplicationFactory&lt;Program&gt;</c> can
/// bind to it from the integration test suite. A top-level program's generated class is internal, and
/// the factory needs a nameable entry point.
/// </summary>
public partial class Program
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Program"/> class.
    /// </summary>
    /// <remarks>
    /// Never called — the host is the generated top-level entry point. Declared only so the partial
    /// class has an accessible constructor for the test factory's generic constraint.
    /// </remarks>
    protected Program()
    {
    }
}
