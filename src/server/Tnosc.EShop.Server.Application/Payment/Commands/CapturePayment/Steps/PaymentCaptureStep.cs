// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;
using Tnosc.EShop.Server.Application.Payment.Ports;
using Tnosc.Lib.Domain.Results;
using PaymentAggregate = Tnosc.EShop.Server.Domain.Payment.Payments.Payment;

namespace Tnosc.EShop.Server.Application.Payment.Commands.CapturePayment.Steps;

/// <param name="gateway">The external gateway this step captures through.</param>
internal sealed class PaymentCaptureStep(IPaymentGateway gateway) : IPaymentCaptureStep
{
    /// <inheritdoc />
    public async ValueTask<Result> CaptureAsync(PaymentAggregate payment, CancellationToken cancellationToken = default)
    {
        GatewayCaptureResult capture = await gateway.CaptureAsync(
            request: new GatewayCaptureRequest(
                PaymentId: payment.Id.Value,
                OrderId: payment.OrderId,
                Amount: payment.Amount.Amount,
                Currency: payment.Amount.Currency,
                GatewayReference: payment.GatewayReference,
                PaymentReference: null),
            cancellationToken: cancellationToken);

        return capture.IsSuccessful
            ? payment.Capture(gatewayReference: capture.CaptureId!)
            : payment.Fail(reason: capture.FailureReason ?? "Capture declined by the payment gateway.");
    }
}
