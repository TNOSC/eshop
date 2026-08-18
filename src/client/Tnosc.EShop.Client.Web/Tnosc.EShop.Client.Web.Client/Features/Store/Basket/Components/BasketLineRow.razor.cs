// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using Microsoft.AspNetCore.Components;
using Tnosc.EShop.Client.Web.Client.Features.Store.Basket.ViewModels;

namespace Tnosc.EShop.Client.Web.Client.Features.Store.Basket.Components;

public partial class BasketLineRow : ComponentBase
{
    [Parameter]
    [EditorRequired]
    public required BasketItemViewModel Item { get; set; }

    [Parameter]
    public EventCallback<int> QuantityChanged { get; set; }

    [Parameter]
    public EventCallback Remove { get; set; }
}
