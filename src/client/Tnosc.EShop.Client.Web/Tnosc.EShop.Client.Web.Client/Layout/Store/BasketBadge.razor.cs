// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Tnosc.EShop.Client.Web.Client.Infrastructure.Api;
using Tnosc.EShop.Client.Web.Client.Infrastructure.Basket;
using Tnosc.EShop.Client.Web.Contracts.Basket;
using Tnosc.Lib.Web.Results;
using BasketDto = Tnosc.EShop.Client.Web.Contracts.Basket.Basket;

namespace Tnosc.EShop.Client.Web.Client.Layout.Store;

/// <summary>
/// Shows the caller's basket line count, kept in sync with <see cref="BasketState"/> so any page
/// that mutates the basket updates the header without a reference to this component.
/// </summary>
public partial class BasketBadge : ComponentBase, IDisposable
{
    [Inject]
    public IBasketApi BasketApi { get; set; } = null!;

    [Inject]
    public BasketState BasketState { get; set; } = null!;

    [CascadingParameter]
    public Task<AuthenticationState>? AuthenticationStateTask { get; set; }

    protected override void OnInitialized() => BasketState.Changed += OnBasketChanged;

    protected override async Task OnInitializedAsync()
    {
        if (AuthenticationStateTask is null)
        {
            return;
        }

        AuthenticationState authenticationState = await AuthenticationStateTask;

        if (authenticationState.User.Identity?.IsAuthenticated != true)
        {
            return;
        }

        ClientResult<BasketDto> result = await BasketApi.GetBasketAsync(cancellationToken: CancellationToken.None);

        if (result.IsSuccess)
        {
            BasketState.SetItemCount(itemCount: result.Value.Items.Count);
        }
    }

    public void Dispose() => BasketState.Changed -= OnBasketChanged;

    private void OnBasketChanged(object? sender, EventArgs e) => StateHasChanged();
}
