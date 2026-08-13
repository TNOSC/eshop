// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using Tnosc.Lib.Application.Commands;

namespace Tnosc.EShop.Server.Application.Ordering.Commands.ShipOrder;

/// <summary>
/// Despatches an order.
/// </summary>
/// <remarks>
/// No customer identifier, unlike <c>ConfirmOrderCommand</c> and <c>CancelOrderCommand</c>: shipping
/// is a warehouse operation over any customer's order, so the endpoint gates it on the
/// <c>ordering:ship</c> permission instead of scoping it to the caller.
/// </remarks>
/// <param name="OrderId">The identifier of the order to despatch.</param>
public sealed record ShipOrderCommand(Guid OrderId) : ICommand;
