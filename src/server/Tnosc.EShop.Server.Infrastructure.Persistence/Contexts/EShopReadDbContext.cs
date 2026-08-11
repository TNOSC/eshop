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

public sealed class EShopReadDbContext(DbContextOptions<EShopReadDbContext> options)
    : ReadDbContextBase(options)
{
    protected override Assembly ConfigurationAssembly => AssemblyReference.Assembly;

    /// <inheritdoc />
    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        base.ConfigureConventions(configurationBuilder: configurationBuilder);

        // Kept symmetrical with the write context so a read model that ever does surface a
        // strongly-typed id converts identically on both sides. See EShopWriteDbContext for why the
        // Domain assembly has to be scanned explicitly.
        configurationBuilder.ApplyEntityIdConversions(assemblies: DomainAssemblyReference.Assembly);
    }
}
