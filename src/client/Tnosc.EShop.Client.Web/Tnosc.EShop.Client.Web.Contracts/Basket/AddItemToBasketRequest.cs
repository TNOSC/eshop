// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;

namespace Tnosc.EShop.Client.Web.Contracts.Basket;

/// <summary>The request body to add a product to the current caller's basket.</summary>
public sealed record AddItemToBasketRequest(Guid ProductId, int Quantity);
