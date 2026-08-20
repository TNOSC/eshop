// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tnosc.EShop.Server.Application.Payment.Commands.InitiatePayment.Steps;
using Tnosc.EShop.Server.Domain.Payment.Payments;
using Tnosc.EShop.Server.Domain.Payment.Payments.Strategies;
using Tnosc.EShop.Server.Domain.Shared;
using Tnosc.Lib.Application.Abstractions.Persistence;
using Tnosc.Lib.Application.Attributes;
using Tnosc.Lib.Application.Commands;
using Tnosc.Lib.Shared.Results;
using PaymentAggregate = Tnosc.EShop.Server.Domain.Payment.Payments.Payment;

namespace Tnosc.EShop.Server.Application.Payment.Commands.InitiatePayment;

/// <summary>
/// Creates a payment for an order and, per the method's <see cref="PaymentPlan"/>, carries it as far
/// as it can go in one step: strategy selects the plan, <see cref="IPaymentSettlementStep"/> carries
/// it out against the gateway, the aggregate records the outcome.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Single commit, deliberately no <c>[Transactional]</c>.</strong> The gateway call — the
/// only realistic source of a retriable failure here — happens entirely in memory, before the
/// payment is ever handed to the repository. <see cref="IUnitOfWork.SaveChangesAsync"/> runs exactly
/// once, after settlement has already succeeded or the payment has already recorded its own decline,
/// so a retried attempt never leaves a previous attempt's aggregate tracked-but-unsaved alongside a
/// new one. Reaching for <c>[Transactional]</c> here — as the plan's summary table suggests — would
/// buy nothing this shape does not already have, per <c>.claude/rules</c>'s guidance that the
/// attribute is reserved for a handler spanning more than one aggregate or committing more than once.
/// </para>
/// <para>
/// <c>[Retry(3)]</c> is what this slice's resilience test exercises: <c>FakePaymentGateway</c> throws
/// <c>TransientFailureException</c> for its timeout test card, and this is the first handler that
/// exercises the pipeline's <c>Retry</c>-inside-<c>Exception</c> ordering end to end.
/// </para>
/// </remarks>
/// <param name="repository">The payment repository.</param>
/// <param name="settlementStep">Carries out the chosen method's settlement plan against the gateway.</param>
/// <param name="unitOfWork">The unit of work this handler commits through.</param>
[Retry(3)]
internal sealed class InitiatePaymentCommandHandler(
    IPaymentRepository repository,
    IPaymentSettlementStep settlementStep,
    IUnitOfWork unitOfWork)
    : ICommandHandler<InitiatePaymentCommand, PaymentId>
{
    /// <inheritdoc />
    public async ValueTask<Result<PaymentId>> HandleAsync(
        InitiatePaymentCommand command,
        CancellationToken cancellationToken = default)
    {
        Result<Money> amount = Money.Create(amount: command.AmountAmount, currency: command.AmountCurrency);

        if (amount.IsError)
        {
            return amount.Errors.ToArray();
        }

        PaymentMethod method = Enum.Parse<PaymentMethod>(value: command.Method, ignoreCase: true);
        IPaymentMethodStrategy strategy = PaymentMethodStrategyFactory.Create(method: method);
        Result<PaymentPlan> plan = strategy.Plan(amount: amount.Value);

        if (plan.IsError)
        {
            return plan.Errors.ToArray();
        }

        Result<PaymentAggregate> created = await PaymentFactory.CreateAsync(
            repository: repository,
            orderId: command.OrderId,
            amount: amount.Value,
            method: method,
            cancellationToken: cancellationToken);

        if (created.IsError)
        {
            return created.Errors.ToArray();
        }

        Result settled = await settlementStep.SettleAsync(
            payment: created.Value,
            plan: plan.Value,
            paymentReference: command.PaymentReference,
            cancellationToken: cancellationToken);

        if (settled.IsError)
        {
            return settled.Errors.ToArray();
        }

        await repository.AddAsync(aggregate: created.Value, cancellationToken: cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken: cancellationToken);

        return created.Value.Id;
    }
}
