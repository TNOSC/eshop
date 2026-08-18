// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Tnosc.EShop.Client.Web.Client.Features.Admin.Services;
using Tnosc.Lib.Web.Components.Shared;
using Tnosc.Lib.Web.Contracts;

namespace Tnosc.EShop.Client.Web.Client.Features.Admin.Pages;

/// <summary>The back-office landing page: tiles linking to the Catalog and Identity consoles.
/// Fetching the tile counts is <see cref="IAdminDashboardService"/>'s responsibility.</summary>
public partial class AdminDashboardPage : ComponentBase
{
    private ComponentState _state = ComponentState.Loading;
    private long? _productCount;
    private long? _customerCount;
    private ClientProblem? _productsProblem;
    private ClientProblem? _customersProblem;

    [Inject]
    public IAdminDashboardService Service { get; set; } = null!;

    protected override async Task OnInitializedAsync()
    {
        AdminDashboardCounts counts = await Service.LoadCountsAsync(cancellationToken: CancellationToken.None);

        _productCount = counts.ProductCount;
        _productsProblem = counts.ProductsProblem;
        _customerCount = counts.CustomerCount;
        _customersProblem = counts.CustomersProblem;

        _state = ComponentState.Content;
    }
}
