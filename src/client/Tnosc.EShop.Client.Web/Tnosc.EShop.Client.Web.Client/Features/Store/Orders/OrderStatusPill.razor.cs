// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using Microsoft.AspNetCore.Components;

namespace Tnosc.EShop.Client.Web.Client.Features.Store.Orders;

public partial class OrderStatusPill : ComponentBase
{
    [Parameter, EditorRequired]
    public required string Status { get; set; }

    private string ColorToken => Status.ToUpperInvariant() switch
    {
        "CANCELLED" => "var(--eshop-status-bad)",
        "CONFIRMED" or "PAID" => "var(--eshop-status-good)",
        _ => "var(--eshop-status-neutral)",
    };
}
