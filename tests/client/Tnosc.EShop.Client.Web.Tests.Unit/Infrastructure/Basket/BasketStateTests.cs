// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Infrastructure;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using Tnosc.EShop.Client.Web.Client.Infrastructure.Basket;

namespace Tnosc.EShop.Client.Web.Tests.Unit.Infrastructure.Basket;

public sealed class BasketStateTests
{
    [Fact]
    public async Task Constructor_Should_SeedItemCount_When_PersistedStateHasAStoredValue()
    {
        // Arrange
        InMemoryPersistentComponentStateStore store = new(
            persistedState: new Dictionary<string, byte[]>(comparer: StringComparer.Ordinal)
            {
                [nameof(BasketState)] = JsonSerializer.SerializeToUtf8Bytes(value: 5),
            });
        ComponentStatePersistenceManager manager = new(logger: NullLogger<ComponentStatePersistenceManager>.Instance);
        await manager.RestoreStateAsync(store: store);

        // Act
        using BasketState sut = new(persistentComponentState: manager.State);

        // Assert
        sut.ItemCount.ShouldBe(expected: 5);
    }

    [Fact]
    public void Constructor_Should_LeaveItemCountAtZero_When_NoPersistedStateExists()
    {
        // Arrange
        ComponentStatePersistenceManager manager = new(logger: NullLogger<ComponentStatePersistenceManager>.Instance);

        // Act
        using BasketState sut = new(persistentComponentState: manager.State);

        // Assert
        sut.ItemCount.ShouldBe(expected: 0);
    }

    private sealed class InMemoryPersistentComponentStateStore(IDictionary<string, byte[]> persistedState)
        : IPersistentComponentStateStore
    {
        public Task<IDictionary<string, byte[]>> GetPersistedStateAsync() =>
            Task.FromResult<IDictionary<string, byte[]>>(
                new Dictionary<string, byte[]>(dictionary: persistedState, comparer: StringComparer.Ordinal));

        public Task PersistStateAsync(IReadOnlyDictionary<string, byte[]> state) => Task.CompletedTask;

        public bool SupportsRenderMode(IComponentRenderMode renderMode) => true;
    }
}
