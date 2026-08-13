// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tnosc.EShop.Server.Domain.Payment.Payments;
using Tnosc.Lib.Application.Validations;
using Tnosc.Lib.Domain.Results;

namespace Tnosc.EShop.Server.Application.Payment.Commands.InitiatePayment;

/// <summary>
/// Structural validation only — the raw order identifier and method name the domain never wraps in a
/// value object on the way in. Whether a payment already exists for the order, whether the amount is
/// positive and whether the method accepts it are all decisions <c>PaymentFactory</c>,
/// <c>Payment</c> and the chosen <c>IPaymentMethodStrategy</c> own.
/// </summary>
internal sealed class InitiatePaymentCommandValidator : IValidator<InitiatePaymentCommand>
{
    /// <inheritdoc />
    public ValueTask<Result> ValidateAsync(InitiatePaymentCommand request, CancellationToken cancellationToken)
    {
        List<Error> errors = [];

        if (request.OrderId == Guid.Empty)
        {
            errors.Add(item: PaymentErrors.OrderRequired);
        }

        if (!Enum.TryParse<PaymentMethod>(value: request.Method, ignoreCase: true, result: out _))
        {
            errors.Add(item: Error.Validation(
                code: "Payment.InvalidMethod",
                description: $"'{request.Method}' is not a recognised payment method."));
        }

        return ValueTask.FromResult<Result>(result: errors);
    }
}
