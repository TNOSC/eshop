using System;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.FluentUI.AspNetCore.Components;
using Tnosc.EShop.Client.Web.Client.Extensions;
using Tnosc.EShop.Client.Web.Client.Infrastructure.Auth;
using Tnosc.Lib.Web.Extensions;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.Services.AddAuthorizationCore().AddPermissionAuthorization();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddSingleton<AuthenticationStateProvider, PersistentAuthenticationStateProvider>();
builder.Services.AddFluentUIComponents();
// Both addresses are this app's own BFF: it forwards "/bff/api/…" to the eShop API and
// "/bff/agents/…" to the agent host, so from the browser the two services share one origin.
builder.Services.AddEShopApiClients(
    baseAddress: new Uri(uriString: builder.HostEnvironment.BaseAddress + "bff/"),
    agentBaseAddress: new Uri(uriString: builder.HostEnvironment.BaseAddress + "bff/"));

await builder.Build().RunAsync();
