// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tnosc.EShop.Server.Domain.Ordering.Orders;
using Tnosc.Lib.Application.Abstractions.Persistence;
using Tnosc.Lib.Application.Attributes;
using Tnosc.Lib.Application.Commands;
using Tnosc.Lib.Shared.Results;

namespace Tnosc.EShop.Server.Application.Ordering.Commands.CancelOrder;

/// <summary>
/// Loads the caller's order and hands the transition to <see cref="Order.Cancel"/>.
/// </summary>
/// <remarks>
/// Which statuses a cancellation is still reachable from — and that a shipped order is not one of them
/// — is entirely the aggregate's business. This handler never sees a status.
/// </remarks>
/// <param name="repository">The order repository.</param>
/// <param name="unitOfWork">The unit of work this handler commits through.</param>
[Retry(5)]
internal sealed class CancelOrderCommandHandler(
    IOrderRepository repository,
    IUnitOfWork unitOfWork)
    : ICommandHandler<CancelOrderCommand>
{
    /// <inheritdoc />
    public async ValueTask<Result> HandleAsync(
        CancelOrderCommand command,
        CancellationToken cancellationToken = default)
    {
        Order? order = await repository.GetByIdForCustomerAsync(
            orderId: OrderId.From(value: command.OrderId),
            customerId: command.CustomerId,
            cancellationToken: cancellationToken);

        if (order is null)
        {
            return OrderErrors.NotFound(orderId: command.OrderId);
        }

        Result cancelled = order.Cancel();

        if (cancelled.IsError)
        {
            return cancelled.Errors.ToArray();
        }

        repository.Update(aggregate: order);
        await unitOfWork.SaveChangesAsync(cancellationToken: cancellationToken);

        return Result.Success();
    }
}
