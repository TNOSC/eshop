// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.FluentUI.AspNetCore.Components;
using Tnosc.EShop.Client.Web.Client.Infrastructure.Api;
using Tnosc.EShop.Client.Web.Client.Infrastructure.Basket;
using Tnosc.EShop.Client.Web.Client.Infrastructure.Errors;
using Tnosc.EShop.Client.Web.Contracts.Basket;
using Tnosc.Lib.Web.Components.Shared;
using Tnosc.Lib.Web.Contracts;
using Tnosc.Lib.Web.Errors;
using Tnosc.Lib.Web.Results;
using BasketDto = Tnosc.EShop.Client.Web.Contracts.Basket.Basket;

namespace Tnosc.EShop.Client.Web.Client.Features.Store.Basket.Pages;

/// <summary>The caller's basket: quantities and totals track the server, never local arithmetic.</summary>
public partial class BasketPage : ComponentBase
{
    private BasketDto? _basket;
    private ClientProblem? _problem;
    private ComponentState _state = ComponentState.Loading;

    [Inject]
    public IBasketApi BasketApi { get; set; } = null!;

    [Inject]
    public BasketState BasketState { get; set; } = null!;

    [Inject]
    public IDialogService DialogService { get; set; } = null!;

    [Inject]
    public INotificationService Notifications { get; set; } = null!;

    [Inject]
    public NavigationManager Navigation { get; set; } = null!;

    protected override Task OnInitializedAsync() => LoadBasketAsync();

    private async Task LoadBasketAsync()
    {
        _state = ComponentState.Loading;

        ClientResult<BasketDto> result = await BasketApi.GetBasketAsync(cancellationToken: CancellationToken.None);

        if (result.IsSuccess)
        {
            _basket = result.Value;
            _problem = null;
            BasketState.SetItemCount(itemCount: _basket.Items.Count);
        }
        else
        {
            _problem = result.Problem;
        }

        _state = ComponentState.Content;
    }

    private async Task ChangeQuantityAsync(BasketItem item, int quantity)
    {
        if (quantity == item.Quantity || quantity < 1)
        {
            return;
        }

        ClientResult<BasketDto> result = await BasketApi.ChangeItemQuantityAsync(
            itemId: item.ItemId,
            request: new ChangeBasketItemQuantityRequest(Quantity: quantity),
            cancellationToken: CancellationToken.None);

        if (result.IsSuccess)
        {
            _basket = result.Value;
            BasketState.SetItemCount(itemCount: _basket.Items.Count);
        }
        else
        {
            await NotificationExtensions.NotifyFailureAsync(
                problem: result.Problem!,
                notifications: Notifications,
                navigation: Navigation,
                humanize: ErrorCodeMessages.Humanize);
        }
    }

    private async Task RemoveItemAsync(BasketItem item)
    {
        DialogResult confirmation = await DialogService.ShowConfirmationAsync(
            message: $"Remove {item.ProductName} from your basket?",
            title: "Remove item");

        if (confirmation.Cancelled)
        {
            return;
        }

        ClientResult result = await BasketApi.RemoveItemAsync(
            itemId: item.ItemId,
            cancellationToken: CancellationToken.None);

        if (result.IsSuccess)
        {
            await LoadBasketAsync();
        }
        else
        {
            await NotificationExtensions.NotifyFailureAsync(
                problem: result.Problem!,
                notifications: Notifications,
                navigation: Navigation,
                humanize: ErrorCodeMessages.Humanize);
        }
    }
}
