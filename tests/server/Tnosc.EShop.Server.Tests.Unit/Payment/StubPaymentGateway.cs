// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;
using Tnosc.EShop.Server.Application.Payment.Ports;
using Tnosc.Lib.Application.Exceptions;

namespace Tnosc.EShop.Server.Tests.Unit.Payment;

/// <summary>
/// A hand-written <see cref="IPaymentGateway"/> test double that can throw a set number of times
/// before succeeding — the shape <see cref="Tnosc.Lib.Application.Decorators.RetryDecorator"/>'s
/// stateful retry-then-succeed behaviour needs, which a stateless NSubstitute stub cannot express as
/// naturally across repeated calls.
/// </summary>
internal sealed class StubPaymentGateway : IPaymentGateway
{
    private int _authorizeAttempts;

    /// <summary>
    /// Gets or sets how many <see cref="AuthorizeAsync"/> calls throw
    /// <see cref="TransientFailureException"/> before the call that finally succeeds.
    /// </summary>
    public int TransientFailuresBeforeSuccess { get; set; }

    /// <summary>
    /// Gets or sets the result returned once <see cref="TransientFailuresBeforeSuccess"/> attempts
    /// have thrown.
    /// </summary>
    public GatewayAuthorizationResult AuthorizationResult { get; set; } =
        new(IsApproved: true, AuthorizationId: "auth_stub", DeclineReason: null);

    /// <summary>
    /// Gets or sets the result <see cref="CaptureAsync"/> returns.
    /// </summary>
    public GatewayCaptureResult CaptureResult { get; set; } =
        new(IsSuccessful: true, CaptureId: "cap_stub", FailureReason: null);

    /// <summary>
    /// Gets how many times <see cref="AuthorizeAsync"/> has been called.
    /// </summary>
    public int AuthorizeCallCount => _authorizeAttempts;

    /// <inheritdoc />
    public ValueTask<GatewayAuthorizationResult> AuthorizeAsync(GatewayAuthorizationRequest request, CancellationToken cancellationToken = default)
    {
        _authorizeAttempts++;

        if (_authorizeAttempts <= TransientFailuresBeforeSuccess)
        {
            throw new TransientFailureException(message: "Simulated gateway timeout.", correlationId: null, inner: null);
        }

        return ValueTask.FromResult(result: AuthorizationResult);
    }

    /// <inheritdoc />
    public ValueTask<GatewayCaptureResult> CaptureAsync(GatewayCaptureRequest request, CancellationToken cancellationToken = default) =>
        ValueTask.FromResult(result: CaptureResult);

    /// <inheritdoc />
    public ValueTask RefundAsync(GatewayRefundRequest request, CancellationToken cancellationToken = default) =>
        ValueTask.CompletedTask;
}
