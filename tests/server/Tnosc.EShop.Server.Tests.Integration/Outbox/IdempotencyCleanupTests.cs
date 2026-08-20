// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using Tnosc.EShop.Server.Infrastructure.Persistence.Contexts;
using Tnosc.EShop.Server.Tests.Integration.Infrastructure;
using Tnosc.Lib.Infrastructure.Persistence.Idempotency;
using Xunit;

namespace Tnosc.EShop.Server.Tests.Integration.Outbox;

/// <summary>
/// The retention half of the feature: expired idempotency keys and inbox claims are collected, live
/// ones are left alone, and each pass is bounded by the configured batch size.
/// </summary>
/// <remarks>
/// Drives <see cref="IdempotencyCleanupBackgroundService{TContext}.CollectAsync"/> directly rather
/// than waiting on the hosted service's timer, so the pass is deterministic.
/// </remarks>
[Collection(nameof(SharedInfrastructureCollection))]
public sealed class IdempotencyCleanupTests(PostgresFixture fixture) : IntegrationTestBase(fixture)
{
    private const string Handler = "Tests.SomeCommandHandler";

    [Fact]
    public async Task CollectAsync_Should_DeleteExpiredRowsOnly()
    {
        // Arrange
        DateTime now = TimeProvider.GetUtcNow().UtcDateTime;

        await SeedAsync(
            Request(key: "expired", expiresOnUtc: now.AddMinutes(value: -1)),
            Request(key: "live", expiresOnUtc: now.AddHours(value: 1)),
            new ProcessedEvent(eventId: Guid.CreateVersion7(), handler: Handler, processedOnUtc: now.AddDays(value: -2)),
            new ProcessedEvent(eventId: Guid.CreateVersion7(), handler: Handler, processedOnUtc: now));

        // Act
        int deleted = await CollectAsync(options: new IdempotencyOptions());

        // Assert
        deleted.ShouldBe(expected: 2, customMessage: "one expired key and one out-of-retention inbox claim");

        WriteContext.ChangeTracker.Clear();
        (await WriteContext.Set<IdempotencyRequest>().SingleAsync()).Key.ShouldBe(expected: "live");
        (await WriteContext.Set<ProcessedEvent>().CountAsync()).ShouldBe(expected: 1);
    }

    [Fact]
    public async Task CollectAsync_Should_StopAtTheBatchSize_When_MoreRowsHaveExpired()
    {
        // Arrange
        DateTime expired = TimeProvider.GetUtcNow().UtcDateTime.AddMinutes(value: -1);

        await SeedAsync(
            Request(key: "a", expiresOnUtc: expired),
            Request(key: "b", expiresOnUtc: expired),
            Request(key: "c", expiresOnUtc: expired));

        // Act
        int deleted = await CollectAsync(options: new IdempotencyOptions { BatchSize = 2 });

        // Assert
        deleted.ShouldBe(expected: 2, customMessage: "a tick must stay bounded rather than lock a whole day of rows at once");

        WriteContext.ChangeTracker.Clear();
        (await WriteContext.Set<IdempotencyRequest>().CountAsync()).ShouldBe(expected: 1);
    }

    private async Task<int> CollectAsync(IdempotencyOptions options) =>
        await IdempotencyCleanupBackgroundService<EShopWriteDbContext>.CollectAsync(
            context: WriteContext,
            options: options,
            timeProvider: TimeProvider,
            cancellationToken: CancellationToken.None);

    private static IdempotencyRequest Request(string key, DateTime expiresOnUtc) =>
        new(key: key,
            handler: Handler,
            requestHash: new string(c: '0', count: 64),
            response: null,
            responseType: null,
            createdOnUtc: expiresOnUtc.AddHours(value: -24),
            expiresOnUtc: expiresOnUtc);
}
