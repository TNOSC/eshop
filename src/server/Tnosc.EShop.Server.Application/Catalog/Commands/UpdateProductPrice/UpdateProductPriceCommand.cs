// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC). All rights reserved.
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using Tnosc.Lib.Application.Commands;

namespace Tnosc.EShop.Server.Application.Catalog.Commands.UpdateProductPrice;

/// <summary>
/// Reprices an existing product.
/// </summary>
/// <param name="ProductId">The identifier of the product to reprice.</param>
/// <param name="Amount">The new price amount.</param>
/// <param name="Currency">The three-letter ISO 4217 currency of the new price.</param>
public sealed record UpdateProductPriceCommand(
    Guid ProductId,
    decimal Amount,
    string? Currency) : ICommand;
