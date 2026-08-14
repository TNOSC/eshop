// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

namespace Tnosc.EShop.Client.Web.Contracts.Catalog;

/// <summary>The request body to adjust a product's stock by a signed delta.</summary>
public sealed record AdjustStockRequest(int Delta);
