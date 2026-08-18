// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;
using Tnosc.EShop.Client.Web.Client.Infrastructure.Api;
using Tnosc.EShop.Client.Web.Contracts.Identity;
using Tnosc.Lib.Web.Contracts;
using Tnosc.Lib.Web.Results;

namespace Tnosc.EShop.Client.Web.Client.Features.Admin.Identity.Services;

/// <inheritdoc cref="IAdminCustomersService" />
internal sealed class AdminCustomersService(IIdentityApi identityApi) : IAdminCustomersService
{
    public Task<ClientResult<PagedResult<CustomerSummary>>> SearchAsync(int page, int pageSize, CancellationToken cancellationToken) =>
        identityApi.SearchCustomersAsync(
            search: null,
            isActive: null,
            page: page,
            pageSize: pageSize,
            cancellationToken: cancellationToken);
}
