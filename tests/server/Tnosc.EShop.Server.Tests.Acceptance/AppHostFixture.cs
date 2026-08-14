// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Testing;
using Microsoft.Extensions.DependencyInjection;
using Tnosc.EShop.Server.Tests.Acceptance.Contracts;

namespace Tnosc.EShop.Server.Tests.Acceptance;

/// <summary>
/// Boots the real AppHost — Postgres, Redis, Keycloak and the API — once for the whole collection.
/// </summary>
/// <remarks>
/// <para>
/// This is the one suite where the distributed application <em>is</em> the unit under test, which is
/// why it uses <see cref="DistributedApplicationTestingBuilder"/> rather than the Testcontainers
/// harness the integration suite uses. Booting all four resources takes tens of seconds, so it happens
/// once per collection and every test shares it.
/// </para>
/// <para>
/// <strong>Docker must be running, and host port 8080 must be free.</strong> The AppHost pins
/// Keycloak to 8080 so a human can reach the admin console at a stable address; if that port is taken
/// — most often by a <c>dotnet run --project aspire/Tnosc.EShop.AppHost</c> left running — the
/// Keycloak container never starts and every test here fails waiting for it.
/// </para>
/// <para>
/// <strong>Seeding is switched on explicitly rather than inherited.</strong> The journeys buy
/// <see cref="AcceptanceRoutes.FeaturedSku"/>, and no endpoint can create a product from nothing —
/// creating one needs a brand and a category, and neither has a write endpoint. The seeded catalogue
/// is therefore a precondition of the journey, stated here rather than left to whichever environment
/// the test host happened to inherit.
/// </para>
/// </remarks>
public sealed class AppHostFixture : IAsyncLifetime
{
    /// <summary>The AppHost resource name of the API project.</summary>
    private const string ApiResourceName = "eshop-host";

    /// <summary>The AppHost resource name of the identity provider.</summary>
    private const string KeycloakResourceName = "keycloak";

    /// <summary>
    /// The API's endpoint the journeys use.
    /// </summary>
    /// <remarks>
    /// It has to be the HTTPS one. <c>Program.cs</c> calls <c>UseHttpsRedirection()</c>, and Aspire
    /// gives the project both endpoints — so a request to the HTTP one is answered with a 307 to HTTPS,
    /// and <see cref="HttpClient"/> strips the <c>Authorization</c> header when it follows a redirect
    /// that changes scheme. Every authenticated call would then arrive anonymous and come back 401 with
    /// a bare <c>WWW-Authenticate: Bearer</c> — a failure that reads like a token problem and is not
    /// one. This needs the ASP.NET Core development certificate to be trusted
    /// (<c>dotnet dev-certs https --trust</c>).
    /// </remarks>
    private const string ApiEndpointName = "https";

    /// <summary>
    /// Keycloak's endpoint. HTTP, matching the AppHost's fixed 8080 and the API's own service-discovery
    /// reference, so the issuer in a minted token and the issuer the API discovered are the same string.
    /// </summary>
    private const string KeycloakEndpointName = "http";

    private const string TokenPath = "/realms/eshop/protocol/openid-connect/token";

    private static readonly TimeSpan StartupTimeout = TimeSpan.FromMinutes(value: 10);

    private static readonly JsonSerializerOptions JsonOptions =
        new(defaults: JsonSerializerDefaults.Web);

    private readonly List<HttpClient> _clients = [];

    private DistributedApplication? _application;
    private Uri? _keycloakBaseAddress;

