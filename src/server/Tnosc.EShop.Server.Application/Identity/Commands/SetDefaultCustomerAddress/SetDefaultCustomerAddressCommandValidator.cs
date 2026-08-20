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
using Tnosc.Lib.Shared.Results;

namespace Tnosc.EShop.Server.Application.Identity.Commands.SetDefaultCustomerAddress;

/// <summary>
/// Structural validation only — the caller's identity and the raw address identifier.
/// </summary>
internal sealed class SetDefaultCustomerAddressCommandValidator : IValidator<SetDefaultCustomerAddressCommand>
{
    /// <inheritdoc />
    public ValueTask<Result> ValidateAsync(SetDefaultCustomerAddressCommand request, CancellationToken cancellationToken)
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
