// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using Microsoft.Agents.AI.Hosting;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Tnosc.EShop.Agent.Api.Extensions;
using Tnosc.EShop.Agent.Application.Extensions;
using Tnosc.EShop.Agent.Host.Authentication;
using Tnosc.EShop.Agent.Host.Extensions;
using Tnosc.EShop.Agent.Infrastructure.Ai.Extensions;
using Tnosc.EShop.Agent.Infrastructure.Mcp.Extensions;
using Tnosc.Lib.Api.Extensions;
using Tnosc.Lib.Host.Extensions;
using Tnosc.Lib.Host.Middleware;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args: args);

builder.AddServiceDefaults();
builder.AddKeycloakAuthentication();
builder.AddTokenForwarding();

builder.Services.AddUserContext();
builder.Services.AddAgentApplication();
builder.Services.AddAgentInfrastructureAi(configuration: builder.Configuration);
builder.Services.AddAgentInfrastructureMcp(configuration: builder.Configuration)
    .AddHttpMessageHandler<TokenForwarder>();

builder.Services.AddAgentEndpoints();
builder.Services.AddAGUIServer();

// Must come before AddHostedAgents: the isolation provider is what makes each agent's session store
// key conversations by caller as well as by thread id. Without it the store still works, and any
// caller presenting another caller's thread id reads that caller's conversation.
builder.Services.UseClaimsBasedAgentIsolation(
    new ClaimsIdentityAgentIsolationKeyProviderOptions
    {
        ClaimType = HostedAgentExtensions.IsolationClaimType,
    });

builder.AddHostedAgents();

WebApplication app = builder.Build();

app.MapDefaultEndpoints();

app.UseAuthentication();
app.UseAuthorization();

// After UseAuthorization deliberately: this middleware opens a logging scope carrying the caller's
// id, which is still null anywhere earlier in the pipeline.
app.UseMiddleware<RequestContextMiddleware>();

app.MapEndpoints();

await app.RunAsync();
