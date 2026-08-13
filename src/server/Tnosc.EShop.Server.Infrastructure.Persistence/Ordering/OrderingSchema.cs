// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

namespace Tnosc.EShop.Server.Infrastructure.Persistence.Ordering;

/// <summary>
/// Names the Postgres objects owned by the Ordering bounded context. One schema per context keeps the
/// contexts separable inside the single database the outbox forces them to share.
/// </summary>
internal static class OrderingSchema
{
    /// <summary>
    /// The Postgres schema every Ordering table lives in.
    /// </summary>
    public const string Name = "ordering";

    /// <summary>
    /// The name of the orders table.
    /// </summary>
    public const string OrdersTable = "orders";

    /// <summary>
    /// The name of the order lines table.
    /// </summary>
    public const string OrderLinesTable = "order_lines";
}
