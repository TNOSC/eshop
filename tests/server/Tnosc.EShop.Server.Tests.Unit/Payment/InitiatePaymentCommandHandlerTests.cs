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
using Tnosc.EShop.Server.Application.Payment.Commands.InitiatePayment;
using Tnosc.EShop.Server.Application.Payment.Commands.InitiatePayment.Steps;
using Tnosc.EShop.Server.Domain.Payment.Payments;
using Tnosc.Lib.Application.Abstractions.Persistence;
using Tnosc.Lib.Application.Decorators;
using Tnosc.Lib.Shared.Results;
using PaymentAggregate = Tnosc.EShop.Server.Domain.Payment.Payments.Payment;

namespace Tnosc.EShop.Server.Tests.Unit.Payment;

/// <summary>
/// <see cref="InitiatePaymentCommandHandler"/> orchestrating against a stubbed repository, settlement
/// step and unit of work — including the resilience test proving <c>[Retry(3)]</c> actually fires
/// around a handler that throws <c>TransientFailureException</c>.
/// </summary>
public sealed class InitiatePaymentCommandHandlerTests
{
    private readonly IPaymentRepository _repository = Substitute.For<IPaymentRepository>();
    private readonly IPaymentSettlementStep _settlementStep = Substitute.For<IPaymentSettlementStep>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    public InitiatePaymentCommandHandlerTests() =>
        _repository
            .GetByOrderIdAsync(orderId: Arg.Any<Guid>(), cancellationToken: Arg.Any<CancellationToken>())
            .Returns(returnThis: ValueTask.FromResult<PaymentAggregate?>(result: null));

    [Fact]
    public async Task HandleAsync_Should_CreateAndPersistThePayment_When_SettlementSucceeds()
    {
        // Arrange
        _settlementStep
            .SettleAsync(payment: Arg.Any<PaymentAggregate>(), plan: Arg.Any<Domain.Payment.Payments.Strategies.PaymentPlan>(), paymentReference: Arg.Any<string?>(), cancellationToken: Arg.Any<CancellationToken>())
            .Returns(returnThis: ValueTask.FromResult(result: Result.Success()));

        var command = new InitiatePaymentCommand(
            OrderId: Guid.CreateVersion7(),
            AmountAmount: 25m,
            AmountCurrency: "EUR",
            Method: nameof(PaymentMethod.Card),
            PaymentReference: "4242424242424242");

        // Act
        Result<PaymentId> result = await HandleAsync(command: command);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        await _repository.Received(requiredNumberOfCalls: 1).AddAsync(aggregate: Arg.Any<PaymentAggregate>(), cancellationToken: Arg.Any<CancellationToken>());
        await _unitOfWork.Received(requiredNumberOfCalls: 1).SaveChangesAsync(cancellationToken: Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_Should_PropagateTheDomainsFailure_Unchanged_When_APaymentAlreadyExistsForTheOrder()
    {
        // Arrange
        PaymentAggregate existing = await PaymentTestFactory.PendingAsync();
        _repository
            .GetByOrderIdAsync(orderId: Arg.Any<Guid>(), cancellationToken: Arg.Any<CancellationToken>())
            .Returns(returnThis: ValueTask.FromResult<PaymentAggregate?>(result: existing));

        var command = new InitiatePaymentCommand(
            OrderId: Guid.CreateVersion7(),
            AmountAmount: 25m,
            AmountCurrency: "EUR",
            Method: nameof(PaymentMethod.Card),
            PaymentReference: null);

        // Act
        Result<PaymentId> result = await HandleAsync(command: command);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe(expected: "Payment.AlreadyExistsForOrder");
        await _unitOfWork.DidNotReceive().SaveChangesAsync(cancellationToken: Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_Should_PropagateADeclinedSettlement_AsSuccess_BecauseADeclineIsABusinessOutcome()
    {
        // Arrange — the aggregate already recorded Fail(); the settlement step returns Success because
        // it successfully applied the gateway's (declining) verdict.
        _settlementStep
            .SettleAsync(payment: Arg.Any<PaymentAggregate>(), plan: Arg.Any<Domain.Payment.Payments.Strategies.PaymentPlan>(), paymentReference: Arg.Any<string?>(), cancellationToken: Arg.Any<CancellationToken>())
            .Returns(returnThis: callInfo =>
            {
                PaymentAggregate payment = callInfo.ArgAt<PaymentAggregate>(0);
                payment.Fail(reason: "card_declined");

                return ValueTask.FromResult(result: Result.Success());
            });

        var command = new InitiatePaymentCommand(
            OrderId: Guid.CreateVersion7(),
            AmountAmount: 25m,
            AmountCurrency: "EUR",
            Method: nameof(PaymentMethod.Card),
            PaymentReference: "4000000000000002");

        // Act
        Result<PaymentId> result = await HandleAsync(command: command);

        // Assert
        result.IsSuccess.ShouldBeTrue(customMessage: "a decline is data on the aggregate, not a failed command");
        await _unitOfWork.Received(requiredNumberOfCalls: 1).SaveChangesAsync(cancellationToken: Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Pipeline_Should_RetryAndSucceed_When_TheGatewayThrowsTransientFailureTwice()
    {
        // Arrange — the first real end-to-end proof that [Retry(3)] on InitiatePaymentCommandHandler
        // actually retries a throw from the gateway boundary. RetryDecorator only ever sees an
        // exception that escaped the handler, so the settlement step is real here (not stubbed) and
        // wraps a gateway double that throws twice before approving.
        var gateway = new StubPaymentGateway { TransientFailuresBeforeSuccess = 2 };
        IPaymentRepository repository = Substitute.For<IPaymentRepository>();
        repository
            .GetByOrderIdAsync(orderId: Arg.Any<Guid>(), cancellationToken: Arg.Any<CancellationToken>())
            .Returns(returnThis: ValueTask.FromResult<PaymentAggregate?>(result: null));
        IUnitOfWork unitOfWork = Substitute.For<IUnitOfWork>();

        var handler = new InitiatePaymentCommandHandler(
            repository: repository,
            settlementStep: new PaymentSettlementStep(gateway: gateway),
            unitOfWork: unitOfWork);
        var retrying = new RetryDecorator.CommandHandler<InitiatePaymentCommand, PaymentId>(innerHandler: handler);

        var command = new InitiatePaymentCommand(
            OrderId: Guid.CreateVersion7(),
            AmountAmount: 25m,
            AmountCurrency: "EUR",
            Method: nameof(PaymentMethod.Card),
            PaymentReference: "4242424242424242");

        // Act
        Result<PaymentId> result = await retrying.HandleAsync(command: command, cancellationToken: CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue(customMessage: "the third attempt must succeed once the transient failures are exhausted");
        gateway.AuthorizeCallCount.ShouldBe(expected: 3, customMessage: "two failed attempts plus the one that succeeded");
        await unitOfWork.Received(requiredNumberOfCalls: 1).SaveChangesAsync(cancellationToken: Arg.Any<CancellationToken>());
    }

    private ValueTask<Result<PaymentId>> HandleAsync(InitiatePaymentCommand command) =>
        new InitiatePaymentCommandHandler(repository: _repository, settlementStep: _settlementStep, unitOfWork: _unitOfWork)
            .HandleAsync(command: command, cancellationToken: CancellationToken.None);
}
