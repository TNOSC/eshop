// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tnosc.EShop.Server.Domain.Catalog.Products;
using Tnosc.Lib.Application.Validations;
using Tnosc.Lib.Domain.Results;

namespace Tnosc.EShop.Server.Application.Catalog.Commands.AdjustStock;

/// <summary>
/// Structural validation only. Whether the adjusted stock level may go below zero is an invariant
/// owned by <c>StockQuantity</c>.
/// </summary>
internal sealed class AdjustStockCommandValidator : IValidator<AdjustStockCommand>
{
    /// <inheritdoc />
    public ValueTask<Result> ValidateAsync(AdjustStockCommand request, CancellationToken cancellationToken)
    {
        List<Error> errors = [];

        if (request.ProductId == Guid.Empty)
        {
            errors.Add(item: ProductErrors.IdRequired);
        }

        if (request.Delta == 0)
        {
            errors.Add(item: ProductErrors.StockDeltaRequired);
        }

        return ValueTask.FromResult<Result>(result: errors);
    }
}
