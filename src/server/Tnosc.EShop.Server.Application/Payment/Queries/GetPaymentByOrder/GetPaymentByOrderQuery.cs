// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using Tnosc.Lib.Application.Queries;

namespace Tnosc.EShop.Server.Application.Payment.Queries.GetPaymentByOrder;

/// <summary>
/// Reads the payment initiated for an order, if any.
/// </summary>
/// <param name="OrderId">The identifier of the order to look up.</param>
public sealed record GetPaymentByOrderQuery(Guid OrderId) : IQuery<PaymentDto>;
