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

namespace Tnosc.EShop.Server.Application.Ordering.Commands.ShipOrder;

/// <summary>
/// Loads the order and hands the transition to <see cref="Order.Ship"/>.
/// </summary>
/// <remarks>
/// The "ship an unpaid order" case the plan singles out resolves here without this handler knowing
/// anything about it: <see cref="Order.Ship"/> returns <c>Order.CannotShip</c> as a <c>Conflict</c>,
/// this handler propagates it unchanged, and the endpoint maps <c>Conflict</c> to <strong>409</strong>
/// with an RFC 7807 body. Nothing in this file mentions a status.
/// </remarks>
/// <param name="repository">The order repository.</param>
/// <param name="unitOfWork">The unit of work this handler commits through.</param>
internal sealed class ShipOrderCommandHandler(
    IOrderRepository repository,
    IUnitOfWork unitOfWork)
    : ICommandHandler<ShipOrderCommand>
{
    /// <inheritdoc />
    public async ValueTask<Result> HandleAsync(
        ShipOrderCommand command,
        CancellationToken cancellationToken = default)
    {
        Order? order = await repository.GetByIdAsync(
            id: OrderId.From(value: command.OrderId),
            cancellationToken: cancellationToken);

        if (order is null)
        {
            return OrderErrors.NotFound(orderId: command.OrderId);
        }

        Result shipped = order.Ship();

        if (shipped.IsError)
        {
            return shipped.Errors.ToArray();
        }

        repository.Update(aggregate: order);
        await unitOfWork.SaveChangesAsync(cancellationToken: cancellationToken);

        return Result.Success();
    }
}
