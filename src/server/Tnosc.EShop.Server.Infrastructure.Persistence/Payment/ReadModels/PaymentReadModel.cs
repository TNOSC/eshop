// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using Tnosc.Lib.Infrastructure.Persistence.ReadModels;

namespace Tnosc.EShop.Server.Infrastructure.Persistence.Payment.ReadModels;

/// <summary>
/// The query-side view of <c>payment.payments</c>: flat primitives, no typed ids, no value objects.
/// </summary>
internal sealed class PaymentReadModel : IReadModel
{
    /// <summary>
    /// Gets the payment's identifier.
    /// </summary>
    public Guid Id { get; init; }

    /// <summary>
    /// Gets the identifier of the order the payment is for.
    /// </summary>
    public Guid OrderId { get; init; }

    /// <summary>
    /// Gets the amount the payment covers.
    /// </summary>
    public decimal Amount { get; init; }

    /// <summary>
    /// Gets the currency of the amount.
    /// </summary>
    public string Currency { get; init; } = null!;

    /// <summary>
    /// Gets how the customer paid, as its name.
    /// </summary>
    public string Method { get; init; } = null!;

    /// <summary>
    /// Gets the payment's status, as its name.
    /// </summary>
    public string Status { get; init; } = null!;

    /// <summary>
    /// Gets the gateway's reference, once authorized or captured.
    /// </summary>
    public string? GatewayReference { get; init; }

    /// <summary>
    /// Gets why the payment failed, when it did.
    /// </summary>
    public string? FailureReason { get; init; }
}
