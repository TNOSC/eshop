// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using Microsoft.AspNetCore.Components;

namespace Tnosc.EShop.Client.Web.Client.Features.Shared;

public partial class ProductImage : ComponentBase
{
    [Parameter]
    [EditorRequired]
    public string Sku { get; set; } = string.Empty;

    [Parameter]
    [EditorRequired]
    public string Alt { get; set; } = string.Empty;
}
