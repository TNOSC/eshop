// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tnosc.EShop.Server.Application.Basket.Queries.GetBasket;
using Tnosc.EShop.Server.Domain.Basket.Baskets;
using Tnosc.EShop.Server.Shared.Basket;
using Tnosc.Lib.Application.Attributes;
using Tnosc.Lib.Application.Commands;
using Tnosc.Lib.Domain.Results;
using BasketAggregate = Tnosc.EShop.Server.Domain.Basket.Baskets.Basket;

namespace Tnosc.EShop.Server.Application.Basket.Commands.ChangeBasketItemQuantity;

/// <summary>
/// Loads the caller's basket and delegates the transition to
/// <see cref="BasketAggregate.ChangeItemQuantity"/>. No <c>IUnitOfWork</c>, no
/// <c>[Transactional]</c>, no <c>[Idempotent]</c> — see <see cref="Tnosc.EShop.Server.Application.Basket.Commands.AddItemToBasket.AddItemToBasketCommandHandler"/>'s remarks.
/// </summary>
/// <param name="repository">The basket repository.</param>
internal sealed class ChangeBasketItemQuantityCommandHandler(IBasketRepository repository)
    : ICommandHandler<ChangeBasketItemQuantityCommand, BasketDto>
{
    /// <inheritdoc />
    public async ValueTask<Result<BasketDto>> HandleAsync(
        ChangeBasketItemQuantityCommand command,
        CancellationToken cancellationToken = default)
    {
        Result<Quantity> quantity = Quantity.Create(value: command.Quantity);

        if (quantity.IsError)
        {
            return quantity.Errors.ToArray();
        }

        BasketAggregate? basket = await repository.GetByCustomerIdAsync(
            customerId: command.CustomerId,
            cancellationToken: cancellationToken);

        if (basket is null)
        {
            return BasketErrors.ItemNotFound(itemId: command.ItemId);
        }

        Result changed = basket.ChangeItemQuantity(
            itemId: BasketItemId.From(value: command.ItemId),
            quantity: quantity.Value);

        if (changed.IsError)
        {
            return changed.Errors.ToArray();
        }

        await repository.SaveAsync(basket: basket, cancellationToken: cancellationToken);

        return basket.ToDto();
    }
}
