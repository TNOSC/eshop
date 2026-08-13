// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

namespace Tnosc.EShop.Server.Infrastructure.Persistence.Payment;

/// <summary>
/// Names the Postgres objects owned by the Payment bounded context. One schema per context keeps the
/// contexts separable inside the single database the outbox forces them to share.
/// </summary>
internal static class PaymentSchema
{
    /// <summary>
    /// The Postgres schema every Payment table lives in.
    /// </summary>
    public const string Name = "payment";

    /// <summary>
    /// The name of the payments table.
    /// </summary>
    public const string PaymentsTable = "payments";
}
