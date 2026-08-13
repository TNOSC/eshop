// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;
using Tnosc.Lib.Domain.Repositories;

namespace Tnosc.EShop.Server.Domain.Payment.Payments;

/// <summary>
/// Command-side persistence contract for <see cref="Payment"/>.
/// </summary>
public interface IPaymentRepository : IRepository<Payment, PaymentId>
{
    /// <summary>
    /// Retrieves the payment initiated for an order, if any.
    /// </summary>
    /// <param name="orderId">The identifier of the order to look up.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>The matching payment, or <see langword="null"/> when none has been initiated.</returns>
    ValueTask<Payment?> GetByOrderIdAsync(Guid orderId, CancellationToken cancellationToken = default);
}
