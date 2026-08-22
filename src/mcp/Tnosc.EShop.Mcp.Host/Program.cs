using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Tnosc.EShop.Mcp.Application.Extensions;
using Tnosc.EShop.Mcp.Host.Authentication;
using Tnosc.EShop.Mcp.Host.Extensions;
using Tnosc.EShop.Mcp.Infrastructure.External.Extensions;
using Tnosc.Lib.Host.Extensions;
using Tnosc.Lib.Host.Middleware;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args: args);

builder.AddServiceDefaults();
builder.AddKeycloakAuthentication();
builder.AddTokenForwarding();

builder.Services.AddUserContext();
builder.Services.AddMcpApplication();
builder.Services.AddMcpInfrastructureExternal()
    .AddHttpMessageHandler<TokenForwarder>();

builder.Services.AddMcpServer()
    .WithHttpTransport()
    .WithToolsFromAssembly(toolAssembly: Tnosc.EShop.Mcp.Tool.AssemblyReference.Assembly)
    .AddAuthorizationFilters();

WebApplication app = builder.Build();

app.MapDefaultEndpoints();

app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<RequestContextMiddleware>();

app.MapMcp(pattern: "/mcp").RequireAuthorization();

await app.RunAsync();
