// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using Tnosc.EShop.Client.Web.Client.Features.Store.Checkout.ViewModels;
using Tnosc.Lib.Web.Contracts;

namespace Tnosc.EShop.Client.Web.Client.Features.Store.Checkout.Services;

/// <summary>The combined outcome of loading the caller's basket and profile for checkout.</summary>
/// <param name="Basket">The caller's basket, when the load succeeded.</param>
/// <param name="Customer">The caller's profile, when the load succeeded.</param>
/// <param name="Problem">The failure, when either call failed.</param>
public sealed record CheckoutLoadResult(CheckoutBasketViewModel? Basket, CheckoutCustomerViewModel? Customer, ClientProblem? Problem)
{
    /// <summary>Gets a value indicating whether both calls succeeded.</summary>
    public bool IsSuccess => Basket is not null && Customer is not null;
}
