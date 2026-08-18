// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;
using Tnosc.EShop.Client.Web.Client.Infrastructure.Api;
using Tnosc.EShop.Client.Web.Contracts.Identity;
using Tnosc.Lib.Web.Contracts;
using Tnosc.Lib.Web.Results;
using BasketDto = Tnosc.EShop.Client.Web.Contracts.Basket.Basket;

namespace Tnosc.EShop.Client.Web.Client.Features.Store.Checkout.Services;

/// <inheritdoc cref="ICheckoutService" />
internal sealed class CheckoutService(IBasketApi basketApi, IIdentityApi identityApi, IOrderingApi orderingApi) : ICheckoutService
{
    public async Task<CheckoutLoadResult> LoadAsync(CancellationToken cancellationToken)
    {
        ClientResult<BasketDto> basketResult = await basketApi.GetBasketAsync(cancellationToken: cancellationToken);
        ClientResult<Customer> customerResult = await identityApi.GetMeAsync(cancellationToken: cancellationToken);

        if (basketResult.IsSuccess && customerResult.IsSuccess)
        {
            return new CheckoutLoadResult(Basket: basketResult.Value, Customer: customerResult.Value, Problem: null);
        }

        ClientProblem? problem = basketResult.Problem ?? customerResult.Problem;
        return new CheckoutLoadResult(Basket: null, Customer: null, Problem: problem);
    }

    public Task<ClientResult<Guid>> PlaceOrderAsync(Guid idempotencyKey, CancellationToken cancellationToken) =>
        orderingApi.PlaceOrderAsync(idempotencyKey: idempotencyKey, cancellationToken: cancellationToken);
}
