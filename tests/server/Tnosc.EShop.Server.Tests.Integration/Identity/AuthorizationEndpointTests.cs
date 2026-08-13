// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Shouldly;
using Tnosc.EShop.Server.Shared.Authorization;
using Tnosc.EShop.Server.Tests.Integration.Infrastructure;
using Xunit;

namespace Tnosc.EShop.Server.Tests.Integration.Identity;

/// <summary>
/// The 401 → 403 → 200 progression over the real HTTP pipeline: no token, a token whose role lacks the
/// permission, and a token whose role grants it.
/// </summary>
/// <remarks>
/// These run against the real endpoints, the real policy provider and the real claims transformation —
/// only the token's signing key is a test one. That is what makes them evidence rather than a restatement
/// of the fixture.
/// </remarks>
[Collection(nameof(PostgresCollection))]
public sealed class AuthorizationEndpointTests(PostgresFixture fixture) : IAsyncLifetime, IDisposable
{
    private const string CatalogWriteRoute = "/api/catalog/products";
    private const string CatalogReadRoute = "/api/catalog/products";
    private const string ProvisionRoute = "/api/identity/customers";
    private const string CurrentCustomerRoute = "/api/identity/customers/me";
    private const string ListCustomersRoute = "/api/identity/customers";

    private EShopApiFactory _factory = null!;
    private HttpClient _client = null!;

    /// <inheritdoc />
    public async Task InitializeAsync()
    {
        await fixture.ResetAsync();

        _factory = new EShopApiFactory(connectionString: fixture.ConnectionString, redisConnectionString: fixture.RedisConnectionString);
        _client = _factory.CreateClient();
    }

