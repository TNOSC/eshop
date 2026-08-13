// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;
using Tnosc.EShop.Server.Application.Ordering.Ports;
using Tnosc.EShop.Server.Domain.Ordering.Orders;
using Tnosc.Lib.Domain.Results;

namespace Tnosc.EShop.Server.Application.Ordering.Commands.PlaceOrder.Steps;

/// <summary>
/// Reads the basket through Ordering's own port and rejects an order with nothing to order.
/// </summary>
/// <remarks>
/// "No basket" and "an empty basket" are the same answer to the caller, so both return the one
/// conflict. Neither is a business branch: the first is an existence check, the second a count of
/// zero — the shape the no-business-branching rule explicitly permits.
/// </remarks>
/// <param name="basketReader">The basket read port.</param>
internal sealed class BasketResolver(IOrderBasketReader basketReader) : IBasketResolver
{
    /// <inheritdoc />
    public async ValueTask<Result<OrderBasketSnapshot>> ResolveAsync(
        Guid customerId,
        CancellationToken cancellationToken = default)
    {
        OrderBasketSnapshot? basket = await basketReader.ReadAsync(
            customerId: customerId,
            cancellationToken: cancellationToken);

        if (basket is null || basket.Lines.Length == 0)
        {
            return OrderErrors.BasketEmpty(customerId: customerId);
        }

        return basket;
    }
}
