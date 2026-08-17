// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.FluentUI.AspNetCore.Components;
using Tnosc.EShop.Client.Web.Client.Infrastructure.Api;
using Tnosc.EShop.Client.Web.Client.Infrastructure.Basket;
using Tnosc.EShop.Client.Web.Client.Infrastructure.Errors;
using Tnosc.EShop.Client.Web.Contracts.Basket;
using Tnosc.EShop.Client.Web.Contracts.Catalog;
using Tnosc.Lib.Web.Api;
using Tnosc.Lib.Web.Contracts;
using Tnosc.Lib.Web.Errors;
using BasketDto = Tnosc.EShop.Client.Web.Contracts.Basket.Basket;

namespace Tnosc.EShop.Client.Web.Client.Features.Store.Catalog;

/// <summary>A single product's detail page, reached from <see cref="Products"/>.</summary>
public partial class ProductDetail : ComponentBase
{
    private Product? _product;
    private ApiProblem? _problem;
    private bool _isLoading = true;
    private bool _isAddingToBasket;
    private int _quantity = 1;

    [Inject]
    public ICatalogApi CatalogApi { get; set; } = null!;

    [Inject]
    public IBasketApi BasketApi { get; set; } = null!;

    [Inject]
    public BasketState BasketState { get; set; } = null!;

    [Inject]
    public INotificationService Notifications { get; set; } = null!;

    [Inject]
    public NavigationManager Navigation { get; set; } = null!;

    [Parameter]
    public Guid Id { get; set; }

    protected override async Task OnParametersSetAsync()
    {
        _isLoading = true;
        _problem = null;

        ApiResult<Product> result = await CatalogApi.GetProductAsync(
            id: Id,
            cancellationToken: CancellationToken.None);

        if (result.IsSuccess)
        {
            _product = result.Value;
            _quantity = 1;
        }
        else
        {
            _problem = result.Problem;
            _product = null;
        }

        _isLoading = false;
    }

    private async Task AddToBasketAsync()
    {
        if (_product is null)
        {
            return;
        }

        _isAddingToBasket = true;

        try
        {
            ApiResult<BasketDto> result = await BasketApi.AddItemAsync(
                request: new AddItemToBasketRequest(ProductId: _product.Id, Quantity: _quantity),
                cancellationToken: CancellationToken.None);

            if (result.IsSuccess)
            {
                BasketState.SetItemCount(itemCount: result.Value.Items.Count);
                await Notifications.ShowSuccessToastAsync(title: "Added to basket", message: _product.Name);
                return;
            }

            await NotificationExtensions.NotifyFailureAsync(
                problem: result.Problem!,
                notifications: Notifications,
                navigation: Navigation,
                humanize: ErrorCodeMessages.Humanize);
        }
        finally
        {
            _isAddingToBasket = false;
        }
    }
}
