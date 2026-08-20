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

namespace Tnosc.EShop.Server.Application.Identity.Commands.AdminAddCustomerAddress;

/// <summary>
/// Structural validation only — that the target customer identifier arrived. Every rule about the
/// address itself belongs to <see cref="Address"/>.
/// </summary>
internal sealed class AdminAddCustomerAddressCommandValidator : IValidator<AdminAddCustomerAddressCommand>
{
    /// <inheritdoc />
    public ValueTask<Result> ValidateAsync(AdminAddCustomerAddressCommand request, CancellationToken cancellationToken)
    {
        List<Error> errors = [];

        if (request.CustomerId == Guid.Empty)
        {
            errors.Add(item: CustomerErrors.IdRequired);
        }

        return ValueTask.FromResult<Result>(result: errors);
    }
}
