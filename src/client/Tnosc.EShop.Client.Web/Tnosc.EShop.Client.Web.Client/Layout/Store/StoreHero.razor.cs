// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using Microsoft.AspNetCore.Components;

namespace Tnosc.EShop.Client.Web.Client.Layout.Store;

public partial class StoreHero : ComponentBase
{
    [Parameter]
    [EditorRequired]
    public required string Title { get; set; }

    [Parameter]
    public string? Subtitle { get; set; }

    [Parameter]
    public bool Tall { get; set; }
}
