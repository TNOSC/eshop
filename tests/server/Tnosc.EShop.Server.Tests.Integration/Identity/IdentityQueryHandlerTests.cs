// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Tnosc.EShop.Server.Application.Identity.Queries.GetCurrentCustomer;
using Tnosc.EShop.Server.Application.Identity.Queries.GetCustomerById;
using Tnosc.EShop.Server.Application.Identity.Queries.ListCustomers;
using Tnosc.EShop.Server.Domain.Identity.Customers;
using Tnosc.EShop.Server.Tests.Integration.Infrastructure;
using Tnosc.Lib.Application.Queries;
using Tnosc.Lib.Shared.Results;
using Xunit;

namespace Tnosc.EShop.Server.Tests.Integration.Identity;

/// <summary>
/// The Identity query handlers against real Postgres: the projection, the address collection, and the
/// not-found paths.
/// </summary>
public sealed class IdentityQueryHandlerTests(PostgresFixture fixture) : IntegrationTestBase(fixture)
{
    private IQueryHandler<GetCurrentCustomerQuery, CustomerDto> CurrentCustomerHandler =>
        Scope.ServiceProvider.GetRequiredService<IQueryHandler<GetCurrentCustomerQuery, CustomerDto>>();

    private IQueryHandler<GetCustomerByIdQuery, CustomerDto> CustomerByIdHandler =>
        Scope.ServiceProvider.GetRequiredService<IQueryHandler<GetCustomerByIdQuery, CustomerDto>>();

    private ICustomerRepository Repository => Scope.ServiceProvider.GetRequiredService<ICustomerRepository>();

    private IQueryHandler<ListCustomersQuery, PagedResult<CustomerSummaryDto>> ListCustomersHandler =>
        Scope.ServiceProvider.GetRequiredService<IQueryHandler<ListCustomersQuery, PagedResult<CustomerSummaryDto>>>();

    [Fact]
    public async Task GetCurrentCustomer_Should_ProjectTheProfile_And_ItsAddresses()
    {
        // Arrange
        Customer customer = await SeedCustomerAsync(externalUserId: "sub-projection", email: "sami@example.com");

        // Act
        Result<CustomerDto> result = await CurrentCustomerHandler.HandleAsync(
            query: new GetCurrentCustomerQuery(ExternalUserId: "sub-projection"));

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.Id.ShouldBe(expected: customer.Id.Value);
        result.Value.Email.ShouldBe(expected: "sami@example.com");
        result.Value.FirstName.ShouldBe(expected: "Sami");
        result.Value.LastName.ShouldBe(expected: "Shopper");
        result.Value.IsActive.ShouldBeTrue();

        CustomerAddressDto address = result.Value.Addresses.ShouldHaveSingleItem();
        address.Street.ShouldBe(expected: "12 Rue Neuve");
        address.City.ShouldBe(expected: "Tunis");
        address.PostalCode.ShouldBe(expected: "1001");
        address.Country.ShouldBe(expected: "TN");
        result.Value.DefaultAddressId.ShouldBe(expected: address.Id, customMessage: "The first address added is the default.");
    }

    [Fact]
    public async Task GetCurrentCustomer_Should_ReturnNotProvisioned_When_TheSubjectHasNoProfile()
    {
        // Act
        Result<CustomerDto> result = await CurrentCustomerHandler.HandleAsync(
            query: new GetCurrentCustomerQuery(ExternalUserId: "sub-nobody"));

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Type.ShouldBe(expected: ErrorType.NotFound);
        result.FirstError.Code.ShouldBe(expected: "Customer.NotProvisioned");
    }

    [Fact]
    public async Task GetCustomerById_Should_ProjectTheProfile()
    {
        // Arrange
        Customer customer = await SeedCustomerAsync(externalUserId: "sub-byid", email: "byid@example.com");

        // Act
        Result<CustomerDto> result = await CustomerByIdHandler.HandleAsync(
            query: new GetCustomerByIdQuery(CustomerId: customer.Id.Value));

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.Email.ShouldBe(expected: "byid@example.com");
        result.Value.Addresses.ShouldHaveSingleItem();
    }

    [Fact]
    public async Task GetCustomerById_Should_ReturnNotFound_When_TheIdIsUnknown()
    {
        // Act
        Result<CustomerDto> result = await CustomerByIdHandler.HandleAsync(
            query: new GetCustomerByIdQuery(CustomerId: Guid.CreateVersion7()));

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Type.ShouldBe(expected: ErrorType.NotFound);
        result.FirstError.Code.ShouldBe(expected: "Customer.NotFound");
    }

