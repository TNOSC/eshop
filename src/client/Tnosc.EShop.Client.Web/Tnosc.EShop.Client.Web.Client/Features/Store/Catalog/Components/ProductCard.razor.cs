// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using Microsoft.AspNetCore.Components;
using Tnosc.EShop.Client.Web.Contracts.Catalog;

namespace Tnosc.EShop.Client.Web.Client.Features.Store.Catalog.Components;

public partial class ProductCard : ComponentBase
{
    [Parameter]
    [EditorRequired]
    public required ProductSummary Product { get; set; }
}
