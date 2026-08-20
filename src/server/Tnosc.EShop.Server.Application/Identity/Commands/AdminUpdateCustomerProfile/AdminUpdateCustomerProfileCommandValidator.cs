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

namespace Tnosc.EShop.Server.Application.Identity.Commands.AdminUpdateCustomerProfile;

/// <summary>
/// Structural validation only — that the target customer identifier arrived. Name and phone
/// invariants belong to <see cref="PersonName"/> and <see cref="PhoneNumber"/>.
/// </summary>
internal sealed class AdminUpdateCustomerProfileCommandValidator : IValidator<AdminUpdateCustomerProfileCommand>
{
    /// <inheritdoc />
    public ValueTask<Result> ValidateAsync(AdminUpdateCustomerProfileCommand request, CancellationToken cancellationToken)
    {
        List<Error> errors = [];

        if (request.CustomerId == Guid.Empty)
        {
            errors.Add(item: CustomerErrors.IdRequired);
        }

        return ValueTask.FromResult<Result>(result: errors);
    }
}
