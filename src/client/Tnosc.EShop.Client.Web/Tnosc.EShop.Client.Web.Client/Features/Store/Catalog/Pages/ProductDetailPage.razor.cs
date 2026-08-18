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
using Tnosc.EShop.Client.Web.Client.Features.Store.Catalog.Services;
using Tnosc.EShop.Client.Web.Client.Features.Store.Catalog.ViewModels;
using Tnosc.EShop.Client.Web.Client.Infrastructure.Basket;
using Tnosc.EShop.Client.Web.Client.Infrastructure.Errors;
using Tnosc.Lib.Web.Components.Shared;
using Tnosc.Lib.Web.Contracts;
using Tnosc.Lib.Web.Errors;
using Tnosc.Lib.Web.Results;

namespace Tnosc.EShop.Client.Web.Client.Features.Store.Catalog.Pages;

/// <summary>A single product's detail page, reached from <see cref="ProductsPage"/>. Fetching and
/// adding to basket are <see cref="IProductDetailService"/>'s responsibility.</summary>
public partial class ProductDetailPage : ComponentBase
{
    private ProductDetailViewModel? _product;
    private ClientProblem? _problem;
    private ComponentState _state = ComponentState.Loading;
    private bool _isAddingToBasket;
    private int _quantity = 1;

    [Inject]
    public IProductDetailService Service { get; set; } = null!;

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
        _state = ComponentState.Loading;
        _problem = null;

        ClientResult<ProductDetailViewModel> result = await Service.GetProductAsync(
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

        _state = ComponentState.Content;
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
            ClientResult<int> result = await Service.AddToBasketAsync(
                productId: _product.Id,
                quantity: _quantity,
                cancellationToken: CancellationToken.None);

            if (result.IsSuccess)
            {
                BasketState.SetItemCount(itemCount: result.Value);
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
