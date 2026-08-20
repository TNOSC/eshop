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
using Tnosc.Lib.Shared.Results;

namespace Tnosc.EShop.Server.Application.Catalog.Commands.UpdateProductPrice;

/// <summary>
/// Structural validation only. The currency format and the non-negative amount are invariants owned
/// by <c>Money</c>, and repricing a discontinued product is a rule owned by <c>Product</c>.
/// </summary>
internal sealed class UpdateProductPriceCommandValidator : IValidator<UpdateProductPriceCommand>
{
    /// <inheritdoc />
    public ValueTask<Result> ValidateAsync(UpdateProductPriceCommand request, CancellationToken cancellationToken)
    {
        List<Error> errors = [];

        if (request.ProductId == Guid.Empty)
        {
            errors.Add(item: ProductErrors.IdRequired);
        }

        return ValueTask.FromResult<Result>(result: errors);
    }
}