    /// <inheritdoc />
    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _factory.DisposeAsync();
    }

    /// <summary>
    /// Satisfies CA1001 for the disposable fields; the real teardown is the async one xUnit calls.
    /// </summary>
    public void Dispose()
    {
        _client?.Dispose();
        _factory?.Dispose();
    }

    [Fact]
    public async Task Returns_401_When_No_Token_Is_Supplied()
    {
        // Act
        HttpResponseMessage response = await _client.PostAsJsonAsync(requestUri: CatalogWriteRoute, value: NewProduct());

        // Assert
        response.StatusCode.ShouldBe(expected: HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Returns_401_When_The_Token_Signature_Is_Not_Trusted()
    {
        // Arrange
        Authorize(token: TestTokenIssuer.IssueWithWrongKey());

        // Act
        HttpResponseMessage response = await _client.PostAsJsonAsync(requestUri: CatalogWriteRoute, value: NewProduct());

        // Assert
        response.StatusCode.ShouldBe(expected: HttpStatusCode.Unauthorized);
    }

    // The distinction the policy provider buys us: authenticated but unpermitted is 403, not 401.
    [Fact]
    public async Task Returns_403_When_The_Role_Lacks_The_Permission()
    {
        // Arrange
        Authorize(token: TestTokenIssuer.Issue(
            subject: Guid.CreateVersion7().ToString(),
            email: "customer@eshop.local",
            realmRoles: Roles.Customer));

        // Act
        HttpResponseMessage response = await _client.PostAsJsonAsync(requestUri: CatalogWriteRoute, value: NewProduct());

        // Assert
        response.StatusCode.ShouldBe(
            expected: HttpStatusCode.Forbidden,
            customMessage: "A customer token authenticates, but 'customer' does not grant catalog:write.");
    }

    [Fact]
    public async Task Returns_Not_403_When_The_Role_Grants_The_Permission()
    {
        // Arrange
        Authorize(token: TestTokenIssuer.Issue(
            subject: Guid.CreateVersion7().ToString(),
            email: "admin@eshop.local",
            realmRoles: Roles.Admin));

        // Act
        HttpResponseMessage response = await _client.PostAsJsonAsync(requestUri: CatalogWriteRoute, value: NewProduct());

        // Assert
        // The admin token clears authorization, so this must not be 401 or 403. What comes back next is
        // the handler's own verdict — a 400 for the missing Idempotency-Key that CreateProduct requires —
        // which is exactly the point: the request reached the pipeline behind the permission check.
        response.StatusCode.ShouldNotBe(expected: HttpStatusCode.Unauthorized);
        response.StatusCode.ShouldNotBe(expected: HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Catalog_Reads_Should_Stay_Anonymous()
    {
        // Act
        HttpResponseMessage response = await _client.GetAsync(requestUri: CatalogReadRoute);

        // Assert
        response.StatusCode.ShouldBe(
            expected: HttpStatusCode.OK,
            customMessage: "A storefront catalogue is public — that is a decision, not an oversight.");
    }

    [Fact]
    public async Task Returns_401_When_No_Token_Is_Supplied_On_An_Admin_Identity_Write_Endpoint()
    {
        // Act
        HttpResponseMessage response = await _client.PutAsJsonAsync(
            requestUri: AdminUpdateProfileRoute(customerId: Guid.CreateVersion7()),
            value: new { firstName = "Amel", lastName = "Operator" });

        // Assert
        response.StatusCode.ShouldBe(expected: HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Returns_403_When_A_Customer_Token_Calls_An_Admin_Identity_Write_Endpoint()
    {
        // Arrange
        Authorize(token: TestTokenIssuer.Issue(
            subject: Guid.CreateVersion7().ToString(),
            email: "customer@eshop.local",
            realmRoles: Roles.Customer));

        // Act
        HttpResponseMessage response = await _client.PutAsJsonAsync(
            requestUri: AdminUpdateProfileRoute(customerId: Guid.CreateVersion7()),
            value: new { firstName = "Amel", lastName = "Operator" });

        // Assert
        response.StatusCode.ShouldBe(
            expected: HttpStatusCode.Forbidden,
            customMessage: "A customer token authenticates, but 'customer' does not grant identity:write.");
    }

    [Fact]
    public async Task Returns_Not_401_Or_403_When_An_Admin_Token_Calls_An_Admin_Identity_Write_Endpoint()
    {
        // Arrange
        Authorize(token: TestTokenIssuer.Issue(
            subject: Guid.CreateVersion7().ToString(),
            email: "admin@eshop.local",
            realmRoles: Roles.Admin));

        // Act
        HttpResponseMessage response = await _client.PutAsJsonAsync(
            requestUri: AdminUpdateProfileRoute(customerId: Guid.CreateVersion7()),
            value: new { firstName = "Amel", lastName = "Operator" });

        // Assert
        // The admin token clears authorization, so this must not be 401 or 403. A 404 comes back next
        // because the customer id does not exist — the request reached the handler behind the check.
        response.StatusCode.ShouldNotBe(expected: HttpStatusCode.Unauthorized);
        response.StatusCode.ShouldNotBe(expected: HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Identity_List_Should_Require_The_Read_Permission()
    {
        // Act
        HttpResponseMessage response = await _client.GetAsync(requestUri: ListCustomersRoute);

        // Assert
        response.StatusCode.ShouldBe(expected: HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Identity_Me_Should_Require_Authentication()
    {
        // Act
        HttpResponseMessage response = await _client.GetAsync(requestUri: CurrentCustomerRoute);

        // Assert
        response.StatusCode.ShouldBe(expected: HttpStatusCode.Unauthorized);
    }

    // IUserContext is populated from the token alone, so provisioning with a body that carries no
    // subject or email still lands a customer whose external id is the token's `sub`.
    [Fact]
    public async Task UserContext_Should_Be_Populated_From_The_Token()
    {
        // Arrange
        string subject = Guid.CreateVersion7().ToString();
        Authorize(token: TestTokenIssuer.Issue(subject: subject, email: "sami@example.com", realmRoles: Roles.Customer));

        // Act
        HttpResponseMessage provisioned = await _client.PostAsJsonAsync(
            requestUri: ProvisionRoute,
            value: new { firstName = "Sami", lastName = "Shopper" });

        HttpResponseMessage read = await _client.GetAsync(requestUri: CurrentCustomerRoute);

        // Assert
        provisioned.StatusCode.ShouldBe(expected: HttpStatusCode.Created);
        read.StatusCode.ShouldBe(expected: HttpStatusCode.OK);

        CustomerResponse? customer = await read.Content.ReadFromJsonAsync<CustomerResponse>();
        customer.ShouldNotBeNull();
        customer.Email.ShouldBe(expected: "sami@example.com", customMessage: "The email is taken from the token, never from the body.");
        customer.FirstName.ShouldBe(expected: "Sami");
    }

    [Fact]
    public async Task Provisioning_Should_Return_200_On_A_Second_Login_And_201_On_The_First()
    {
        // Arrange
        string subject = Guid.CreateVersion7().ToString();
        Authorize(token: TestTokenIssuer.Issue(subject: subject, email: "repeat@example.com", realmRoles: Roles.Customer));
        object body = new { firstName = "Sami", lastName = "Shopper" };

        // Act
        HttpResponseMessage first = await _client.PostAsJsonAsync(requestUri: ProvisionRoute, value: body);
        HttpResponseMessage second = await _client.PostAsJsonAsync(requestUri: ProvisionRoute, value: body);

        // Assert
        first.StatusCode.ShouldBe(expected: HttpStatusCode.Created);
        second.StatusCode.ShouldBe(expected: HttpStatusCode.OK, customMessage: "A repeat login reconciles rather than registering again.");

        Guid firstId = await first.Content.ReadFromJsonAsync<Guid>();
        Guid secondId = await second.Content.ReadFromJsonAsync<Guid>();
        secondId.ShouldBe(expected: firstId, customMessage: "Both calls must resolve to the same customer.");
    }

    [Fact]
    public async Task Provisioning_Should_Return_409_When_Another_Subject_Claims_The_Same_Email()
    {
        // Arrange
        object body = new { firstName = "Sami", lastName = "Shopper" };

        Authorize(token: TestTokenIssuer.Issue(
            subject: Guid.CreateVersion7().ToString(),
            email: "shared@example.com",
            realmRoles: Roles.Customer));
        await _client.PostAsJsonAsync(requestUri: ProvisionRoute, value: body);

        Authorize(token: TestTokenIssuer.Issue(
            subject: Guid.CreateVersion7().ToString(),
            email: "shared@example.com",
            realmRoles: Roles.Customer));

        // Act
        HttpResponseMessage response = await _client.PostAsJsonAsync(requestUri: ProvisionRoute, value: body);

        // Assert
        response.StatusCode.ShouldBe(expected: HttpStatusCode.Conflict);
    }

    private static string AdminUpdateProfileRoute(Guid customerId) =>
        $"/api/identity/customers/{customerId}/profile";

    private void Authorize(string token) =>
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(scheme: "Bearer", parameter: token);

    private static object NewProduct() => new
    {
        sku = "AUTH-1",
        name = "Widget",
        priceAmount = 9.99m,
        priceCurrency = "EUR",
        stockQuantity = 1,
        brandId = Guid.CreateVersion7(),
        categoryId = Guid.CreateVersion7(),
    };

    private sealed record CustomerResponse(Guid Id, string Email, string FirstName, string LastName);
}
