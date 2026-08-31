// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using Tnosc.Lib.Application.Commands;

namespace Tnosc.EShop.Server.Application.Catalog.Commands.RemoveProductImage;

/// <summary>
/// Removes an existing product's image, if it has one.
/// </summary>
/// <param name="ProductId">The identifier of the product to remove the image from.</param>
public sealed record RemoveProductImageCommand(Guid ProductId) : ICommand;
