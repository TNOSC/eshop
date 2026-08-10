// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC). All rights reserved.
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using Tnosc.Lib.Domain;

namespace Tnosc.EShop.Server.Tests.Integration.Infrastructure.TestModel;

/// <summary>
/// A minimal, test-only <see cref="AggregateRoot{TEntityId}"/> used to exercise the write pipeline —
/// audit stamping, domain-event-to-outbox conversion, and outbox processing — against a real
/// Postgres instance, without touching any production aggregate or model.
/// </summary>
internal sealed class TestAggregate : AggregateRoot<TestAggregateId>
{
    private TestAggregate()
    {
        // EF.
    }

    /// <summary>
    /// Gets the aggregate's name.
    /// </summary>
    public string Name { get; private set; } = null!;

    /// <summary>
    /// Creates a new <see cref="TestAggregate"/>. Raises no domain event by itself — callers raise
    /// one explicitly via <see cref="RaiseCreatedEvent"/> or <see cref="RaisePoisonEvent"/> so tests
    /// control exactly what lands in the outbox.
    /// </summary>
    /// <param name="name">The aggregate's name.</param>
    /// <returns>A new <see cref="TestAggregate"/>.</returns>
    public static TestAggregate Create(string name) =>
        new() { Id = TestAggregateId.New(), Name = name };

    /// <summary>
    /// Renames the aggregate, triggering the <c>Modified</c> audit-stamping path on save.
    /// </summary>
    /// <param name="name">The new name.</param>
    public void Rename(string name) => Name = name;

    /// <summary>
    /// Raises a <see cref="TestAggregateCreatedDomainEvent"/> carrying the supplied extra properties.
    /// </summary>
    /// <param name="note">An arbitrary note.</param>
    /// <param name="amount">An arbitrary amount.</param>
    /// <param name="tags">An arbitrary tag collection.</param>
    public void RaiseCreatedEvent(string note, int amount, IReadOnlyCollection<string> tags) =>
        AddDomainEvent(new TestAggregateCreatedDomainEvent(Guid.NewGuid(), DateTime.UtcNow, Id.Value, Name, note, amount, tags));

    /// <summary>
    /// Raises a <see cref="PoisonTestDomainEvent"/> whose registered handler always throws.
    /// </summary>
    public void RaisePoisonEvent() =>
        AddDomainEvent(new PoisonTestDomainEvent(Guid.NewGuid(), DateTime.UtcNow, Id.Value));
}
