// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC). All rights reserved.
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Tnosc.EShop.Server.Tests.Integration.Infrastructure.TestModel;

/// <summary>
/// Extends the model built by the default EF Core model customizer with
/// <see cref="TestAggregateConfiguration"/>, without changing <c>EShopWriteDbContext</c> itself.
/// </summary>
/// <remarks>
/// <c>EShopWriteDbContext</c> is <c>sealed</c> and its <c>OnModelCreating</c> is production code that
/// must not be touched to wire in a test-only entity. Replacing EF Core's internal
/// <see cref="IModelCustomizer"/> service — via <c>DbContextOptionsBuilder.ReplaceService</c> — is the
/// documented, supported way to extend a sealed context's model from outside the assembly that
/// declares it. <see cref="PostgresFixture"/> wires this in only for the test service provider.
/// </remarks>
/// <param name="dependencies">The dependencies required by <see cref="RelationalModelCustomizer"/>.</param>
internal sealed class TestModelCustomizer(ModelCustomizerDependencies dependencies) : RelationalModelCustomizer(dependencies)
{
    /// <inheritdoc />
    public override void Customize(ModelBuilder modelBuilder, DbContext context)
    {
        base.Customize(modelBuilder, context);
        modelBuilder.ApplyConfiguration(new TestAggregateConfiguration());
    }
}
