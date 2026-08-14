// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;

namespace Tnosc.EShop.Server.Tests.Acceptance.Contracts;

/// <summary>
/// The payment opened for an order.
/// </summary>
/// <param name="Id">The payment's identifier.</param>
/// <param name="OrderId">The order it settles.</param>
/// <param name="Method">How it is being paid.</param>
/// <param name="Status">The payment's current status.</param>
/// <param name="FailureReason">Why it failed, when it did.</param>
public sealed record Payment(
    Guid Id,
    Guid OrderId,
    string Method,
    string Status,
    string? FailureReason);
