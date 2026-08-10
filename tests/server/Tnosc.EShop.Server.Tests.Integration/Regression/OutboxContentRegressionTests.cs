// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC). All rights reserved.
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using Tnosc.EShop.Server.Tests.Integration.Infrastructure;
using Tnosc.EShop.Server.Tests.Integration.Infrastructure.TestModel;
using Tnosc.Lib.Infrastructure.Persistence.Outbox;
using Xunit;

namespace Tnosc.EShop.Server.Tests.Integration.Regression;

/// <summary>
/// Regression tests for B1 — the most important test in the suite. <c>UnitOfWork.ConvertDomainEventsToOutboxMessage</c>
/// used to serialize each domain event through its statically-typed <c>IDomainEvent</c> reference,
/// so <c>System.Text.Json</c> serialized only the members visible on that interface —
/// <c>{Id, OccurredOnUtc}</c> — and every other property was silently dropped. The fix serializes
/// against <c>domainEvent.GetType()</c>. Also covers T4's requirement that <c>OutboxMessage.Type</c>
/// holds the <c>[DomainEventName]</c> registry contract name, never the CLR short name.
/// </summary>
[Collection(nameof(PostgresCollection))]
public sealed class OutboxContentRegressionTests(PostgresFixture fixture) : IntegrationTestBase(fixture)
{
    [Fact]
    public async Task SaveChangesAsync_Should_WriteExtraDomainEventPropertiesIntoOutboxContent_When_TheAggregateRaisesAnEventWithExtraProperties()
    {
        var aggregate = TestAggregate.Create(name: "widget");
        aggregate.RaiseCreatedEvent(note: "handle with care", amount: 42, tags: ["red", "blue"]);
        WriteContext.Add(entity: aggregate);

        await UnitOfWork.SaveChangesAsync();

        OutboxMessage row = await WriteContext.Set<OutboxMessage>().AsNoTracking()
            .SingleAsync(predicate: message => message.Type == "test.aggregate-created.v1");

        using var document = JsonDocument.Parse(json: row.Content);

        // Against the pre-fix code, Content is exactly {"Id":...,"OccurredOnUtc":...} — none of these
        // properties exist and GetProperty throws.
        document.RootElement.GetProperty(propertyName: "AggregateId").GetGuid().ShouldBe(expected: aggregate.Id.Value);
        document.RootElement.GetProperty(propertyName: "Name").GetString().ShouldBe(expected: "widget");
        document.RootElement.GetProperty(propertyName: "Note").GetString().ShouldBe(expected: "handle with care");
        document.RootElement.GetProperty(propertyName: "Amount").GetInt32().ShouldBe(expected: 42);
        document.RootElement.GetProperty(propertyName: "Tags").EnumerateArray().Select(selector: tag => tag.GetString())
            .ShouldBe(expected: ["red", "blue"]);

        // Belt and braces: the pre-fix payload has exactly two properties.
        document.RootElement.EnumerateObject().Count().ShouldBeGreaterThan(expected: 2);
    }

    [Fact]
    public async Task SaveChangesAsync_Should_WriteTheRegistryContractName_When_ConvertingADomainEventToAnOutboxMessage()
    {
        var aggregate = TestAggregate.Create(name: "widget");
        aggregate.RaiseCreatedEvent(note: "n/a", amount: 1, tags: []);
        WriteContext.Add(entity: aggregate);

        await UnitOfWork.SaveChangesAsync();

        OutboxMessage row = await WriteContext.Set<OutboxMessage>().AsNoTracking().SingleAsync();

        row.Type.ShouldBe(expected: "test.aggregate-created.v1");
        row.Type.Equals(value: nameof(TestAggregateCreatedDomainEvent), comparisonType: StringComparison.Ordinal).ShouldBeFalse();
    }
}
