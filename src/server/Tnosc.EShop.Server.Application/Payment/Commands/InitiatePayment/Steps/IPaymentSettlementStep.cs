// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;
using Tnosc.EShop.Server.Domain.Payment.Payments.Strategies;
using Tnosc.Lib.Shared.Results;
using PaymentAggregate = Tnosc.EShop.Server.Domain.Payment.Payments.Payment;

namespace Tnosc.EShop.Server.Application.Payment.Commands.InitiatePayment.Steps;

/// <summary>
/// Carries out a <see cref="PaymentPlan"/> against the gateway and applies its outcome to the
/// <see cref="PaymentAggregate"/> aggregate.
/// </summary>
/// <remarks>
/// A step, not a handler: which gateway call to make — authorize, capture immediately, or none at
/// all for a method that settles later — is dictated entirely by the <see cref="PaymentPlan"/> the
/// domain already decided, so branching on its flags here is mechanical dispatch, not a business
/// decision. Keeping it out of <c>InitiatePaymentCommandHandler</c> itself is what keeps that class
/// free of the <c>if</c>s <c>NoBusinessBranchingTests</c> would otherwise flag.
/// </remarks>
public interface IPaymentSettlementStep
{
    /// <summary>
    /// Executes <paramref name="plan"/> for <paramref name="payment"/>, calling the gateway when the
    /// plan calls for it and applying the outcome to the aggregate.
    /// </summary>
    /// <param name="payment">The payment to settle.</param>
    /// <param name="plan">The settlement plan the chosen payment method produced.</param>
    /// <param name="paymentReference">The caller-supplied funding-source reference, when any.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>Success once the aggregate has been updated, or the aggregate's own transition error.</returns>
    ValueTask<Result> SettleAsync(
        PaymentAggregate payment,
        PaymentPlan plan,
        string? paymentReference,
        CancellationToken cancellationToken = default);
}
