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

namespace Tnosc.EShop.Server.Application.Catalog.Commands.CreateProduct;

/// <summary>
/// Structural validation only — the two raw identifiers the domain never wraps in a value object, so
/// nothing else guards against them being unset. Name/description length, SKU format, currency format,
/// non-negative price, non-negative stock, and SKU uniqueness are all invariants the domain enforces
/// itself in <see cref="Product.Create"/>, the value objects, and <see cref="ProductFactory"/> — none
/// of them are re-checked here, and re-checking them would let this validator drift out of sync with
/// the rule the aggregate actually enforces.
/// </summary>
internal sealed class CreateProductCommandValidator : IValidator<CreateProductCommand>
{
    /// <inheritdoc />
    public ValueTask<Result> ValidateAsync(CreateProductCommand request, CancellationToken cancellationToken)
    {
        List<Error> errors = [];

        if (request.BrandId == Guid.Empty)
        {
            errors.Add(item: ProductErrors.BrandRequired);
        }

        if (request.CategoryId == Guid.Empty)
        {
            errors.Add(item: ProductErrors.CategoryRequired);
        }

        return ValueTask.FromResult<Result>(result: errors);
    }
}
