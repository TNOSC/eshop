// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC). All rights reserved.
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Tnosc.EShop.Server.Application.Catalog.Queries.GetCategories;
using Tnosc.EShop.Server.Infrastructure.Persistence.Catalog.ReadModels;
using Tnosc.EShop.Server.Infrastructure.Persistence.Contexts;
using Tnosc.EShop.Server.Shared.Catalog;
using Tnosc.Lib.Application.Attributes;
using Tnosc.Lib.Application.Queries;
using Tnosc.Lib.Domain.Results;

namespace Tnosc.EShop.Server.Infrastructure.Persistence.Catalog.Queries;

/// <summary>
/// Projects every category from the read context. Categories change rarely and are read on nearly
/// every catalogue page, so the result is cached for five minutes under the <c>catalog</c> tag — the
/// same tag the three write handlers invalidate on success.
/// </summary>
/// <param name="context">The read context.</param>
[Cacheable(300)]
[CacheTag(CacheTags.Catalog)]
internal sealed class GetCategoriesQueryHandler(EShopReadDbContext context)
    : IQueryHandler<GetCategoriesQuery, CategoryDto[]>
{
    /// <inheritdoc />
    public async ValueTask<Result<CategoryDto[]>> HandleAsync(
        GetCategoriesQuery query,
        CancellationToken cancellationToken = default)
    {
        CategoryDto[] categories = await context.Set<CategoryReadModel>()
            .OrderBy(keySelector: readModel => readModel.Name)
            .Select(selector: readModel => new CategoryDto(Id: readModel.Id, Name: readModel.Name))
            .ToArrayAsync(cancellationToken: cancellationToken);

        return categories;
    }
}
