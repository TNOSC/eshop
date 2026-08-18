// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.FluentUI.AspNetCore.Components;
using Tnosc.EShop.Client.Web.Client.Features.Store.Checkout.Services;
using Tnosc.EShop.Client.Web.Client.Features.Store.Checkout.ViewModels;
using Tnosc.EShop.Client.Web.Client.Infrastructure.Basket;
using Tnosc.EShop.Client.Web.Client.Infrastructure.Errors;
using Tnosc.Lib.Web.Components.Shared;
using Tnosc.Lib.Web.Contracts;
using Tnosc.Lib.Web.Errors;
using Tnosc.Lib.Web.Results;

namespace Tnosc.EShop.Client.Web.Client.Features.Store.Checkout.Pages;

/// <summary>
/// Turns the caller's basket into an order. Requires an <c>Idempotency-Key</c> — see
/// <see cref="_submissionKey"/> for why it lives in component state rather than a
/// <c>DelegatingHandler</c>. Getting this wrong here is the most expensive case in the whole app: a
/// duplicate order is a duplicate charge. Fetching and mapping are <see cref="ICheckoutService"/>'s
/// responsibility.
/// </summary>
public partial class CheckoutPage : ComponentBase
{
    private CheckoutBasketViewModel? _basket;
    private CheckoutCustomerViewModel? _customer;
    private ClientProblem? _problem;
    private ComponentState _state = ComponentState.Loading;
    private bool _isPlacingOrder;

    // A key minted once when the page loads, rotated only once a response arrives — a transport
    // failure keeps the same key, so a retried send replays the original order instead of placing a
    // second one.
    private Guid _submissionKey = Guid.CreateVersion7();

    [Inject]
    public ICheckoutService Service { get; set; } = null!;

    [Inject]
    public BasketState BasketState { get; set; } = null!;

    [Inject]
    public INotificationService Notifications { get; set; } = null!;

    [Inject]
    public NavigationManager Navigation { get; set; } = null!;

    protected override Task OnInitializedAsync() => LoadAsync();

    private async Task LoadAsync()
    {
        _state = ComponentState.Loading;

        CheckoutLoadResult result = await Service.LoadAsync(cancellationToken: CancellationToken.None);

        _basket = result.Basket;
        _customer = result.Customer;
        _problem = result.Problem;

        _state = ComponentState.Content;
    }

    private async Task PlaceOrderAsync()
    {
        _isPlacingOrder = true;

        try
        {
            ClientResult<Guid> result = await Service.PlaceOrderAsync(
                idempotencyKey: _submissionKey,
                cancellationToken: CancellationToken.None);

            // A response arrived — success or business failure — so the next attempt is a new
            // logical request and gets a new key.
            _submissionKey = Guid.CreateVersion7();

            if (result.IsSuccess)
            {
                BasketState.SetItemCount(itemCount: 0);
                Navigation.NavigateTo(uri: $"{Routes.Store.OrderDetail(result.Value)}?placed=true");
                return;
            }

            if (result.Problem!.Status == 409)
            {
                await Notifications.ShowWarningToastAsync(
                    title: "Basket changed",
                    message: "Your basket changed since you started checkout. It has been refreshed below.");
                await LoadAsync();
                return;
            }

            await NotificationExtensions.NotifyFailureAsync(
                problem: result.Problem,
                notifications: Notifications,
                navigation: Navigation,
                humanize: ErrorCodeMessages.Humanize);
        }
        catch (HttpRequestException)
        {
            // No response arrived — the key is deliberately NOT rotated here, so a retry replays
            // this exact request instead of placing a second order.
            await Notifications.ShowErrorToastAsync(
                title: "Something went wrong",
                message: "Could not reach the server. Try again.");
        }
        finally
        {
            _isPlacingOrder = false;
        }
    }
}
