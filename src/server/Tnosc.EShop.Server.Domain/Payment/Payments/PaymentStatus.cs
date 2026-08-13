// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

namespace Tnosc.EShop.Server.Domain.Payment.Payments;

/// <summary>
/// Where a <see cref="Payment"/> sits in its lifecycle.
/// </summary>
/// <remarks>
/// Only <see cref="Payment"/> ever compares one of these values. A handler that reads
/// <see cref="Payment.Status"/> to decide anything has taken a business decision that belongs to the
/// aggregate — <see cref="Payment.Authorize"/>, <see cref="Payment.Capture"/>,
/// <see cref="Payment.Fail"/> and <see cref="Payment.Refund"/> each decide for themselves whether
/// they are reachable from here.
/// </remarks>
public enum PaymentStatus
{
    /// <summary>
    /// The payment has been created but nothing has been authorized or captured yet.
    /// </summary>
    Pending = 0,

    /// <summary>
    /// A gateway has reserved the funds; capturing still has to follow.
    /// </summary>
    Authorized = 1,

    /// <summary>
    /// The funds have been taken. Terminal on the success path.
    /// </summary>
    Captured = 2,

    /// <summary>
    /// The gateway declined the payment, or a technical failure was recorded as one. Terminal.
    /// </summary>
    Failed = 3,

    /// <summary>
    /// A previously captured payment has been returned to the customer. Terminal.
    /// </summary>
    Refunded = 4,
}
