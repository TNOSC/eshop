// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tnosc.EShop.Server.Domain.Identity.Customers;
using Tnosc.Lib.Application.Validations;
using Tnosc.Lib.Domain.Results;

namespace Tnosc.EShop.Server.Application.Identity.Commands.AddCustomerAddress;

/// <summary>
/// Structural validation only — that the caller's identity actually arrived. Every rule about the
/// address itself belongs to <see cref="Address"/>.
/// </summary>
internal sealed class AddCustomerAddressCommandValidator : IValidator<AddCustomerAddressCommand>
{
    /// <inheritdoc />
    public ValueTask<Result> ValidateAsync(AddCustomerAddressCommand request, CancellationToken cancellationToken)
    {
        List<Error> errors = [];

        if (string.IsNullOrWhiteSpace(value: request.ExternalUserId))
        {
            errors.Add(item: ExternalUserIdErrors.Empty);
        }

        return ValueTask.FromResult<Result>(result: errors);
    }
}
