// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;

namespace Tnosc.EShop.Server.Tests.Acceptance.Contracts;

/// <summary>
/// One line of the caller's basket.
/// </summary>
/// <param name="ProductId">The product on this line.</param>
/// <param name="Quantity">How many units of it the line holds.</param>
public sealed record BasketItem(Guid ProductId, int Quantity);
