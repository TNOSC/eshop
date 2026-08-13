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
using Tnosc.EShop.Server.Host.OpenApi;
using Tnosc.EShop.Server.Infrastructure.External.Extensions;
using Tnosc.EShop.Server.Infrastructure.Persistence.Extensions;
using Tnosc.Lib.Api.Extensions;
using Tnosc.Lib.Host.Extensions;
using Tnosc.Lib.Host.Middleware;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args: args);

builder.AddServiceDefaults();
builder.AddKeycloakAuthentication();

builder.Services.AddUserContext();
builder.Services.AddGlobalExceptionHandling();

// The L2 registrations must precede AddHybridCache() — otherwise HybridCache resolves with no
// IDistributedCache and stays L1-only, silently losing the shared cache every [Cacheable] query
// (not only Basket's) is meant to get. AddRedisClient registers the IConnectionMultiplexer that
// Server.Infrastructure.External's Redis basket store consumes.
builder.AddRedisDistributedCache(connectionName: "cache");
builder.AddRedisClient(connectionName: "cache");
builder.Services.AddHybridCache();
builder.Services.AddInfrastructureExternal(configuration: builder.Configuration);

builder.Services.AddApiEndpoints();
builder.Services.AddApplication();
builder.AddInfrastructurePersistence();


WebApplication app = builder.Build();

app.UseHttpsRedirection();
app.MapDefaultEndpoints();

if (app.Environment.IsDevelopment())
{
    app.AddOpenApiWithAuthSupport();
}
app.UseExceptionHandler();
app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<RequestContextMiddleware>();
app.MapEndpoints();

await app.RunAsync();
