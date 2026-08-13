// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;
using NSubstitute;
using Shouldly;
using Tnosc.EShop.Server.Application.Payment.Commands.RefundPayment;
using Tnosc.EShop.Server.Application.Payment.Ports;
using Tnosc.EShop.Server.Domain.Payment.Payments;
using Tnosc.Lib.Application.Abstractions.Persistence;
using Tnosc.Lib.Domain.Results;
using PaymentAggregate = Tnosc.EShop.Server.Domain.Payment.Payments.Payment;

namespace Tnosc.EShop.Server.Tests.Unit.Payment;

/// <summary>
/// <see cref="RefundPaymentCommandHandler"/> orchestrating against a stubbed repository, gateway and
/// unit of work.
/// </summary>
public sealed class RefundPaymentCommandHandlerTests
{
    private readonly IPaymentRepository _repository = Substitute.For<IPaymentRepository>();
    private readonly IPaymentGateway _gateway = Substitute.For<IPaymentGateway>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    [Fact]
    public async Task HandleAsync_Should_ReturnNotFound_When_NoSuchPaymentExists()
    {
        // Arrange
        _repository
            .GetByIdAsync(id: Arg.Any<PaymentId>(), cancellationToken: Arg.Any<CancellationToken>())
            .Returns(returnThis: ValueTask.FromResult<PaymentAggregate?>(result: null));

        // Act
        Result result = await HandleAsync(paymentId: Guid.CreateVersion7());

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Type.ShouldBe(expected: ErrorType.NotFound);
        await _gateway.DidNotReceive().RefundAsync(request: Arg.Any<GatewayRefundRequest>(), cancellationToken: Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_Should_RefundTheGateway_ThenTheAggregate_When_ThePaymentWasCaptured()
    {
        // Arrange
        PaymentAggregate payment = await PaymentTestFactory.CapturedAsync();
        _repository
            .GetByIdAsync(id: Arg.Any<PaymentId>(), cancellationToken: Arg.Any<CancellationToken>())
            .Returns(returnThis: ValueTask.FromResult<PaymentAggregate?>(result: payment));

        // Act
        Result result = await HandleAsync(paymentId: payment.Id.Value, reason: "customer request");

        // Assert
        result.IsSuccess.ShouldBeTrue();
        await _gateway.Received(requiredNumberOfCalls: 1).RefundAsync(
            request: Arg.Is<GatewayRefundRequest>(predicate: request => request.PaymentId == payment.Id.Value),
            cancellationToken: Arg.Any<CancellationToken>());
        _repository.Received(requiredNumberOfCalls: 1).Update(aggregate: payment);
        await _unitOfWork.Received(requiredNumberOfCalls: 1).SaveChangesAsync(cancellationToken: Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_Should_PropagateTheAggregatesConflict_Unchanged_When_ThePaymentWasNeverCaptured()
    {
        // Arrange
        PaymentAggregate payment = await PaymentTestFactory.PendingAsync();
        _repository
            .GetByIdAsync(id: Arg.Any<PaymentId>(), cancellationToken: Arg.Any<CancellationToken>())
            .Returns(returnThis: ValueTask.FromResult<PaymentAggregate?>(result: payment));

        // Act
        Result result = await HandleAsync(paymentId: payment.Id.Value);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe(expected: "Payment.CannotRefund");
        await _unitOfWork.DidNotReceive().SaveChangesAsync(cancellationToken: Arg.Any<CancellationToken>());
    }

    private ValueTask<Result> HandleAsync(Guid paymentId, string? reason = null) =>
        new RefundPaymentCommandHandler(repository: _repository, gateway: _gateway, unitOfWork: _unitOfWork)
            .HandleAsync(command: new RefundPaymentCommand(PaymentId: paymentId, Reason: reason), cancellationToken: CancellationToken.None);
}
