// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tnosc.EShop.Server.Domain.Ordering.Orders;
using Tnosc.Lib.Application.Validations;
using Tnosc.Lib.Domain.Results;

namespace Tnosc.EShop.Server.Application.Ordering.Commands.PlaceOrder;

/// <summary>
/// Structural validation only — the one raw identifier the domain never wraps in a value object.
/// Whether the basket has lines, whether the customer has an address, whether stock covers the
/// order and how the total is discounted are all decisions the domain and the workflow steps own,
/// and none of them is re-checked here.
/// </summary>
internal sealed class PlaceOrderCommandValidator : IValidator<PlaceOrderCommand>
{
    /// <inheritdoc />
    public ValueTask<Result> ValidateAsync(PlaceOrderCommand request, CancellationToken cancellationToken)
    {
        List<Error> errors = [];

        if (request.CustomerId == Guid.Empty)
        {
            errors.Add(item: OrderErrors.CustomerRequired);
        }

        return ValueTask.FromResult<Result>(result: errors);
    }
}
