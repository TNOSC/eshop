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
using Tnosc.Lib.Shared.Results;

namespace Tnosc.EShop.Server.Application.Identity.Commands.ProvisionCustomer;

/// <summary>
/// Structural validation only — that the two values lifted off the caller's token actually arrived.
/// </summary>
/// <remarks>
/// Everything else is a domain invariant already enforced where it belongs: email format and
/// normalisation in <see cref="Email"/>, name presence and length in <see cref="PersonName"/>, phone
/// format in <see cref="PhoneNumber"/>, and email uniqueness in <see cref="CustomerFactory"/>.
/// Re-checking any of them here would let this validator drift out of sync with the rule the domain
/// actually enforces.
/// </remarks>
internal sealed class ProvisionCustomerCommandValidator : IValidator<ProvisionCustomerCommand>
{
    /// <inheritdoc />
    public ValueTask<Result> ValidateAsync(ProvisionCustomerCommand request, CancellationToken cancellationToken)
    {
        List<Error> errors = [];

        if (string.IsNullOrWhiteSpace(value: request.ExternalUserId))
        {
            errors.Add(item: ExternalUserIdErrors.Empty);
        }

        if (string.IsNullOrWhiteSpace(value: request.Email))
        {
            errors.Add(item: EmailErrors.Empty);
        }

        return ValueTask.FromResult<Result>(result: errors);
    }
}
