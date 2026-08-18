// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;

namespace Tnosc.EShop.Client.Web.Client.Features.Admin.Services;

/// <summary>
/// <see cref="Pages.AdminDashboardPage"/>'s component service — the only place that touches
/// <see cref="Tnosc.EShop.Client.Web.Client.Infrastructure.Api.ICatalogApi"/> and
/// <see cref="Tnosc.EShop.Client.Web.Client.Infrastructure.Api.IIdentityApi"/> for the back-office
/// landing page.
/// </summary>
public interface IAdminDashboardService
{
    /// <summary>Loads the product and customer counts shown on the dashboard's tiles.</summary>
    /// <param name="cancellationToken">The token observed while the call is in flight.</param>
    Task<AdminDashboardCounts> LoadCountsAsync(CancellationToken cancellationToken);
}
