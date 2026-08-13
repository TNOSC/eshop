// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using Tnosc.Lib.Application.Commands;

namespace Tnosc.EShop.Server.Application.Ordering.Commands.CancelOrder;

/// <summary>
/// Cancels one of the caller's own orders, before it ships.
/// </summary>
/// <param name="OrderId">The identifier of the order to cancel.</param>
/// <param name="CustomerId">The identifier of the customer the order must belong to.</param>
public sealed record CancelOrderCommand(Guid OrderId, Guid CustomerId) : ICommand;
