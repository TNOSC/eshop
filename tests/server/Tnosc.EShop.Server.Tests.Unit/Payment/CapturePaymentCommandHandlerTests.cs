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
using Tnosc.EShop.Server.Application.Payment.Commands.CapturePayment;
using Tnosc.EShop.Server.Application.Payment.Commands.CapturePayment.Steps;
using Tnosc.EShop.Server.Domain.Payment.Payments;
using Tnosc.Lib.Application.Abstractions.Persistence;
using Tnosc.Lib.Shared.Results;
using PaymentAggregate = Tnosc.EShop.Server.Domain.Payment.Payments.Payment;

namespace Tnosc.EShop.Server.Tests.Unit.Payment;

/// <summary>
/// <see cref="CapturePaymentCommandHandler"/> orchestrating against a stubbed repository, capture
/// step and unit of work.
/// </summary>
public sealed class CapturePaymentCommandHandlerTests
{
    private readonly IPaymentRepository _repository = Substitute.For<IPaymentRepository>();
    private readonly IPaymentCaptureStep _captureStep = Substitute.For<IPaymentCaptureStep>();
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
        result.FirstError.Code.ShouldBe(expected: "Payment.NotFound");
        await _unitOfWork.DidNotReceive().SaveChangesAsync(cancellationToken: Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_Should_CommitTheCapture_When_TheStepSucceeds()
    {
        // Arrange
        PaymentAggregate payment = await PaymentTestFactory.AuthorizedAsync();
        _repository
            .GetByIdAsync(id: Arg.Any<PaymentId>(), cancellationToken: Arg.Any<CancellationToken>())
            .Returns(returnThis: ValueTask.FromResult<PaymentAggregate?>(result: payment));
        _captureStep
            .CaptureAsync(payment: Arg.Any<PaymentAggregate>(), cancellationToken: Arg.Any<CancellationToken>())
            .Returns(returnThis: callInfo =>
            {
                callInfo.ArgAt<PaymentAggregate>(0).Capture(gatewayReference: "cap_test");

                return ValueTask.FromResult(result: Result.Success());
            });

        // Act
        Result result = await HandleAsync(paymentId: payment.Id.Value);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        _repository.Received(requiredNumberOfCalls: 1).Update(aggregate: payment);
        await _unitOfWork.Received(requiredNumberOfCalls: 1).SaveChangesAsync(cancellationToken: Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_Should_PropagateTheAggregatesConflict_Unchanged_When_TheStepFails()
    {
        // Arrange
        PaymentAggregate payment = await PaymentTestFactory.RefundedAsync();
        _repository
            .GetByIdAsync(id: Arg.Any<PaymentId>(), cancellationToken: Arg.Any<CancellationToken>())
            .Returns(returnThis: ValueTask.FromResult<PaymentAggregate?>(result: payment));
        _captureStep
            .CaptureAsync(payment: Arg.Any<PaymentAggregate>(), cancellationToken: Arg.Any<CancellationToken>())
            .Returns(returnThis: ValueTask.FromResult<Result>(result: PaymentErrors.CannotCapture(status: PaymentStatus.Refunded)));

        // Act
        Result result = await HandleAsync(paymentId: payment.Id.Value);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe(expected: "Payment.CannotCapture");
        await _unitOfWork.DidNotReceive().SaveChangesAsync(cancellationToken: Arg.Any<CancellationToken>());
    }

    private ValueTask<Result> HandleAsync(Guid paymentId) =>
        new CapturePaymentCommandHandler(repository: _repository, captureStep: _captureStep, unitOfWork: _unitOfWork)
            .HandleAsync(command: new CapturePaymentCommand(PaymentId: paymentId), cancellationToken: CancellationToken.None);
}
