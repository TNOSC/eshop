// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;
using Tnosc.EShop.Server.Domain.Basket.Baskets;
using Tnosc.EShop.Server.Shared.Basket;
using Tnosc.Lib.Application.Attributes;
using Tnosc.Lib.Application.Commands;
using Tnosc.Lib.Domain.Results;
using BasketAggregate = Tnosc.EShop.Server.Domain.Basket.Baskets.Basket;

namespace Tnosc.EShop.Server.Application.Basket.Commands.ClearBasket;

/// <summary>
/// Loads the caller's basket, if any, and delegates to <see cref="BasketAggregate.Clear"/>. A
/// customer with no basket yet has nothing to clear, which is a structural existence check rather
/// than a business branch. No <c>IUnitOfWork</c>, no <c>[Transactional]</c>, no <c>[Idempotent]</c> —
/// see <see cref="Tnosc.EShop.Server.Application.Basket.Commands.AddItemToBasket.AddItemToBasketCommandHandler"/>'s remarks.
/// </summary>
/// <param name="repository">The basket repository.</param>
[CacheTag(CacheTags.Basket)]
internal sealed class ClearBasketCommandHandler(IBasketRepository repository)
    : ICommandHandler<ClearBasketCommand>
{
    /// <inheritdoc />
    public async ValueTask<Result> HandleAsync(
        ClearBasketCommand command,
        CancellationToken cancellationToken = default)
    {
        BasketAggregate? basket = await repository.GetByCustomerIdAsync(
            customerId: command.CustomerId,
            cancellationToken: cancellationToken);

        if (basket is null)
        {
            return Result.Success();
        }

        basket.Clear();
        await repository.SaveAsync(basket: basket, cancellationToken: cancellationToken);

        return Result.Success();
    }
}
