// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using Tnosc.Lib.Application.Commands;

namespace Tnosc.EShop.Server.Application.Basket.Commands.ClearBasket;

/// <summary>
/// Empties the caller's basket.
/// </summary>
/// <param name="CustomerId">The identifier of the customer whose basket to clear.</param>
public sealed record ClearBasketCommand(Guid CustomerId) : ICommand;
