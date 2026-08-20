// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tnosc.EShop.Server.Application.Payment.Ports;
using Tnosc.EShop.Server.Domain.Payment.Payments;
using Tnosc.Lib.Application.Abstractions.Persistence;
using Tnosc.Lib.Application.Commands;
using Tnosc.Lib.Shared.Results;
using PaymentAggregate = Tnosc.EShop.Server.Domain.Payment.Payments.Payment;

namespace Tnosc.EShop.Server.Application.Payment.Commands.RefundPayment;

/// <summary>
/// Loads a captured payment, asks the gateway to return the funds, and hands the transition to
/// <see cref="Payment.Refund"/>.
/// </summary>
/// <remarks>
/// No <c>[Retry]</c> here, unlike Initiate and Capture — the plan does not ask for one, and a refund
/// retried against an already-refunded gateway reference is not a case worth adding complexity for in
/// this slice. Single commit, so no <c>[Transactional]</c> either: <c>GetByIdAsync</c> and
/// <c>Update</c> both operate on the one aggregate this handler ever touches.
/// </remarks>
/// <param name="repository">The payment repository.</param>
/// <param name="gateway">The external gateway this handler refunds through.</param>
/// <param name="unitOfWork">The unit of work this handler commits through.</param>
internal sealed class RefundPaymentCommandHandler(
    IPaymentRepository repository,
    IPaymentGateway gateway,
    IUnitOfWork unitOfWork)
    : ICommandHandler<RefundPaymentCommand>
{
    /// <inheritdoc />
    public async ValueTask<Result> HandleAsync(RefundPaymentCommand command, CancellationToken cancellationToken = default)
    {
        PaymentAggregate? payment = await repository.GetByIdAsync(
            id: PaymentId.From(value: command.PaymentId),
            cancellationToken: cancellationToken);

        if (payment is null)
        {
            return PaymentErrors.NotFound(paymentId: command.PaymentId);
        }

        await gateway.RefundAsync(
            request: new GatewayRefundRequest(
                PaymentId: payment.Id.Value,
                OrderId: payment.OrderId,
                Amount: payment.Amount.Amount,
                Currency: payment.Amount.Currency,
                GatewayReference: payment.GatewayReference),
            cancellationToken: cancellationToken);

        Result refunded = payment.Refund(reason: command.Reason);

        if (refunded.IsError)
        {
            return refunded.Errors.ToArray();
        }

        repository.Update(aggregate: payment);
        await unitOfWork.SaveChangesAsync(cancellationToken: cancellationToken);

        return Result.Success();
    }
}
