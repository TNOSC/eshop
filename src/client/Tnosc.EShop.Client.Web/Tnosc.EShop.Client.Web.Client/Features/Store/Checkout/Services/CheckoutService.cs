// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tnosc.EShop.Client.Web.Client.Features.Store.Checkout.ViewModels;
using Tnosc.EShop.Client.Web.Client.Infrastructure.Api;
using Tnosc.EShop.Client.Web.Contracts.Identity;
using Tnosc.Lib.Web.Contracts;
using Tnosc.Lib.Web.Results;
using BasketDto = Tnosc.EShop.Client.Web.Contracts.Basket.Basket;
using BasketItemDto = Tnosc.EShop.Client.Web.Contracts.Basket.BasketItem;

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
            return new CheckoutLoadResult(
                Basket: ToViewModel(basket: basketResult.Value),
                Customer: ToViewModel(customer: customerResult.Value),
                Problem: null);
        }

        ClientProblem? problem = basketResult.Problem ?? customerResult.Problem;
        return new CheckoutLoadResult(Basket: null, Customer: null, Problem: problem);
    }

    public Task<ClientResult<Guid>> PlaceOrderAsync(Guid idempotencyKey, CancellationToken cancellationToken) =>
        orderingApi.PlaceOrderAsync(idempotencyKey: idempotencyKey, cancellationToken: cancellationToken);

    private static CheckoutBasketViewModel ToViewModel(BasketDto basket) =>
        new()
        {
            Items = [.. basket.Items.Select(ToViewModel)],
            TotalAmount = basket.TotalAmount,
            TotalCurrency = basket.TotalCurrency,
        };

    private static CheckoutBasketItemViewModel ToViewModel(BasketItemDto item) =>
        new()
        {
            ItemId = item.ItemId,
            ProductName = item.ProductName,
            UnitPriceAmount = item.UnitPriceAmount,
            UnitPriceCurrency = item.UnitPriceCurrency,
            Quantity = item.Quantity,
        };

    private static CheckoutCustomerViewModel ToViewModel(Customer customer) =>
        new()
        {
            DefaultAddressId = customer.DefaultAddressId,
            Addresses = [.. customer.Addresses.Select(ToViewModel)],
        };

    private static CheckoutAddressViewModel ToViewModel(CustomerAddress address) =>
        new()
        {
            Id = address.Id,
            Street = address.Street,
            City = address.City,
            PostalCode = address.PostalCode,
            Country = address.Country,
        };
}
