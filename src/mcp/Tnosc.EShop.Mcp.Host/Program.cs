using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Tnosc.EShop.Mcp.Application.Extensions;
using Tnosc.EShop.Mcp.Host.Extensions;
using Tnosc.EShop.Mcp.Infrastructure.External.Extensions;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddKeycloakAuthentication();

builder.Services.AddMcpApplication();
builder.Services.AddMcpInfrastructureExternal();

builder.Services.AddMcpServer()
    .WithHttpTransport()
    .WithToolsFromAssembly(toolAssembly: Tnosc.EShop.Mcp.Tool.AssemblyReference.Assembly)
    .AddAuthorizationFilters();

WebApplication app = builder.Build();

app.MapDefaultEndpoints();

app.UseAuthentication();
app.UseAuthorization();

app.MapMcp("/mcp").RequireAuthorization();

await app.RunAsync();
