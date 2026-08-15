// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Tnosc.EShop.Client.Web.Client.Infrastructure.Api;
using Tnosc.EShop.Client.Web.Contracts.Catalog;
using Tnosc.Lib.Web.Api;
using Tnosc.Lib.Web.Contracts;

namespace Tnosc.EShop.Client.Web.Client.Features.Store.Catalog;

/// <summary>A single product's detail page, reached from <see cref="Products"/>.</summary>
public partial class ProductDetail : ComponentBase
{
    private Product? _product;
    private ApiProblem? _problem;
    private bool _isLoading = true;
    private int _quantity = 1;

    [Inject]
    public ICatalogApi CatalogApi { get; set; } = null!;

    [Parameter]
    public Guid Id { get; set; }

    protected override async Task OnParametersSetAsync()
    {
        _isLoading = true;
        _problem = null;

        ApiResult<Product> result = await CatalogApi.GetProductAsync(
            id: Id,
            cancellationToken: CancellationToken.None);

        if (result.IsSuccess)
        {
            _product = result.Value;
            _quantity = 1;
        }
        else
        {
            _problem = result.Problem;
            _product = null;
        }

        _isLoading = false;
    }
}
