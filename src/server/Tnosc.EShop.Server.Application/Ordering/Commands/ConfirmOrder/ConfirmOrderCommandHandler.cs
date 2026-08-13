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
using Tnosc.Lib.Application.Commands;
using Tnosc.Lib.Domain.Results;

namespace Tnosc.EShop.Server.Application.Ordering.Commands.ConfirmOrder;

/// <summary>
/// Loads the caller's order and hands the transition to <see cref="Order.Confirm"/>.
/// </summary>
/// <remarks>
/// A plain handler, not a workflow. The workflow-plus-steps pattern earns its place in
/// <c>PlaceOrder</c>, which needs five collaborators; applying it to a load-transition-save handler
/// would be ceremony with nothing behind it. Whether a confirmation is legal from the order's current
/// status is the aggregate's decision — this handler propagates the verdict and never inspects
/// <see cref="Order.Status"/>.
/// </remarks>
/// <param name="repository">The order repository.</param>
/// <param name="unitOfWork">The unit of work this handler commits through.</param>
internal sealed class ConfirmOrderCommandHandler(
    IOrderRepository repository,
    IUnitOfWork unitOfWork)
    : ICommandHandler<ConfirmOrderCommand>
{
    /// <inheritdoc />
    public async ValueTask<Result> HandleAsync(
        ConfirmOrderCommand command,
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

        Result confirmed = order.Confirm();

        if (confirmed.IsError)
        {
            return confirmed.Errors.ToArray();
        }

        repository.Update(aggregate: order);
        await unitOfWork.SaveChangesAsync(cancellationToken: cancellationToken);

        return Result.Success();
    }
}
