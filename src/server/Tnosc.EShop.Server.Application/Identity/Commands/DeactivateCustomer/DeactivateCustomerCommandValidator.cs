// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tnosc.EShop.Server.Domain.Identity.Customers;
using Tnosc.Lib.Application.Validations;
using Tnosc.Lib.Domain.Results;

namespace Tnosc.EShop.Server.Application.Identity.Commands.DeactivateCustomer;

/// <summary>
/// Structural validation only — that the target customer identifier arrived.
/// </summary>
internal sealed class DeactivateCustomerCommandValidator : IValidator<DeactivateCustomerCommand>
{
    /// <inheritdoc />
    public ValueTask<Result> ValidateAsync(DeactivateCustomerCommand request, CancellationToken cancellationToken)
    {
        List<Error> errors = [];

        if (request.CustomerId == Guid.Empty)
        {
            errors.Add(item: CustomerErrors.IdRequired);
        }

        return ValueTask.FromResult<Result>(result: errors);
    }
}
