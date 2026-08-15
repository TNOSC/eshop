using System;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.FluentUI.AspNetCore.Components;
using Tnosc.EShop.Client.Web.Client.Extensions;
using Tnosc.EShop.Client.Web.Client.Infrastructure.Auth;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.Services.AddAuthorizationCore();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddSingleton<AuthenticationStateProvider, PersistentAuthenticationStateProvider>();
builder.Services.AddFluentUIComponents();
builder.Services.AddEShopApiClients(
    baseAddress: new Uri(uriString: builder.HostEnvironment.BaseAddress + "bff/"));

await builder.Build().RunAsync();
