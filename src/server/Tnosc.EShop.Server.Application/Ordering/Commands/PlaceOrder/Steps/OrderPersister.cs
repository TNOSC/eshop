// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;
using Tnosc.EShop.Server.Domain.Ordering.Orders;
using Tnosc.Lib.Application.Abstractions.Persistence;
using Tnosc.Lib.Shared.Results;

namespace Tnosc.EShop.Server.Application.Ordering.Commands.PlaceOrder.Steps;

/// <summary>
/// Adds the order to the repository and commits through <see cref="IUnitOfWork"/>.
/// </summary>
/// <remarks>
/// The single commit of the whole workflow, and the reason <c>PlaceOrderCommandHandler</c> carries no
/// <c>[Transactional]</c>: the earlier steps only read. This save is also where
/// <c>OrderPlacedDomainEvent</c> becomes an outbox row, in the same transaction as the order itself —
/// so the two contexts downstream can never be told about an order that failed to save, and can never
/// miss one that did.
/// </remarks>
/// <param name="orderRepository">The order repository.</param>
/// <param name="unitOfWork">The unit of work this step commits through.</param>
internal sealed class OrderPersister(IOrderRepository orderRepository, IUnitOfWork unitOfWork) : IOrderPersister
{
    /// <inheritdoc />
    public async ValueTask<Result<OrderId>> PersistAsync(Order order, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(argument: order);

        await orderRepository.AddAsync(aggregate: order, cancellationToken: cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken: cancellationToken);

        return order.Id;
    }
}
