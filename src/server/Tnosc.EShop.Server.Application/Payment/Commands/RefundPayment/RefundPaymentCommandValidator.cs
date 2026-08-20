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

namespace Tnosc.EShop.Server.Application.Payment.Commands.RefundPayment;

/// <summary>
/// Structural validation only — whether the payment exists and whether it can be refunded from its
/// current status are decisions the repository lookup and the <c>Payment</c> aggregate own.
/// </summary>
internal sealed class RefundPaymentCommandValidator : IValidator<RefundPaymentCommand>
{
    /// <inheritdoc />
    public ValueTask<Result> ValidateAsync(RefundPaymentCommand request, CancellationToken cancellationToken)
    {
        List<Error> errors = [];

        if (request.PaymentId == Guid.Empty)
        {
            errors.Add(item: PaymentErrors.NotFound(paymentId: request.PaymentId));
        }

        return ValueTask.FromResult<Result>(result: errors);
    }
}