    [Fact]
    public async Task GetCurrentCustomer_Should_ReflectAProfileUpdate()
    {
        // Arrange
        Customer customer = await SeedCustomerAsync(externalUserId: "sub-update", email: "update@example.com");
        customer.UpdateProfile(
            name: PersonName.Create(firstName: "Amel", lastName: "Operator").Value,
            phoneNumber: PhoneNumber.Create(value: "+21612345678").Value);
        await UnitOfWork.SaveChangesAsync(cancellationToken: CancellationToken.None);

        // Act
        Result<CustomerDto> result = await CurrentCustomerHandler.HandleAsync(
            query: new GetCurrentCustomerQuery(ExternalUserId: "sub-update"));

        // Assert
        result.Value.FirstName.ShouldBe(expected: "Amel");
        result.Value.PhoneNumber.ShouldBe(expected: "+21612345678");
    }

    [Fact]
    public async Task ListCustomers_Should_PageResults_And_CarryTheTotalCount()
    {
        // Arrange
        await SeedCustomerAsync(externalUserId: "sub-list-1", email: "a@example.com");
        await SeedCustomerAsync(externalUserId: "sub-list-2", email: "b@example.com");
        await SeedCustomerAsync(externalUserId: "sub-list-3", email: "c@example.com");

        // Act
        Result<PagedResult<CustomerSummaryDto>> result = await ListCustomersHandler.HandleAsync(
            query: new ListCustomersQuery(SearchTerm: null, IsActive: null, Page: 1, PageSize: 2));

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.Items.Count.ShouldBe(expected: 2);
        result.Value.TotalCount.ShouldBe(expected: 3);
        result.Value.TotalPages.ShouldBe(expected: 2);
    }

    [Fact]
    public async Task ListCustomers_Should_FilterBySearchTerm()
    {
        // Arrange
        await SeedCustomerAsync(externalUserId: "sub-search-1", email: "findme@example.com");
        await SeedCustomerAsync(externalUserId: "sub-search-2", email: "other@example.com");

        // Act
        Result<PagedResult<CustomerSummaryDto>> result = await ListCustomersHandler.HandleAsync(
            query: new ListCustomersQuery(SearchTerm: "findme", IsActive: null, Page: 1, PageSize: 20));

        // Assert
        result.IsSuccess.ShouldBeTrue();
        CustomerSummaryDto found = result.Value.Items.ShouldHaveSingleItem();
        found.Email.ShouldBe(expected: "findme@example.com");
    }

    [Fact]
    public async Task ListCustomers_Should_FilterByIsActive()
    {
        // Arrange
        await SeedCustomerAsync(externalUserId: "sub-active-1", email: "active@example.com");
        Customer deactivated = await SeedCustomerAsync(externalUserId: "sub-active-2", email: "inactive@example.com");
        deactivated.Deactivate();
        await UnitOfWork.SaveChangesAsync(cancellationToken: CancellationToken.None);

        // Act
        Result<PagedResult<CustomerSummaryDto>> result = await ListCustomersHandler.HandleAsync(
            query: new ListCustomersQuery(SearchTerm: null, IsActive: false, Page: 1, PageSize: 20));

        // Assert
        result.IsSuccess.ShouldBeTrue();
        CustomerSummaryDto found = result.Value.Items.ShouldHaveSingleItem();
        found.Email.ShouldBe(expected: "inactive@example.com");
        found.IsActive.ShouldBeFalse();
    }

    private async Task<Customer> SeedCustomerAsync(string externalUserId, string email)
    {
        Result<CustomerProvisioning> provisioning = await CustomerFactory.ProvisionAsync(
            repository: Repository,
            externalUserId: ExternalUserId.Create(value: externalUserId).Value,
            email: Email.Create(value: email).Value,
            name: PersonName.Create(firstName: "Sami", lastName: "Shopper").Value,
            phoneNumber: null);

        Customer customer = provisioning.Value.Customer;
        customer.AddAddress(street: "12 Rue Neuve", city: "Tunis", postalCode: "1001", country: "TN");

        await UnitOfWork.SaveChangesAsync(cancellationToken: CancellationToken.None);

        return customer;
    }
}
