// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;

namespace Tnosc.EShop.Server.Application.Payment.Ports;

/// <summary>
/// The application's contract for an external payment gateway. The application owns this port;
/// <c>Server.Infrastructure.External</c> owns the one adapter that implements it today,
/// <c>FakePaymentGateway</c>.
/// </summary>
/// <remarks>
/// <para>
/// <strong>The exception rule is the whole point of this boundary.</strong> An implementation throws
/// on a technical failure — a timeout or a 5xx becomes <c>TransientFailureException</c>
/// (<c>IsRetriable = true</c>), a 4xx becomes <c>InvalidRequestException</c> — and must never return
/// a <see cref="Tnosc.Lib.Domain.Results.Result"/> for one. <c>ExceptionDecorator</c> maps whatever
/// escapes a command handler into <c>Result.Failure(ErrorType.Unexpected)</c>, and
/// <c>[Retry(3)]</c> on the payment command handlers retries the retriable ones.
/// </para>
/// <para>
/// A declined card is the opposite: a <em>business</em> outcome the gateway reported as data, not a
/// fault. It comes back as a field on <see cref="GatewayAuthorizationResult"/> /
/// <see cref="GatewayCaptureResult"/>, and the <c>Payment</c> aggregate — never this port or its
/// adapter — decides what it means.
/// </para>
/// </remarks>
public interface IPaymentGateway
{
    /// <summary>
    /// Asks the gateway to authorize (reserve) funds for a payment.
    /// </summary>
    /// <param name="request">The authorization request.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>Whether the gateway approved the authorization, and its reference or decline reason.</returns>
    /// <exception cref="Tnosc.Lib.Application.Exceptions.TransientFailureException">
    /// The gateway timed out or returned a 5xx. Retriable.
    /// </exception>
    /// <exception cref="Tnosc.Lib.Application.Exceptions.InvalidRequestException">
    /// The request was malformed — a 4xx from the gateway.
    /// </exception>
    ValueTask<GatewayAuthorizationResult> AuthorizeAsync(GatewayAuthorizationRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Asks the gateway to capture funds — either previously authorized, or immediately for a method
    /// that has no separate authorization step.
    /// </summary>
    /// <param name="request">The capture request.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>Whether the gateway captured the funds, and its reference or failure reason.</returns>
    /// <exception cref="Tnosc.Lib.Application.Exceptions.TransientFailureException">
    /// The gateway timed out or returned a 5xx. Retriable.
    /// </exception>
    /// <exception cref="Tnosc.Lib.Application.Exceptions.InvalidRequestException">
    /// The request was malformed — a 4xx from the gateway.
    /// </exception>
    ValueTask<GatewayCaptureResult> CaptureAsync(GatewayCaptureRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Asks the gateway to return previously captured funds to the customer.
    /// </summary>
    /// <param name="request">The refund request.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <exception cref="Tnosc.Lib.Application.Exceptions.TransientFailureException">
    /// The gateway timed out or returned a 5xx. Retriable.
    /// </exception>
    /// <exception cref="Tnosc.Lib.Application.Exceptions.InvalidRequestException">
    /// The request was malformed — a 4xx from the gateway.
    /// </exception>
    ValueTask RefundAsync(GatewayRefundRequest request, CancellationToken cancellationToken = default);
}
