// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Tnosc.EShop.Server.Application.Payment.Ports;
using Tnosc.Lib.Application.Exceptions;

namespace Tnosc.EShop.Server.Infrastructure.External.Payment;

/// <summary>
/// A simulated payment gateway behind an <see cref="HttpClient"/> registered with
/// <c>Microsoft.Extensions.Http.Resilience</c>, standing in for a real one this reference
/// implementation does not have.
/// </summary>
/// <remarks>
/// <para>
/// <strong>The exception rule, applied literally.</strong> A technical failure — <see cref="FakeCardNumbers.Timeout"/>
/// or a 5xx a real gateway might return — <em>throws</em> <see cref="TransientFailureException"/>
/// (<c>IsRetriable = true</c>); a malformed request throws <see cref="InvalidRequestException"/>.
/// Neither is ever wrapped in a <c>Result</c> here — that translation is
/// <c>ExceptionDecorator</c>'s job, one layer up, and <c>[Retry(3)]</c> on the command handlers is
/// what actually retries the retriable ones. A <em>declined</em> card is the opposite: a business
/// outcome the gateway reported as data, carried back on <see cref="GatewayAuthorizationResult"/> /
/// <see cref="GatewayCaptureResult"/> for the <c>Payment</c> aggregate to interpret.
/// </para>
/// <para>
/// <strong>No real network call.</strong> There is no external gateway for this reference
/// implementation to actually reach, so nothing here calls out over <paramref name="httpClient"/> —
/// swapping in a real gateway is a matter of replacing the body of the three methods below with real
/// HTTP calls issued through the same client. The client is still injected and registered with a
/// standard resilience handler today, so the DI wiring a real adapter would need is already correct
/// and exercised; its <see cref="HttpClient.BaseAddress"/> is folded into the failure messages below
/// so it is not merely an unused constructor parameter.
/// </para>
/// </remarks>
/// <param name="httpClient">
/// The resilience-wrapped client a real gateway call would go out over.
/// </param>
internal sealed class FakePaymentGateway(HttpClient httpClient) : IPaymentGateway
{
    private static readonly TimeSpan SimulatedLatency = TimeSpan.FromMilliseconds(value: 25);

    /// <inheritdoc />
    public async ValueTask<GatewayAuthorizationResult> AuthorizeAsync(
        GatewayAuthorizationRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(argument: request);

        await SimulateNetworkLatencyAsync(cancellationToken: cancellationToken);
        EnsureNotTechnicallyFailing(paymentReference: request.PaymentReference);

        return string.Equals(a: request.PaymentReference, b: FakeCardNumbers.Declined, comparisonType: StringComparison.Ordinal)
            ? new GatewayAuthorizationResult(IsApproved: false, AuthorizationId: null, DeclineReason: "card_declined")
            : new GatewayAuthorizationResult(
                IsApproved: true,
                AuthorizationId: $"auth_{Guid.CreateVersion7()}",
                DeclineReason: null);
    }

    /// <inheritdoc />
    public async ValueTask<GatewayCaptureResult> CaptureAsync(
        GatewayCaptureRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(argument: request);

        await SimulateNetworkLatencyAsync(cancellationToken: cancellationToken);
        EnsureNotTechnicallyFailing(paymentReference: request.PaymentReference);

        return string.Equals(a: request.PaymentReference, b: FakeCardNumbers.Declined, comparisonType: StringComparison.Ordinal)
            ? new GatewayCaptureResult(IsSuccessful: false, CaptureId: null, FailureReason: "card_declined")
            : new GatewayCaptureResult(IsSuccessful: true, CaptureId: $"cap_{Guid.CreateVersion7()}", FailureReason: null);
    }

    /// <inheritdoc />
    public async ValueTask RefundAsync(GatewayRefundRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(argument: request);

        await SimulateNetworkLatencyAsync(cancellationToken: cancellationToken);
    }

    private async ValueTask SimulateNetworkLatencyAsync(CancellationToken cancellationToken) =>
        await Task.Delay(delay: SimulatedLatency, cancellationToken: cancellationToken);

    private void EnsureNotTechnicallyFailing(string? paymentReference)
    {
        if (string.IsNullOrWhiteSpace(value: paymentReference))
        {
            return;
        }

        if (string.Equals(a: paymentReference, b: FakeCardNumbers.Timeout, comparisonType: StringComparison.Ordinal))
        {
            throw new TransientFailureException(
                message: $"Payment gateway at {httpClient.BaseAddress} timed out.",
                correlationId: null,
                inner: null);
        }

        if (string.Equals(a: paymentReference, b: FakeCardNumbers.Malformed, comparisonType: StringComparison.Ordinal))
        {
            throw new InvalidRequestException(
                message: $"Payment gateway at {httpClient.BaseAddress} rejected a malformed funding-source reference.",
                validationErrors: new Dictionary<string, string[]>(comparer: StringComparer.Ordinal)
                {
                    ["paymentReference"] = ["Not a recognised funding-source reference."],
                });
        }
    }
}
