// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using Tnosc.EShop.Server.Domain.Ordering.Orders;
using Tnosc.Lib.Application.Commands;

namespace Tnosc.EShop.Server.Application.Ordering.Commands.PlaceOrder;

/// <summary>
/// Turns the caller's basket into an order.
/// </summary>
/// <remarks>
/// One field, and it is not one the client supplies: the endpoint fills it from
/// <c>IUserContext.UserId</c>. Everything else the order needs — the lines, the prices, the delivery
/// address, the discount — is resolved server-side by <see cref="IPlaceOrderWorkflow"/>, so a client
/// cannot name someone else's basket, pick its own prices, or ship to an address the customer never
/// saved.
/// </remarks>
/// <param name="CustomerId">The identifier of the customer placing the order.</param>
public sealed record PlaceOrderCommand(Guid CustomerId) : ICommand<OrderId>;
