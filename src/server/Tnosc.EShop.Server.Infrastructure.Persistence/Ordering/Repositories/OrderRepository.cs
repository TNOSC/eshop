// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Tnosc.EShop.Server.Domain.Ordering.Orders;
using Tnosc.EShop.Server.Infrastructure.Persistence.Contexts;
using Tnosc.Lib.Infrastructure.Persistence;

namespace Tnosc.EShop.Server.Infrastructure.Persistence.Ordering.Repositories;

/// <summary>
/// The write-side <see cref="Order"/> repository.
/// </summary>
/// <remarks>
/// None of these methods filters or decides anything a caller could not have asked for: the customer
/// identifier in <see cref="GetByIdForCustomerAsync"/> is part of the question, not a policy applied
/// to the answer. Lines come back with every order because <c>OrderConfiguration</c> auto-includes
/// them — an <see cref="Order"/> loaded without its lines could not compute its own subtotal.
/// </remarks>
/// <param name="context">The write context this repository reads and writes through.</param>
internal sealed class OrderRepository(EShopWriteDbContext context)
    : RepositoryBase<Order, OrderId>(context), IOrderRepository
{
    /// <inheritdoc />
    public async ValueTask<IReadOnlyCollection<Order>> GetByCustomerIdAsync(
        Guid customerId,
        CancellationToken cancellationToken = default) =>
        await context.Set<Order>()
            .Where(predicate: order => order.CustomerId == customerId)
            .OrderByDescending(keySelector: order => order.PlacedOnUtc)
            .ToListAsync(cancellationToken: cancellationToken);

    /// <inheritdoc />
    public async ValueTask<Order?> GetByOrderNumberAsync(
        OrderNumber orderNumber,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(argument: orderNumber);

        return await context.Set<Order>()
            .FirstOrDefaultAsync(
                predicate: order => order.Number.Value == orderNumber.Value,
                cancellationToken: cancellationToken);
    }

    /// <inheritdoc />
    public async ValueTask<Order?> GetByIdForCustomerAsync(
        OrderId orderId,
        Guid customerId,
        CancellationToken cancellationToken = default) =>
        await context.Set<Order>()
            .FirstOrDefaultAsync(
                predicate: order => order.Id == orderId && order.CustomerId == customerId,
                cancellationToken: cancellationToken);

    /// <inheritdoc />
    public async ValueTask<int> CountByCustomerIdAsync(
        Guid customerId,
        CancellationToken cancellationToken = default) =>
        await context.Set<Order>()
            .CountAsync(
                predicate: order => order.CustomerId == customerId,
                cancellationToken: cancellationToken);
}
