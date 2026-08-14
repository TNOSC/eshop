// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System.Collections.Generic;

namespace Tnosc.EShop.Server.Tests.Acceptance.Contracts;

/// <summary>
/// One page of catalogue search results.
/// </summary>
/// <param name="Items">The products on this page.</param>
public sealed record PagedProducts(IReadOnlyList<ProductSummary> Items);
