// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC). All rights reserved.
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;

namespace Tnosc.EShop.Server.Application.Catalog.Queries.GetCategories;

/// <summary>
/// A catalogue category, projected for reading.
/// </summary>
/// <param name="Id">The category's identifier.</param>
/// <param name="Name">The category's display name.</param>
public sealed record CategoryDto(Guid Id, string Name);
