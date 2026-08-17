// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System.Globalization;
using Microsoft.AspNetCore.Components;

namespace Tnosc.EShop.Client.Web.Client.Features.Shared;

public partial class MoneyDisplay : ComponentBase
{
    [Parameter]
    [EditorRequired]
    public decimal Amount { get; set; }

    [Parameter]
    [EditorRequired]
    public string Currency { get; set; } = string.Empty;

    private string FormattedAmount => Amount.ToString(format: "N2", provider: CultureInfo.InvariantCulture);
}
