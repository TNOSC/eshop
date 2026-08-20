// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;
using Shouldly;
using Tnosc.EShop.Server.Tests.Integration.Infrastructure;
using Xunit;

namespace Tnosc.EShop.Server.Tests.Integration;

/// <summary>
/// T5 regression: <c>EShopReadDbContext</c> is read-only — every write path must throw
/// <see cref="NotSupportedException"/> rather than silently succeed, across all four
/// <c>SaveChanges</c>/<c>SaveChangesAsync</c> overloads inherited from <c>ReadDbContextBase</c>.
/// </summary>
[Collection(nameof(SharedInfrastructureCollection))]
public sealed class ReadDbContextTests(PostgresFixture fixture) : IntegrationTestBase(fixture)
{
    [Fact]
    public void SaveChanges_Should_Throw_When_Called() =>
        Should.Throw<NotSupportedException>(actual: () => ReadContext.SaveChanges());

    [Fact]
    public void SaveChanges_Should_Throw_When_CalledWithAcceptAllChangesOnSuccess() =>
        Should.Throw<NotSupportedException>(actual: () => ReadContext.SaveChanges(acceptAllChangesOnSuccess: true));

    [Fact]
    public async Task SaveChangesAsync_Should_Throw_When_Called() =>
        await Should.ThrowAsync<NotSupportedException>(actual: () => ReadContext.SaveChangesAsync(cancellationToken: CancellationToken.None));

    [Fact]
    public async Task SaveChangesAsync_Should_Throw_When_CalledWithAcceptAllChangesOnSuccess() =>
        await Should.ThrowAsync<NotSupportedException>(
            actual: () => ReadContext.SaveChangesAsync(acceptAllChangesOnSuccess: true, cancellationToken: CancellationToken.None));
}
