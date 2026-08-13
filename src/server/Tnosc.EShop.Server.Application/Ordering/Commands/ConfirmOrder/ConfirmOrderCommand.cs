// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using Tnosc.Lib.Application.Commands;

namespace Tnosc.EShop.Server.Application.Ordering.Commands.ConfirmOrder;

/// <summary>
/// Confirms one of the caller's own pending orders.
/// </summary>
/// <remarks>
/// <see cref="CustomerId"/> comes from the caller's token, never from the request, and is part of the
/// repository lookup rather than a check the handler performs. A customer therefore cannot address
/// another customer's order at all — the structural-ownership shape from
/// <c>.claude/rules/authorization.md</c>.
/// </remarks>
/// <param name="OrderId">The identifier of the order to confirm.</param>
/// <param name="CustomerId">The identifier of the customer the order must belong to.</param>
public sealed record ConfirmOrderCommand(Guid OrderId, Guid CustomerId) : ICommand;
