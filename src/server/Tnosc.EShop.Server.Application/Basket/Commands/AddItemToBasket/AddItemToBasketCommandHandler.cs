// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tnosc.EShop.Server.Application.Basket.Ports;
using Tnosc.EShop.Server.Application.Basket.Queries.GetBasket;
using Tnosc.EShop.Server.Domain.Basket.Baskets;
using Tnosc.EShop.Server.Domain.Shared;
using Tnosc.EShop.Server.Shared.Basket;
using Tnosc.Lib.Application.Attributes;
using Tnosc.Lib.Application.Commands;
using Tnosc.Lib.Domain.Results;
using BasketAggregate = Tnosc.EShop.Server.Domain.Basket.Baskets.Basket;

namespace Tnosc.EShop.Server.Application.Basket.Commands.AddItemToBasket;

/// <summary>
/// Looks up the product's current snapshot data, gets-or-creates the customer's basket, and delegates
/// the "increment rather than duplicate" rule to <see cref="BasketAggregate.AddItem"/>.
/// </summary>
/// <remarks>
/// The basket does not live in EF's change tracker, so this handler — like every Basket command
/// handler — takes no <c>IUnitOfWork</c>, carries no <c>[Transactional]</c> and no
/// <c>[Idempotent]</c>: persistence is immediate through <see cref="IBasketRepository.SaveAsync"/>,
/// and there is no EF transaction for either facility to hang off. See
/// <c>.claude/rules/idempotency.md</c>.
/// </remarks>
/// <param name="repository">The basket repository.</param>
/// <param name="productLookup">The port used to snapshot the product's current catalogue data.</param>
internal sealed class AddItemToBasketCommandHandler(
    IBasketRepository repository,
    IProductLookup productLookup)
    : ICommandHandler<AddItemToBasketCommand, BasketDto>
{
    /// <inheritdoc />
    public async ValueTask<Result<BasketDto>> HandleAsync(
        AddItemToBasketCommand command,
        CancellationToken cancellationToken = default)
    {
        Result<Quantity> quantity = Quantity.Create(value: command.Quantity);

        if (quantity.IsError)
        {
            return quantity.Errors.ToArray();
        }

        ProductSnapshot? product = await productLookup.GetAsync(
            productId: command.ProductId,
            cancellationToken: cancellationToken);

        if (product is null)
        {
            return BasketErrors.ProductNotFound(productId: command.ProductId);
        }

        Result<Money> unitPrice = Money.Create(amount: product.PriceAmount, currency: product.PriceCurrency);

        if (unitPrice.IsError)
        {
            return unitPrice.Errors.ToArray();
        }

        BasketAggregate? basket = await repository.GetByCustomerIdAsync(
            customerId: command.CustomerId,
            cancellationToken: cancellationToken);

        basket ??= BasketAggregate.CreateFor(customerId: command.CustomerId);

        Result added = basket.AddItem(
            productId: command.ProductId,
            sku: product.Sku,
            productName: product.Name,
            unitPrice: unitPrice.Value,
            quantity: quantity.Value);

        if (added.IsError)
        {
            return added.Errors.ToArray();
        }

        await repository.SaveAsync(basket: basket, cancellationToken: cancellationToken);

        return basket.ToDto();
    }
}
