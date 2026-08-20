// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tnosc.EShop.Server.Domain.Basket.Baskets;
using Tnosc.EShop.Server.Shared.Basket;
using Tnosc.Lib.Application.Attributes;
using Tnosc.Lib.Application.Commands;
using Tnosc.Lib.Shared.Results;
using BasketAggregate = Tnosc.EShop.Server.Domain.Basket.Baskets.Basket;

namespace Tnosc.EShop.Server.Application.Basket.Commands.RemoveBasketItem;

/// <summary>
/// Loads the caller's basket and delegates the transition to
/// <see cref="BasketAggregate.RemoveItem"/>. No <c>IUnitOfWork</c>, no <c>[Transactional]</c>, no
/// <c>[Idempotent]</c> — see <see cref="Tnosc.EShop.Server.Application.Basket.Commands.AddItemToBasket.AddItemToBasketCommandHandler"/>'s remarks.
/// </summary>
/// <param name="repository">The basket repository.</param>
internal sealed class RemoveBasketItemCommandHandler(IBasketRepository repository)
    : ICommandHandler<RemoveBasketItemCommand>
{
    /// <inheritdoc />
    public async ValueTask<Result> HandleAsync(
        RemoveBasketItemCommand command,
        CancellationToken cancellationToken = default)
    {
        BasketAggregate? basket = await repository.GetByCustomerIdAsync(
            customerId: command.CustomerId,
            cancellationToken: cancellationToken);

        if (basket is null)
        {
            return BasketErrors.ItemNotFound(itemId: command.ItemId);
        }

        Result removed = basket.RemoveItem(itemId: BasketItemId.From(value: command.ItemId));

        if (removed.IsError)
        {
            return removed.Errors.ToArray();
        }

        await repository.SaveAsync(basket: basket, cancellationToken: cancellationToken);

        return Result.Success();
    }
}
