// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using System.Text.Json;
using Shouldly;
using Tnosc.EShop.Server.Tests.Unit.LibInfrastructurePersistence.Outbox.Fakes;
using Tnosc.Lib.Domain;
using Tnosc.Lib.Infrastructure.Persistence.Outbox;
using Xunit;

namespace Tnosc.EShop.Server.Tests.Unit.LibInfrastructurePersistence.Outbox;

/// <summary>
/// The B1 regression: <c>UnitOfWork.ConvertDomainEventsToOutboxMessage</c> used to serialize each
/// domain event through its statically-typed <see cref="IDomainEvent"/> reference. <c>System.Text.Json</c>
/// serializes the DECLARED type unless told otherwise, so every outbox row lost all event data and
/// contained only <c>{Id, OccurredOnUtc}</c>. The fix serializes against <c>domainEvent.GetType()</c>.
/// </summary>
public sealed class OutboxSerializationTests
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = false
    };

    [Fact]
    public void Serialize_Should_LoseDerivedProperties_When_SerializedAgainstTheDeclaredIDomainEventType()
    {
        IDomainEvent domainEvent = new ProductCreatedTestDomainEvent(Id: Guid.NewGuid(), OccurredOnUtc: DateTime.UtcNow, Sku: "SKU-1", Price: 9.99m);

        string json = JsonSerializer.Serialize(value: domainEvent, options: SerializerOptions);

        json.ShouldNotContain(expected: "SKU-1");
        json.ShouldNotContain(expected: "9.99");
    }

    [Fact]
    public void Serialize_Should_RoundTripDerivedProperties_When_SerializedAgainstTheRuntimeType()
    {
        IDomainEvent domainEvent = new ProductCreatedTestDomainEvent(Id: Guid.NewGuid(), OccurredOnUtc: DateTime.UtcNow, Sku: "SKU-1", Price: 9.99m);
        var registry = new DomainEventTypeRegistry(assemblies: typeof(ProductCreatedTestDomainEvent).Assembly);

        // Mirrors UnitOfWork.ConvertDomainEventsToOutboxMessage: serialize against the runtime type
        // and store the registry's durable contract name, not domainEvent.GetType().Name.
        string typeName = registry.GetName(domainEventType: domainEvent.GetType());
        string content = JsonSerializer.Serialize(value: domainEvent, inputType: domainEvent.GetType(), options: SerializerOptions);

        var outboxMessage = new OutboxMessage(type: typeName, content: content);

        registry.TryResolve(name: outboxMessage.Type, domainEventType: out Type? resolvedType).ShouldBeTrue();
        var deserialized = (ProductCreatedTestDomainEvent?)JsonSerializer.Deserialize(json: outboxMessage.Content, returnType: resolvedType, options: SerializerOptions);

        deserialized.ShouldNotBeNull();
        deserialized.Sku.ShouldBe(expected: "SKU-1");
        deserialized.Price.ShouldBe(expected: 9.99m);
    }
}
