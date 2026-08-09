// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC). All rights reserved.
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Tnosc.Lib.Host.Extensions;
using Tnosc.Lib.Host.Middleware;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddUserContext();
builder.Services.AddGlobalExceptionHandling();

WebApplication app = builder.Build();

app.MapDefaultEndpoints();

app.UseHttpsRedirection();

app.UseExceptionHandler();
app.UseMiddleware<RequestContextMiddleware>();

await app.RunAsync();
