// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using Tnosc.EShop.Server.Domain.Ordering.Orders.Discounts;
using Tnosc.EShop.Server.Domain.Ordering.Orders.Events;
using Tnosc.EShop.Server.Domain.Shared;
using Tnosc.Lib.Domain;
using Tnosc.Lib.Domain.Results;

namespace Tnosc.EShop.Server.Domain.Ordering.Orders;

/// <summary>
/// A customer's order. Owns its own lifecycle: which transition is legal from which status is decided
/// here and nowhere else.
/// </summary>
/// <remarks>
/// <para>
/// <strong>The status machine belongs to this class.</strong> No handler, workflow step or endpoint
/// ever compares an <see cref="OrderStatus"/> — each of <see cref="Confirm"/>,
/// <see cref="MarkPaid"/>, <see cref="Ship"/>, <see cref="Deliver"/> and <see cref="Cancel"/> decides
/// for itself whether it is reachable from the current status and hands back a <c>Conflict</c> when it
/// is not. A caller's whole job is to propagate that verdict, which is why <c>Status</c> can safely be
/// public: reading it to display an order is fine, reading it to decide something is the defect
/// <c>NoBusinessBranchingTests</c> catches.
/// </para>
/// <para>
/// The legal path is <c>Pending → Confirmed → Paid → Shipped → Delivered</c>, with
/// <see cref="Cancel"/> reachable from any status before despatch. Nothing leaves
/// <see cref="OrderStatus.Delivered"/> or <see cref="OrderStatus.Cancelled"/>; both are terminal.
/// </para>
/// </remarks>
public sealed class Order : AggregateRoot<OrderId>
{
    private readonly List<OrderLine> _lines = [];

    private Order()
    {
        // EF.
    }

    /// <summary>
    /// Gets the order's human-facing reference.
    /// </summary>
    public OrderNumber Number { get; private set; } = null!;

    /// <summary>
    /// Gets the identifier of the customer who placed the order.
    /// </summary>
    /// <remarks>
    /// A plain <see cref="Guid"/> — the caller's identity-provider subject, the same value Basket keys
    /// a basket on. Ordering must not reference Identity's <c>CustomerId</c>, and an order has to
    /// outlive any change to the profile behind it.
    /// </remarks>
    public Guid CustomerId { get; private set; }

    /// <summary>
    /// Gets where the order stands in its lifecycle.
    /// </summary>
    public OrderStatus Status { get; private set; }

    /// <summary>
    /// Gets where the order is to be delivered, snapshotted when it was placed.
    /// </summary>
    public ShippingAddress ShippingAddress { get; private set; } = null!;

    /// <summary>
    /// Gets what the customer owes — <see cref="Subtotal"/> after the discount strategy that applied
    /// when the order was placed.
    /// </summary>
    /// <remarks>
    /// Stored, unlike <see cref="Subtotal"/>, because it is the outcome of a rule evaluated once at a
    /// point in time. Recomputing it on read would silently reprice historical orders whenever the
    /// discount rules change.
    /// </remarks>
    public Money Total { get; private set; } = null!;

    /// <summary>
    /// Gets the UTC instant the order was placed.
    /// </summary>
    public DateTime PlacedOnUtc { get; private set; }

    /// <summary>
    /// Gets the order's lines.
    /// </summary>
    public IReadOnlyCollection<OrderLine> Lines => _lines.AsReadOnly();

    /// <summary>
    /// Gets the sum of the lines before any discount.
    /// </summary>
    /// <remarks>
    /// Computed from the lines, so it cannot drift from them; <c>OrderConfiguration</c> ignores it.
    /// Every line shares one currency — <see cref="Create"/> rejects an order that mixes them — so the
    /// summation never has to reconcile two.
    /// </remarks>
    public Money Subtotal
    {
        get
        {
            Money subtotal = _lines[0].LineTotal;

            foreach (OrderLine line in _lines.Skip(count: 1))
            {
                subtotal = subtotal.Add(other: line.LineTotal).Value;
            }

            return subtotal;
        }
    }

