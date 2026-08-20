// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using Tnosc.EShop.Server.Tests.Integration.Infrastructure;
using Tnosc.EShop.Server.Tests.Integration.Infrastructure.TestModel;
using Xunit;

namespace Tnosc.EShop.Server.Tests.Integration.Regression;

/// <summary>
/// Regression tests for B3: <c>UpdateAuditableEntries</c> used to assign a <c>DateTimeOffset</c> to
/// <c>IAuditable</c>'s <c>DateTime</c> properties, throwing on every write. The fix injects
/// <c>TimeProvider</c> and uses <c>GetUtcNow().UtcDateTime</c>.
/// </summary>
[Collection(nameof(SharedInfrastructureCollection))]
public sealed class AuditableStampingTests(PostgresFixture fixture) : IntegrationTestBase(fixture)
{
    [Fact]
    public async Task SaveChangesAsync_Should_StampCreatedOnUtcAsDateTime_When_SavingANewAuditableAggregate()
    {
        var aggregate = TestAggregate.Create(name: "initial-name");
        WriteContext.Add(entity: aggregate);

        // No exception here is itself part of the assertion — B3 made this throw on every write.
        await UnitOfWork.SaveChangesAsync();

        TestAggregate? persisted = await WriteContext.Set<TestAggregate>().AsNoTracking()
            .SingleOrDefaultAsync(predicate: a => a.Id == aggregate.Id);

        persisted.ShouldNotBeNull();
        // Tolerance covers Postgres's microsecond timestamp precision vs. .NET's 100ns ticks.
        persisted.CreatedOnUtc.ShouldBe(expected: TimeProvider.GetUtcNow().UtcDateTime, tolerance: TimeSpan.FromMilliseconds(milliseconds: 1));
        persisted.CreatedBy.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task SaveChangesAsync_Should_StampUpdatedOnUtcAsDateTime_When_SavingAModifiedAuditableAggregate()
    {
        var aggregate = TestAggregate.Create(name: "initial-name");
        WriteContext.Add(entity: aggregate);
        await UnitOfWork.SaveChangesAsync();

        TimeProvider.Advance(delta: TimeSpan.FromMinutes(minutes: 5));

        aggregate.Rename(name: "renamed");
        WriteContext.Update(entity: aggregate);

        await UnitOfWork.SaveChangesAsync();

        TestAggregate? persisted = await WriteContext.Set<TestAggregate>().AsNoTracking()
            .SingleOrDefaultAsync(predicate: a => a.Id == aggregate.Id);

        persisted.ShouldNotBeNull();
        persisted.Name.ShouldBe(expected: "renamed");
        persisted.UpdatedOnUtc.ShouldBe(expected: TimeProvider.GetUtcNow().UtcDateTime, tolerance: TimeSpan.FromMilliseconds(milliseconds: 1));
        persisted.UpdatedBy.ShouldNotBeNullOrWhiteSpace();
    }
}
