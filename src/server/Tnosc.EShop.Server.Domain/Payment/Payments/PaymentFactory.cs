// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;
using Tnosc.EShop.Server.Domain.Shared;
using Tnosc.Lib.Domain.Results;

namespace Tnosc.EShop.Server.Domain.Payment.Payments;

/// <summary>
/// Creates payments under the ledger-wide invariant a single <see cref="Payment"/> instance cannot
/// see on its own.
/// </summary>
/// <remarks>
/// "One payment per order" is a business rule, so it must not surface as an <c>if</c> in a command
/// handler. It lives here, where the repository contract is reachable — the same shape as Catalog's
/// <c>ProductFactory</c> enforcing SKU uniqueness.
/// </remarks>
public static class PaymentFactory
{
    /// <summary>
    /// Creates a payment for an order, rejecting one that already has a payment initiated.
    /// </summary>
    /// <param name="repository">The payment repository consulted for the uniqueness check.</param>
    /// <param name="orderId">The identifier of the order to create a payment for.</param>
    /// <param name="amount">The amount to collect.</param>
    /// <param name="method">How the customer is paying.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>
    /// The created payment, or a <c>Payment.AlreadyExistsForOrder</c> conflict when one already
    /// exists.
    /// </returns>
    public static async ValueTask<Result<Payment>> CreateAsync(
        IPaymentRepository repository,
        Guid orderId,
        Money amount,
        PaymentMethod method,
        CancellationToken cancellationToken = default)
    {
        if (orderId == Guid.Empty)
        {
            return PaymentErrors.OrderRequired;
        }

        Payment? existing = await repository.GetByOrderIdAsync(orderId: orderId, cancellationToken: cancellationToken);

        if (existing is not null)
        {
            return PaymentErrors.AlreadyExistsForOrder(orderId: orderId);
        }

        return Payment.Create(orderId: orderId, amount: amount, method: method);
    }
}
