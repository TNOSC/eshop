// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tnosc.EShop.Client.Web.Client.Features.Admin.Identity.ViewModels;
using Tnosc.EShop.Client.Web.Client.Infrastructure.Api;
using Tnosc.EShop.Client.Web.Contracts.Identity;
using Tnosc.Lib.Web.Contracts;
using Tnosc.Lib.Web.Results;

namespace Tnosc.EShop.Client.Web.Client.Features.Admin.Identity.Services;

/// <inheritdoc cref="IAdminCustomersService" />
internal sealed class AdminCustomersService(IIdentityApi identityApi) : IAdminCustomersService
{
    public async Task<ClientResult<PagedResult<CustomerRowViewModel>>> SearchAsync(int page, int pageSize, CancellationToken cancellationToken)
    {
        ClientResult<PagedResult<CustomerSummary>> result = await identityApi.SearchCustomersAsync(
            search: null,
            isActive: null,
            page: page,
            pageSize: pageSize,
            cancellationToken: cancellationToken);

        if (!result.IsSuccess)
        {
            return ClientResult<PagedResult<CustomerRowViewModel>>.Failure(problem: result.Problem!);
        }

        PagedResult<CustomerRowViewModel> mappedPage = new(
            Items: [.. result.Value.Items.Select(ToViewModel)],
            Page: result.Value.Page,
            PageSize: result.Value.PageSize,
            TotalCount: result.Value.TotalCount,
            TotalPages: result.Value.TotalPages);

        return ClientResult<PagedResult<CustomerRowViewModel>>.Success(value: mappedPage);
    }

    private static CustomerRowViewModel ToViewModel(CustomerSummary customer) =>
        new()
        {
            Id = customer.Id,
            Email = customer.Email,
            FirstName = customer.FirstName,
            LastName = customer.LastName,
            IsActive = customer.IsActive,
        };
}
