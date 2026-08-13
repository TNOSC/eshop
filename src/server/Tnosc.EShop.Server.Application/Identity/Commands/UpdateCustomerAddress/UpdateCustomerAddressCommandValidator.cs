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

namespace Tnosc.EShop.Server.Application.Identity.Commands.UpdateCustomerAddress;

/// <summary>
/// Structural validation only — the caller's identity, and the raw address identifier the domain
/// never wraps in a value object before the aggregate looks it up.
/// </summary>
internal sealed class UpdateCustomerAddressCommandValidator : IValidator<UpdateCustomerAddressCommand>
{
    /// <inheritdoc />
    public ValueTask<Result> ValidateAsync(UpdateCustomerAddressCommand request, CancellationToken cancellationToken)
    {
        List<Error> errors = [];

        if (string.IsNullOrWhiteSpace(value: request.ExternalUserId))
        {
            errors.Add(item: ExternalUserIdErrors.Empty);
        }

        if (request.AddressId == Guid.Empty)
        {
            errors.Add(item: CustomerErrors.AddressIdRequired);
        }

        return ValueTask.FromResult<Result>(result: errors);
    }
}
