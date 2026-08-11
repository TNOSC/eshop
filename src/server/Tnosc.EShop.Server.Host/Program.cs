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

builder.Services.AddApiEndpoints();
builder.Services.AddApplication();
builder.AddInfrastructurePersistence();


WebApplication app = builder.Build();

app.MapDefaultEndpoints();

app.UseHttpsRedirection();

app.UseExceptionHandler();
app.UseMiddleware<RequestContextMiddleware>();
app.MapEndpoints();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(configureOptions: options => options.WithTitle(title: "Tnosc EShop API"));
}

await app.RunAsync();
