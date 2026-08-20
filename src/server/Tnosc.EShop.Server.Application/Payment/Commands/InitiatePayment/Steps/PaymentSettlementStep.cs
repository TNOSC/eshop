// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;
using Tnosc.EShop.Server.Application.Payment.Ports;
using Tnosc.EShop.Server.Domain.Payment.Payments.Strategies;
using Tnosc.Lib.Shared.Results;
using PaymentAggregate = Tnosc.EShop.Server.Domain.Payment.Payments.Payment;

namespace Tnosc.EShop.Server.Application.Payment.Commands.InitiatePayment.Steps;

/// <param name="gateway">The external gateway this step settles through.</param>
internal sealed class PaymentSettlementStep(IPaymentGateway gateway) : IPaymentSettlementStep
{
    /// <inheritdoc />
    public async ValueTask<Result> SettleAsync(
        PaymentAggregate payment,
        PaymentPlan plan,
        string? paymentReference,
        CancellationToken cancellationToken = default)
    {
        if (plan.RequiresAuthorization)
        {
            return await AuthorizeAsync(payment: payment, plan: plan, paymentReference: paymentReference, cancellationToken: cancellationToken);
        }

        if (plan.CapturesImmediately)
        {
            return await CaptureAsync(payment: payment, plan: plan, paymentReference: paymentReference, cancellationToken: cancellationToken);
        }

        // Cash on delivery: nothing to do yet. The gateway is not contacted until CapturePayment runs
        // at delivery time, and the payment stays Pending until then.
        return Result.Success();
    }

    private async ValueTask<Result> AuthorizeAsync(
        PaymentAggregate payment,
        PaymentPlan plan,
        string? paymentReference,
        CancellationToken cancellationToken)
    {
        GatewayAuthorizationResult authorization = await gateway.AuthorizeAsync(
            request: new GatewayAuthorizationRequest(
                PaymentId: payment.Id.Value,
                OrderId: payment.OrderId,
                Amount: plan.Amount.Amount,
                Currency: plan.Amount.Currency,
                PaymentReference: paymentReference),
            cancellationToken: cancellationToken);

        return authorization.IsApproved
            ? payment.Authorize(gatewayReference: authorization.AuthorizationId!)
            : payment.Fail(reason: authorization.DeclineReason ?? "Declined by the payment gateway.");
    }

    private async ValueTask<Result> CaptureAsync(
        PaymentAggregate payment,
        PaymentPlan plan,
        string? paymentReference,
        CancellationToken cancellationToken)
    {
        GatewayCaptureResult capture = await gateway.CaptureAsync(
            request: new GatewayCaptureRequest(
                PaymentId: payment.Id.Value,
                OrderId: payment.OrderId,
                Amount: plan.Amount.Amount,
                Currency: plan.Amount.Currency,
                GatewayReference: null,
                PaymentReference: paymentReference),
            cancellationToken: cancellationToken);

        return capture.IsSuccessful
            ? payment.Capture(gatewayReference: capture.CaptureId!)
            : payment.Fail(reason: capture.FailureReason ?? "Capture declined by the payment gateway.");
    }
}
