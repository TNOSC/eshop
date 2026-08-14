// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System.Collections.Generic;

namespace Tnosc.EShop.Server.Tests.Acceptance.Contracts;

/// <summary>
/// The caller's basket.
/// </summary>
/// <param name="Items">The basket's lines.</param>
/// <param name="TotalAmount">The basket's total, or <see langword="null"/> when it is empty.</param>
public sealed record Basket(IReadOnlyList<BasketItem> Items, decimal? TotalAmount);
