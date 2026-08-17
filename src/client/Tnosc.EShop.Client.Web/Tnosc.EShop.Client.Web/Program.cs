using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.FluentUI.AspNetCore.Components;
using Tnosc.EShop.Client.Web.Authentication;
using Tnosc.EShop.Client.Web.Client.Extensions;
using Tnosc.EShop.Client.Web.Client.Infrastructure.Auth.Authorization;
using Tnosc.EShop.Client.Web.Client.Infrastructure.Api;
using Tnosc.EShop.Client.Web.Components;
using Tnosc.EShop.Client.Web.Extensions;

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
    configure: static clientBuilder => clientBuilder.AddHttpMessageHandler<ServerAccessTokenHandler>());

// The BFF proxy's own downstream client — deliberately separate from the typed API clients above so
// ServerAccessTokenHandler (which sets Authorization from the ambient HttpContext) is never attached
// to it; the proxy sets Authorization itself, from the token it read directly off the request.
builder.Services.AddHttpClient(
    name: ApiClientNames.Downstream,
    configureClient: static client => client.BaseAddress = new Uri(uriString: "https+http://eshop-host/"));
#pragma warning restore S1075

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
