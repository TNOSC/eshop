// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Tnosc.EShop.Server.Infrastructure.Persistence.Contexts;

namespace Tnosc.EShop.Server.Infrastructure.Persistence.DesignTime;

/// <summary>
/// Creates <see cref="EShopWriteDbContext"/> instances for <c>dotnet ef</c> commands.
/// </summary>
/// <remarks>
/// Aspire injects the connection string at runtime via <c>AddNpgsqlDbContext</c>; <c>dotnet ef</c>
/// runs the design-time host outside Aspire, so no connection string is available unless this
/// factory supplies one explicitly.
/// </remarks>
internal sealed class EShopWriteDbContextFactory : IDesignTimeDbContextFactory<EShopWriteDbContext>
{
#pragma warning disable S2068 // Local-only default for `dotnet ef` design-time execution, not a real credential.
    private const string FallbackConnectionString =
        "Host=localhost;Port=5432;Database=eshopdb;Username=postgres;Password=postgres";
#pragma warning restore S2068

    public EShopWriteDbContext CreateDbContext(string[] args)
    {
        string connectionString =
            Environment.GetEnvironmentVariable(variable: "ConnectionStrings__eshopdb") ?? FallbackConnectionString;

        DbContextOptionsBuilder<EShopWriteDbContext> optionsBuilder = new();
        optionsBuilder.UseNpgsql(connectionString: connectionString);

        return new EShopWriteDbContext(options: optionsBuilder.Options);
    }
}
