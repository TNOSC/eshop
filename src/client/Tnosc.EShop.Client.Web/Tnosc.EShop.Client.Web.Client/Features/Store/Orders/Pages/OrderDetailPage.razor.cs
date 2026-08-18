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
using Tnosc.EShop.Client.Web.Client.Features.Store.Orders.Services;
using Tnosc.EShop.Client.Web.Client.Infrastructure.Errors;
using Tnosc.EShop.Client.Web.Contracts.Ordering;
using Tnosc.Lib.Web.Components.Shared;
using Tnosc.Lib.Web.Contracts;
using Tnosc.Lib.Web.Errors;
using Tnosc.Lib.Web.Results;

namespace Tnosc.EShop.Client.Web.Client.Features.Store.Orders.Pages;

/// <summary>One of the caller's own orders, reached from <c>MyOrdersPage</c> or straight from
/// checkout when <see cref="Placed"/> is set. Fetching and mutation are
/// <see cref="IOrderDetailService"/>'s responsibility.</summary>
public partial class OrderDetailPage : ComponentBase
{
    private Order? _order;
    private ClientProblem? _problem;
    private ComponentState _state = ComponentState.Loading;

    [Inject]
    public IOrderDetailService Service { get; set; } = null!;

    [Inject]
    public IDialogService DialogService { get; set; } = null!;

    [Inject]
    public INotificationService Notifications { get; set; } = null!;

    [Inject]
    public NavigationManager Navigation { get; set; } = null!;

    [Parameter]
    public Guid Id { get; set; }

    [SupplyParameterFromQuery(Name = "placed")]
    [Parameter]
    public bool? Placed { get; set; }

    protected override Task OnParametersSetAsync() => LoadAsync();

    private async Task LoadAsync()
    {
        _state = ComponentState.Loading;

        ClientResult<Order> result = await Service.GetOrderByIdAsync(id: Id, cancellationToken: CancellationToken.None);

        if (result.IsSuccess)
        {
            _order = result.Value;
            _problem = null;
        }
        else
        {
            _problem = result.Problem;
            _order = null;
        }

        _state = ComponentState.Content;
    }

    private async Task ConfirmOrderAsync()
    {
        DialogResult confirmation = await DialogService.ShowConfirmationAsync(
            message: "Confirm this order?",
            title: "Confirm order");

        if (confirmation.Cancelled)
        {
            return;
        }

        ClientResult result = await Service.ConfirmOrderAsync(id: Id, cancellationToken: CancellationToken.None);
        await HandleActionResultAsync(result: result);
    }

    private async Task CancelOrderAsync()
    {
        DialogResult confirmation = await DialogService.ShowConfirmationAsync(
            message: "Cancel this order?",
            title: "Cancel order");

        if (confirmation.Cancelled)
        {
            return;
        }

        ClientResult result = await Service.CancelOrderAsync(id: Id, cancellationToken: CancellationToken.None);
        await HandleActionResultAsync(result: result);
    }

    private async Task HandleActionResultAsync(ClientResult result)
    {
        if (result.IsSuccess)
        {
            await LoadAsync();
            return;
        }

        await NotificationExtensions.NotifyFailureAsync(
            problem: result.Problem!,
            notifications: Notifications,
            navigation: Navigation,
            humanize: ErrorCodeMessages.Humanize);
    }
}