    /// <inheritdoc />
    public async Task InitializeAsync()
    {
        IDistributedApplicationTestingBuilder builder =
            await DistributedApplicationTestingBuilder.CreateAsync<Projects.Tnosc_EShop_AppHost>();

        ProjectResource api = builder.Resources
            .OfType<ProjectResource>()
            .Single(predicate: static resource =>
                string.Equals(a: resource.Name, b: ApiResourceName, comparisonType: StringComparison.Ordinal));

        builder.CreateResourceBuilder(resource: api)
            .WithEnvironment(name: "ASPNETCORE_ENVIRONMENT", value: "Development")
            .WithEnvironment(name: "Persistence__ApplyMigrationsOnStartup", value: "true")
            .WithEnvironment(name: "Seed__Enabled", value: "true");

        _application = await builder.BuildAsync();
        await _application.StartAsync();

        ResourceNotificationService notifications =
            _application.Services.GetRequiredService<ResourceNotificationService>();

        await notifications
            .WaitForResourceAsync(resourceName: ApiResourceName, targetState: KnownResourceStates.Running)
            .WaitAsync(timeout: StartupTimeout);

        // The very address service discovery hands the API, so a token minted here carries the same
        // issuer the API's JWT bearer handler discovered. Composing "http://localhost:8080" by hand
        // would risk a localhost/127.0.0.1 mismatch, which surfaces as a 401 that reads like a
        // credentials problem rather than an issuer one.
        _keycloakBaseAddress = _application.GetEndpoint(
            resourceName: KeycloakResourceName,
            endpointName: KeycloakEndpointName);

        await WaitForSeededCatalogAsync();
    }

    /// <inheritdoc />
    public async Task DisposeAsync()
    {
        foreach (HttpClient client in _clients)
        {
            client.Dispose();
        }

        _clients.Clear();

        if (_application is not null)
        {
            await _application.DisposeAsync();
        }
    }

    /// <summary>
    /// Polls <paramref name="probe"/> until it produces a value or <paramref name="timeout"/> elapses.
    /// </summary>
    /// <remarks>
    /// The outbox delivers asynchronously, so every assertion about an effect it produces has to wait
    /// for one. Polling rather than sleeping a fixed interval is what keeps that from being flaky in
    /// both directions: a fixed sleep is simultaneously too long on an idle machine and too short on a
    /// loaded one.
    /// </remarks>
    /// <typeparam name="T">The value the probe produces.</typeparam>
    /// <param name="probe">Reads the current state, returning <see langword="null"/> until it settles.</param>
    /// <param name="description">What is being waited for, quoted in the timeout message.</param>
    /// <param name="timeout">How long to keep polling. Defaults to 90 seconds.</param>
    /// <returns>The first non-null value the probe produced.</returns>
    /// <exception cref="TimeoutException">The probe never produced a value in time.</exception>
    public static async Task<T> PollAsync<T>(
        Func<Task<T?>> probe,
        string description,
        TimeSpan? timeout = null)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(argument: probe);

        TimeSpan limit = timeout ?? TimeSpan.FromSeconds(value: 90);
        long started = Stopwatch.GetTimestamp();

        while (Stopwatch.GetElapsedTime(startingTimestamp: started) < limit)
        {
            T? current = await probe();

            if (current is not null)
            {
                return current;
            }

            await Task.Delay(delay: TimeSpan.FromMilliseconds(value: 250));
        }

