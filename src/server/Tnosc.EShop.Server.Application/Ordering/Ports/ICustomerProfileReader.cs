// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Tnosc.EShop.Server.Application.Ordering.Ports;

/// <summary>
/// Reads the delivery address an order should ship to.
/// </summary>
/// <remarks>
/// Owned by Ordering, implemented in <c>Server.Infrastructure.Persistence</c> against Identity's read
/// model — a genuine Postgres read, exactly like Basket's <c>IProductLookup</c>.
/// </remarks>
public interface ICustomerProfileReader
{
    /// <summary>
    /// Reads a customer's default delivery address.
    /// </summary>
    /// <param name="customerId">
    /// The caller's identity-provider subject, which is what Basket and Ordering both key a customer
    /// on.
    /// </param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>
    /// The address to ship to, or <see langword="null"/> when the customer has no profile or holds no
    /// default address.
    /// </returns>
    ValueTask<CustomerProfileSnapshot?> GetDefaultAddressAsync(Guid customerId, CancellationToken cancellationToken = default);
}
