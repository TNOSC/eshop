// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;
using Tnosc.Lib.Domain.Results;
using PaymentAggregate = Tnosc.EShop.Server.Domain.Payment.Payments.Payment;

namespace Tnosc.EShop.Server.Application.Payment.Commands.CapturePayment.Steps;

/// <summary>
/// Calls the gateway to capture a payment and applies the outcome to the aggregate.
/// </summary>
/// <remarks>
/// A step, not the handler itself, for the same reason <c>IPaymentSettlementStep</c> is: whether the
/// gateway approved or declined the capture is data the aggregate decides the meaning of, and
/// branching on that outcome here — rather than inside <c>CapturePaymentCommandHandler</c> — is what
/// keeps that class free of the ternary <c>NoBusinessBranchingTests</c> would otherwise flag.
/// </remarks>
public interface IPaymentCaptureStep
{
    /// <summary>
    /// Captures <paramref name="payment"/> through the gateway and applies the outcome.
    /// </summary>
    /// <param name="payment">The payment to capture.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>Success once the aggregate has been updated, or the aggregate's own transition error.</returns>
    ValueTask<Result> CaptureAsync(PaymentAggregate payment, CancellationToken cancellationToken = default);
}
