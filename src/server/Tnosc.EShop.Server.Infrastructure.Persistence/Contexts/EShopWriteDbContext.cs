// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Tnosc.Lib.Infrastructure.Persistence.Contexts;
using Tnosc.Lib.Infrastructure.Persistence.Conventions;
using DomainAssemblyReference = Tnosc.EShop.Server.Domain.AssemblyReference;

namespace Tnosc.EShop.Server.Infrastructure.Persistence.Contexts;

public sealed class EShopWriteDbContext(DbContextOptions<EShopWriteDbContext> options)
    : WriteDbContextBase(options)
{
    protected override Assembly ConfigurationAssembly => AssemblyReference.Assembly;

    /// <inheritdoc />
    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        base.ConfigureConventions(configurationBuilder: configurationBuilder);

        // The base class scans ConfigurationAssembly, which holds the EF configurations — but the
        // strongly-typed ids themselves live in Server.Domain, which cannot reference EF Core. Scanning
        // it here is what makes ProductId/BrandId/CategoryId convert without a single per-property
        // HasConversion call, foreign keys included.
        configurationBuilder.ApplyEntityIdConversions(assemblies: DomainAssemblyReference.Assembly);
    }
}
