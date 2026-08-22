using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.FluentUI.AspNetCore.Components;
using Tnosc.EShop.Client.Web.Authentication;
using Tnosc.EShop.Client.Web.Client.Extensions;
using Tnosc.EShop.Client.Web.Client.Infrastructure.Api;
using Tnosc.EShop.Client.Web.Components;
using Tnosc.EShop.Client.Web.Extensions;
using Tnosc.Lib.Web.Extensions;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddEShopBffAuthentication();

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents();

// Interactive Server rendering runs FluentUI components on the server, so it needs its own HttpClient.
builder.Services.AddHttpClient();
builder.Services.AddAuthorization().AddPermissionAuthorization();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<AuthenticationStateProvider, PersistingRevalidatingAuthenticationStateProvider>();
builder.Services.AddFluentUIComponents();

builder.Services.AddHttpContextAccessor();
builder.Services.AddTransient<ServerAccessTokenHandler>();

#pragma warning disable S1075 // Not a hardcoded endpoint — "eshop-host" is a service-discovery name resolved by AddServiceDefaults/AddServiceDiscovery, not a literal address.
builder.Services.AddEShopApiClients(
    baseAddress: new Uri(uriString: "https+http://eshop-host/"),
    agentBaseAddress: new Uri(uriString: "https+http://eshop-agent/"),
    configure: static clientBuilder => clientBuilder.AddHttpMessageHandler<ServerAccessTokenHandler>());

// The BFF proxy's own downstream client — deliberately separate from the typed API clients above so
// ServerAccessTokenHandler (which sets Authorization from the ambient HttpContext) is never attached
// to it; the proxy sets Authorization itself, from the token it read directly off the request.
builder.Services.AddHttpClient(
    name: ApiClientNames.Downstream,
    configureClient: static client => client.BaseAddress = new Uri(uriString: "https+http://eshop-host/"));

// The proxy's second downstream, the agent host, on the same terms and for the same reason.
builder.Services.AddHttpClient(
    name: ApiClientNames.AgentDownstream,
    configureClient: static client => client.BaseAddress = new Uri(uriString: "https+http://eshop-agent/"));
#pragma warning restore S1075

// AddServiceDefaults applies AddStandardResilienceHandler to every HttpClient in this host, and its
// defaults are wrong for a streamed conversation in both directions: the 10-second attempt timeout
// cuts off a reply that is still arriving, and retrying the POST that starts a run would run the agent
// a second time. Both hops to the agent host therefore opt out, leaving HttpClient.Timeout — set on
// the typed client in AddEShopApiClients — as the bound that applies.
#pragma warning disable EXTEXP0001 // Experimental, but the only supported way to opt a single client out of the resilience handler ConfigureHttpClientDefaults added to every client in this host.
builder.Services.AddHttpClient(name: ApiClientNames.AgentDownstream).RemoveAllResilienceHandlers();
builder.Services.AddHttpClient(name: ApiClientNames.Agent).RemoveAllResilienceHandlers();
#pragma warning restore EXTEXP0001

WebApplication app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();
app.MapDefaultEndpoints();

app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapBffEndpoints();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(Tnosc.EShop.Client.Web.Client._Imports).Assembly);

await app.RunAsync();
