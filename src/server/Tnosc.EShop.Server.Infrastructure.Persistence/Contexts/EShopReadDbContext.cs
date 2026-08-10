// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC). All rights reserved.
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Tnosc.Lib.Infrastructure.Persistence.Contexts;

namespace Tnosc.EShop.Server.Infrastructure.Persistence.Contexts;

public sealed class EShopReadDbContext(DbContextOptions<EShopReadDbContext> options)
    : ReadDbContextBase(options)
{
    protected override Assembly ConfigurationAssembly => AssemblyReference.Assembly;
}
