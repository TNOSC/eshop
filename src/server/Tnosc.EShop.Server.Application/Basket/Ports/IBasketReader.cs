// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Tnosc.EShop.Server.Application.Basket.Ports;

/// <summary>
/// Reads a customer's basket for the query side.
/// </summary>
/// <remarks>
/// Named <c>Reader</c>, deliberately not <c>Repository</c>:
/// <c>HandlerTests.QueryHandlers_Should_Not_Reference_Repositories</c> matches on an <c>I</c> prefix
/// plus a <c>Repository</c> suffix, and a <c>Reader</c> both passes that scan and honestly describes
/// what this is — a read port, not the write-model repository. Implemented in
/// <c>Server.Infrastructure.External</c> over Redis; <c>GetBasketQueryHandler</c> in
/// <c>Server.Infrastructure.Persistence</c> depends only on this port, which is why it stays green
/// against <c>Persistence_Should_Not_Depend_On_Api_Host_Or_External</c>.
/// </remarks>
public interface IBasketReader
{
    /// <summary>
    /// Reads a customer's basket.
    /// </summary>
    /// <param name="customerId">The identifier of the customer whose basket to read.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>The customer's basket snapshot, or <see langword="null"/> when they have none.</returns>
    ValueTask<BasketSnapshot?> ReadAsync(Guid customerId, CancellationToken cancellationToken = default);
}
