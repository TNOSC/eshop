// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Shouldly;
using Tnosc.EShop.Server.Domain.Identity.Customers;
using Tnosc.EShop.Server.Infrastructure.Persistence.Identity.Configurations;
using Tnosc.EShop.Server.Tests.Integration.Infrastructure;
using Tnosc.Lib.Shared.Results;
using Xunit;

namespace Tnosc.EShop.Server.Tests.Integration.Identity;

/// <summary>
/// Belt and braces behind the domain's uniqueness rules: the database itself rejects a duplicate email
/// or external user id, so a race that slips past <see cref="CustomerFactory"/>'s check still cannot
/// land two rows.
/// </summary>
/// <remarks>
/// The domain check is what turns a duplicate into a <c>409 Conflict</c> for the caller; these indexes
/// are what stop a concurrent pair of requests — both of which saw "no such customer" — from both
/// committing. Neither replaces the other.
/// </remarks>
public sealed class CustomerUniqueIndexTests(PostgresFixture fixture) : IntegrationTestBase(fixture)
{
    private ICustomerRepository Repository => Scope.ServiceProvider.GetRequiredService<ICustomerRepository>();

    [Fact]
    public async Task TheDatabase_Should_RejectASecondCustomer_WithTheSameEmail()
    {
        // Arrange — both staged before either commits, which is precisely the race the domain check
        // cannot see: each factory call queries a database that does not yet hold the other's row, so
        // both pass the uniqueness check and only the index can stop the second insert.
        await AddCustomerAsync(externalUserId: "sub-a", email: "duplicate@example.com");
        await AddCustomerAsync(externalUserId: "sub-b", email: "duplicate@example.com");

        // Act
        DbUpdateException exception = await Should.ThrowAsync<DbUpdateException>(
            actual: async () => await UnitOfWork.SaveChangesAsync(cancellationToken: CancellationToken.None));

        // Assert
        PostgresException postgres = exception.InnerException.ShouldBeOfType<PostgresException>();
        postgres.SqlState.ShouldBe(expected: PostgresErrorCodes.UniqueViolation);
        postgres.ConstraintName.ShouldBe(expected: CustomerConfiguration.EmailIndexName);
    }

    [Fact]
    public async Task TheDatabase_Should_RejectASecondCustomer_WithTheSameExternalUserId()
    {
        // Arrange
        await AddCustomerAsync(externalUserId: "sub-same", email: "first@example.com");
        await AddCustomerAsync(externalUserId: "sub-same", email: "second@example.com");

        // Act
        DbUpdateException exception = await Should.ThrowAsync<DbUpdateException>(
            actual: async () => await UnitOfWork.SaveChangesAsync(cancellationToken: CancellationToken.None));

        // Assert
        PostgresException postgres = exception.InnerException.ShouldBeOfType<PostgresException>();
        postgres.SqlState.ShouldBe(expected: PostgresErrorCodes.UniqueViolation);
        postgres.ConstraintName.ShouldBe(expected: CustomerConfiguration.ExternalUserIdIndexName);
    }

    [Fact]
    public async Task TheDatabase_Should_AcceptTwoCustomers_WithDifferentEmailsAndSubjects()
    {
        // Arrange
        await AddCustomerAsync(externalUserId: "sub-1", email: "one@example.com");
        await AddCustomerAsync(externalUserId: "sub-2", email: "two@example.com");

        // Act
        await UnitOfWork.SaveChangesAsync(cancellationToken: CancellationToken.None);

        // Assert
        Customer? first = await Repository.GetByExternalIdAsync(
            externalUserId: ExternalUserId.Create(value: "sub-1").Value,
            cancellationToken: CancellationToken.None);

        first.ShouldNotBeNull();
    }

    private async Task AddCustomerAsync(string externalUserId, string email)
    {
        Result<CustomerProvisioning> provisioning = await CustomerFactory.ProvisionAsync(
            repository: Repository,
            externalUserId: ExternalUserId.Create(value: externalUserId).Value,
            email: Email.Create(value: email).Value,
            name: PersonName.Create(firstName: "Sami", lastName: "Shopper").Value,
            phoneNumber: null);

        provisioning.IsSuccess.ShouldBeTrue();
    }
}
