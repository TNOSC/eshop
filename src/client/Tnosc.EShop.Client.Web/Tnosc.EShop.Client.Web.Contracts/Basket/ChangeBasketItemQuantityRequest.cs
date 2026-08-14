// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

namespace Tnosc.EShop.Client.Web.Contracts.Basket;

/// <summary>The request body to change a basket item's quantity.</summary>
public sealed record ChangeBasketItemQuantityRequest(int Quantity);