        throw new TimeoutException(
            message: $"Timed out after {limit.TotalSeconds:0} seconds waiting for {description}.");
    }

    /// <summary>
    /// Renders a response for an assertion message.
    /// </summary>
    /// <remarks>
    /// A bare "expected 201 but was 401" is the least useful failure this suite can produce, because a
    /// 401 here is almost never a wrong password — it is a token the API declined, and the reason is in
    /// <c>WWW-Authenticate</c>. Quoting that header turns "authentication is broken" into "the issuer
    /// is X and the API expected Y".
    /// </remarks>
    /// <param name="response">The response to describe.</param>
    /// <returns>The status code, the challenge header when there is one, and the body.</returns>
    public static async Task<string> DescribeAsync(HttpResponseMessage response)
    {
        ArgumentNullException.ThrowIfNull(argument: response);

        string challenge = string.Join(
            separator: "; ",
            values: response.Headers.WwwAuthenticate.Select(selector: static header => header.ToString()));

        string body = await response.Content.ReadAsStringAsync();

        return $"{(int)response.StatusCode} {response.StatusCode} from {response.RequestMessage?.RequestUri}; "
            + $"WWW-Authenticate: [{challenge}]; body: {body}";
    }

    /// <summary>
    /// Creates an unauthenticated client for the API.
    /// </summary>
    /// <returns>An HTTP client pointed at the running API, disposed with the fixture.</returns>
    public HttpClient CreateClient()
    {
        if (_application is null)
        {
            throw new InvalidOperationException(message: "The distributed application has not been started.");
        }

        HttpClient client = _application.CreateHttpClient(
            resourceName: ApiResourceName,
            endpointName: ApiEndpointName);

        _clients.Add(item: client);

        return client;
    }

    /// <summary>
    /// Authenticates against Keycloak and returns a client carrying the resulting bearer token.
    /// </summary>
    /// <param name="username">The realm username to authenticate as.</param>
    /// <param name="password">That user's password.</param>
    /// <returns>The authenticated client, together with the token's subject.</returns>
    public async Task<AuthenticatedClient> AuthenticateAsync(string username, string password)
    {
        Uri authority = _keycloakBaseAddress
            ?? throw new InvalidOperationException(message: "Keycloak's endpoint has not been resolved.");

        using var keycloak = new HttpClient { BaseAddress = authority };

        using var form = new FormUrlEncodedContent(
            nameValueCollection: new Dictionary<string, string>(comparer: StringComparer.Ordinal)
            {
                // The realm's eshop-web client is public and has directAccessGrantsEnabled, which is
                // what makes this password grant possible — and is why the API ships no dev token
                // endpoint of its own.
                ["grant_type"] = "password",
                ["client_id"] = "eshop-web",
                ["scope"] = "openid profile email",
                ["username"] = username,
                ["password"] = password,
            });

        using HttpResponseMessage response = await keycloak.PostAsync(
            requestUri: new Uri(uriString: TokenPath, uriKind: UriKind.Relative),
            content: form);

        string body = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                message: $"Keycloak refused the password grant for {username}: {(int)response.StatusCode} {body}.");
        }

        TokenResponse token = JsonSerializer.Deserialize<TokenResponse>(json: body, options: JsonOptions)
            ?? throw new InvalidOperationException(message: "Keycloak returned an empty token response.");

#pragma warning disable CA2000 // Ownership passes to the fixture, which disposes every client it handed out.
        HttpClient client = CreateClient();
#pragma warning restore CA2000
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(scheme: "Bearer", parameter: token.AccessToken);

        return new AuthenticatedClient(Client: client, Subject: ReadSubject(accessToken: token.AccessToken));
    }

    // The seeder is a hosted service, so the API reports Running the moment it is listening, which is
    // before rather than after the sample catalogue exists. Waiting for the featured product here
    // means no individual test has to know that.
    private async Task WaitForSeededCatalogAsync()
    {
        HttpClient client = CreateClient();

        await PollAsync(
            probe: async () =>
            {
                using HttpResponseMessage response = await client.GetAsync(
                    requestUri: new Uri(uriString: AcceptanceRoutes.CatalogProductsPage, uriKind: UriKind.Relative));

                if (!response.IsSuccessStatusCode)
                {
                    return null;
                }

                PagedProducts? page = await response.Content.ReadFromJsonAsync<PagedProducts>();

                return page?.Items.FirstOrDefault(predicate: static product => string.Equals(
                    a: product.Sku,
                    b: AcceptanceRoutes.FeaturedSku,
                    comparisonType: StringComparison.Ordinal));
            },
            description: $"the seeded catalogue to contain {AcceptanceRoutes.FeaturedSku}",
            timeout: StartupTimeout);
    }

    // The journeys need the caller's subject: it is the identifier Basket and Ordering key the
    // caller's own data on. Reading it out of the token beats a round trip to the userinfo endpoint.
    private static string ReadSubject(string accessToken)
    {
        string payload = accessToken.Split(separator: '.')[1]
            .Replace(oldChar: '-', newChar: '+')
            .Replace(oldChar: '_', newChar: '/');

        string padded = payload.PadRight(
            totalWidth: payload.Length + (4 - payload.Length % 4) % 4,
            paddingChar: '=');

        using var claims = JsonDocument.Parse(utf8Json: Convert.FromBase64String(s: padded));

        return claims.RootElement.GetProperty(propertyName: "sub").GetString()
            ?? throw new InvalidOperationException(message: "The access token carried no subject.");
    }
}