    /// <summary>
    /// Places an order and raises <see cref="OrderPlacedDomainEvent"/>.
    /// </summary>
    /// <remarks>
    /// The discount strategy is a parameter rather than something this method picks, so the selection
    /// rule stays in <see cref="DiscountStrategyFactory"/> and this method stays honest about depending
    /// on it. It is applied here, before the event is raised, so the total the event carries is the
    /// total the customer will be charged.
    /// </remarks>
    /// <param name="customerId">The identifier of the customer placing the order.</param>
    /// <param name="shippingAddress">Where the order is to be delivered.</param>
    /// <param name="lines">The raw lines to order, validated here.</param>
    /// <param name="discountStrategy">The discount scheme the order qualifies for.</param>
    /// <returns>
    /// The placed order, or one of the <c>Order.*</c> / <c>Money.*</c> / <c>OrderQuantity.*</c>
    /// validation errors.
    /// </returns>
    public static Result<Order> Create(
        Guid customerId,
        ShippingAddress shippingAddress,
        IReadOnlyCollection<OrderLineDraft> lines,
        IDiscountStrategy discountStrategy)
    {
        ArgumentNullException.ThrowIfNull(argument: shippingAddress);
        ArgumentNullException.ThrowIfNull(argument: discountStrategy);

        if (customerId == Guid.Empty)
        {
            return OrderErrors.CustomerRequired;
        }

        if (lines is null || lines.Count == 0)
        {
            return OrderErrors.NoLines;
        }

        Result<List<OrderLine>> built = BuildLines(drafts: lines);

        if (built.IsError)
        {
            return built.Errors.ToArray();
        }

        DateTime placedOnUtc = DateTime.UtcNow;

        var order = new Order
        {
            Id = OrderId.New(),
            Number = OrderNumber.Generate(placedOnUtc: placedOnUtc),
            CustomerId = customerId,
            Status = OrderStatus.Pending,
            ShippingAddress = shippingAddress,
            PlacedOnUtc = placedOnUtc,
        };

        order._lines.AddRange(collection: built.Value);
        order.Total = discountStrategy.Apply(total: order.Subtotal);
        order.IncrementVersion();

        order.AddDomainEvent(domainEvent: new OrderPlacedDomainEvent(
            Id: Guid.CreateVersion7(),
            OccurredOnUtc: placedOnUtc,
            OrderId: order.Id.Value,
            OrderNumber: order.Number.Value,
            CustomerId: order.CustomerId,
            TotalAmount: order.Total.Amount,
            TotalCurrency: order.Total.Currency,
            Lines: order.ToEventLines()));

        return order;
    }

    /// <summary>
    /// Reprices the order under a different discount scheme.
    /// </summary>
    /// <remarks>
    /// Only while the order is still <see cref="OrderStatus.Pending"/>. Once it is confirmed the total
    /// is what the customer agreed to, and a repricing after that is a credit note rather than an edit.
    /// </remarks>
    /// <param name="discountStrategy">The discount scheme to apply.</param>
    /// <returns>Success, or an <c>Order.CannotApplyDiscount</c> conflict.</returns>
    public Result ApplyDiscount(IDiscountStrategy discountStrategy)
    {
        ArgumentNullException.ThrowIfNull(argument: discountStrategy);

        if (Status != OrderStatus.Pending)
        {
            return OrderErrors.CannotApplyDiscount(status: Status);
        }

        Total = discountStrategy.Apply(total: Subtotal);
        IncrementVersion();

        return Result.Success();
    }

    /// <summary>
    /// Confirms the order and raises <see cref="OrderConfirmedDomainEvent"/>.
    /// </summary>
    /// <returns>Success, or an <c>Order.CannotConfirm</c> conflict when the order is not pending.</returns>
    public Result Confirm()
    {
        if (Status != OrderStatus.Pending)
        {
            return OrderErrors.CannotConfirm(status: Status);
        }

        Status = OrderStatus.Confirmed;
        IncrementVersion();

        AddDomainEvent(domainEvent: new OrderConfirmedDomainEvent(
            Id: Guid.CreateVersion7(),
            OccurredOnUtc: DateTime.UtcNow,
            OrderId: Id.Value,
            OrderNumber: Number.Value,
            CustomerId: CustomerId,
            TotalAmount: Total.Amount,
            TotalCurrency: Total.Currency));

        return Result.Success();
    }

    /// <summary>
    /// Records that the order has been paid for and raises <see cref="OrderPaidDomainEvent"/>.
    /// </summary>
    /// <returns>Success, or an <c>Order.CannotMarkPaid</c> conflict when the order is not confirmed.</returns>
    public Result MarkPaid()
    {
        if (Status != OrderStatus.Confirmed)
        {
            return OrderErrors.CannotMarkPaid(status: Status);
        }

        Status = OrderStatus.Paid;
        IncrementVersion();

        AddDomainEvent(domainEvent: new OrderPaidDomainEvent(
            Id: Guid.CreateVersion7(),
            OccurredOnUtc: DateTime.UtcNow,
            OrderId: Id.Value,
            OrderNumber: Number.Value,
            CustomerId: CustomerId,
            TotalAmount: Total.Amount,
            TotalCurrency: Total.Currency));

        return Result.Success();
    }

