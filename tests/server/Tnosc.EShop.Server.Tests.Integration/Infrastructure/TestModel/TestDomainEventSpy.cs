// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Tnosc.EShop.Server.Tests.Integration.Infrastructure.TestModel;

/// <summary>
/// A process-wide singleton that <see cref="TestDomainEventHandler"/> records every successfully
/// handled <see cref="TestAggregateCreatedDomainEvent"/> into, keyed by the raising aggregate's
/// <see cref="Guid"/>, so concurrent <c>OutboxProcessor</c> instances — each opening their own DI
/// scope — can be observed from the single test thread that started them.
/// </summary>
/// <remarks>
/// Backed by a <see cref="ConcurrentBag{T}"/> because the SKIP LOCKED concurrency test deliberately
/// drives two processors from two threads at once. <see cref="IntegrationTestBase"/> clears this
/// spy before every test, so state never leaks across tests despite the singleton lifetime.
/// </remarks>
public sealed class TestDomainEventSpy
{
    private readonly ConcurrentBag<Guid> _delivered = [];

    /// <summary>
    /// Records that the domain event with the given <paramref name="aggregateId"/> was delivered to
    /// a handler.
    /// </summary>
    /// <param name="aggregateId">The raising aggregate's identifier, used as the delivery key.</param>
    public void RecordDelivery(Guid aggregateId) => _delivered.Add(item: aggregateId);

    /// <summary>
    /// Gets a snapshot of every aggregate id delivered since the last <see cref="Clear"/>.
    /// </summary>
    /// <returns>A snapshot list of delivered aggregate ids, duplicates included.</returns>
    public IReadOnlyList<Guid> Delivered() => [.. _delivered];

    /// <summary>
    /// Clears every recorded delivery. Called by <see cref="IntegrationTestBase"/> before each test.
    /// </summary>
    public void Clear() => _delivered.Clear();
}
