// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tnosc.EShop.Server.Application.Payment.Commands.CapturePayment.Steps;
using Tnosc.EShop.Server.Domain.Payment.Payments;
using Tnosc.Lib.Application.Abstractions.Persistence;
using Tnosc.Lib.Application.Attributes;
using Tnosc.Lib.Application.Commands;
using Tnosc.Lib.Domain.Results;
using PaymentAggregate = Tnosc.EShop.Server.Domain.Payment.Payments.Payment;

namespace Tnosc.EShop.Server.Application.Payment.Commands.CapturePayment;

/// <summary>
/// Loads a payment and hands its capture to <see cref="IPaymentCaptureStep"/>.
/// </summary>
/// <remarks>
/// Single commit, no <c>[Transactional]</c> — same reasoning as <c>InitiatePaymentCommandHandler</c>:
/// the gateway call happens before the only <see cref="IUnitOfWork.SaveChangesAsync"/> call, so a
/// retried attempt (<c>[Retry(3)]</c>) never leaves a stale tracked update behind — <c>Update</c>
/// simply re-marks the same, already-loaded aggregate.
/// </remarks>
/// <param name="repository">The payment repository.</param>
/// <param name="captureStep">Carries out the capture against the gateway.</param>
/// <param name="unitOfWork">The unit of work this handler commits through.</param>
[Retry(3)]
internal sealed class CapturePaymentCommandHandler(
    IPaymentRepository repository,
    IPaymentCaptureStep captureStep,
    IUnitOfWork unitOfWork)
    : ICommandHandler<CapturePaymentCommand>
{
    /// <inheritdoc />
    public async ValueTask<Result> HandleAsync(CapturePaymentCommand command, CancellationToken cancellationToken = default)
    {
        PaymentAggregate? payment = await repository.GetByIdAsync(
            id: PaymentId.From(value: command.PaymentId),
            cancellationToken: cancellationToken);

        if (payment is null)
        {
            return PaymentErrors.NotFound(paymentId: command.PaymentId);
        }

        Result captured = await captureStep.CaptureAsync(payment: payment, cancellationToken: cancellationToken);

        if (captured.IsError)
        {
            return captured.Errors.ToArray();
        }

        repository.Update(aggregate: payment);
        await unitOfWork.SaveChangesAsync(cancellationToken: cancellationToken);

        return Result.Success();
    }
}