    /// <summary>
    /// Despatches the order.
    /// </summary>
    /// <remarks>
    /// Only a paid order ships. That is the invariant the plan singles out: an attempt to ship an
    /// unconfirmed or unpaid order is a <c>Conflict</c>, surfacing as 409, not something a caller can
    /// talk its way past.
    /// </remarks>
    /// <returns>Success, or an <c>Order.CannotShip</c> conflict when the order has not been paid for.</returns>
    public Result Ship()
    {
        if (Status != OrderStatus.Paid)
        {
            return OrderErrors.CannotShip(status: Status);
        }

        Status = OrderStatus.Shipped;
        IncrementVersion();

        return Result.Success();
    }

    /// <summary>
    /// Records that the order has reached the customer.
    /// </summary>
    /// <returns>Success, or an <c>Order.CannotDeliver</c> conflict when the order has not shipped.</returns>
    public Result Deliver()
    {
        if (Status != OrderStatus.Shipped)
        {
            return OrderErrors.CannotDeliver(status: Status);
        }

        Status = OrderStatus.Delivered;
        IncrementVersion();

        return Result.Success();
    }

    /// <summary>
    /// Cancels the order and raises <see cref="OrderCancelledDomainEvent"/>.
    /// </summary>
    /// <remarks>
    /// Reachable up to despatch. Once the parcel has left, cancelling is a return, which is a different
    /// process with different money attached — so <see cref="OrderStatus.Shipped"/> and
    /// <see cref="OrderStatus.Delivered"/> both refuse, as does an order already cancelled.
    /// </remarks>
    /// <returns>Success, or an <c>Order.CannotCancel</c> conflict.</returns>
    public Result Cancel()
    {
        if (Status is OrderStatus.Shipped or OrderStatus.Delivered or OrderStatus.Cancelled)
        {
            return OrderErrors.CannotCancel(status: Status);
        }

        Status = OrderStatus.Cancelled;
        IncrementVersion();

        AddDomainEvent(domainEvent: new OrderCancelledDomainEvent(
            Id: Guid.CreateVersion7(),
            OccurredOnUtc: DateTime.UtcNow,
            OrderId: Id.Value,
            OrderNumber: Number.Value,
            CustomerId: CustomerId,
            Lines: ToEventLines()));

        return Result.Success();
    }

    private static Result<List<OrderLine>> BuildLines(IReadOnlyCollection<OrderLineDraft> drafts)
    {
        List<OrderLine> lines = new(capacity: drafts.Count);
        string? currency = null;

        foreach (OrderLineDraft draft in drafts)
        {
            if (draft.ProductId == Guid.Empty)
            {
                return OrderErrors.ProductRequired;
            }

            if (string.IsNullOrWhiteSpace(value: draft.Sku) || draft.Sku.Length > OrderLine.SkuMaxLength)
            {
                return OrderErrors.InvalidSku;
            }

            if (string.IsNullOrWhiteSpace(value: draft.ProductName) || draft.ProductName.Length > OrderLine.ProductNameMaxLength)
            {
                return OrderErrors.InvalidProductName;
            }

            Result<Money> unitPrice = Money.Create(amount: draft.UnitPriceAmount, currency: draft.UnitPriceCurrency);

            if (unitPrice.IsError)
            {
                return unitPrice.Errors.ToArray();
            }

            Result<OrderQuantity> quantity = OrderQuantity.Create(value: draft.Quantity);

            if (quantity.IsError)
            {
                return quantity.Errors.ToArray();
            }

            currency ??= unitPrice.Value.Currency;

            if (!string.Equals(a: currency, b: unitPrice.Value.Currency, comparisonType: StringComparison.Ordinal))
            {
                return OrderErrors.MixedCurrencies;
            }

            lines.Add(item: OrderLine.Create(
                productId: draft.ProductId,
                sku: draft.Sku,
                productName: draft.ProductName,
                unitPrice: unitPrice.Value,
                quantity: quantity.Value));
        }

        return lines;
    }

    private OrderPlacedLine[] ToEventLines() =>
        [.. _lines.Select(selector: static line => new OrderPlacedLine(
            ProductId: line.ProductId,
            Sku: line.Sku,
            Quantity: line.Quantity.Value,
            UnitPriceAmount: line.UnitPrice.Amount,
            UnitPriceCurrency: line.UnitPrice.Currency))];
}
