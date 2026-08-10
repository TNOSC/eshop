// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC). All rights reserved.
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using Tnosc.Lib.Application.Commands;

namespace Tnosc.EShop.Server.Application.Catalog.Commands.AdjustStock;

/// <summary>
/// Adds or removes units from a product's stock level.
/// </summary>
/// <param name="ProductId">The identifier of the product to adjust.</param>
/// <param name="Delta">The signed number of units to add or remove.</param>
public sealed record AdjustStockCommand(
    Guid ProductId,
    int Delta) : ICommand;
