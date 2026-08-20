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
using Tnosc.Lib.Shared.Results;

namespace Tnosc.EShop.Server.Application.Payment.Commands.CapturePayment;

/// <summary>
/// Structural validation only — whether the payment exists and whether it can be captured from its
/// current status are decisions the repository lookup and the <c>Payment</c> aggregate own.
/// </summary>
internal sealed class CapturePaymentCommandValidator : IValidator<CapturePaymentCommand>
{
    /// <inheritdoc />
    public ValueTask<Result> ValidateAsync(CapturePaymentCommand request, CancellationToken cancellationToken)
    {
        List<Error> errors = [];

        if (request.PaymentId == Guid.Empty)
        {
            errors.Add(item: PaymentErrors.NotFound(paymentId: request.PaymentId));
        }

        return ValueTask.FromResult<Result>(result: errors);
    }
}
