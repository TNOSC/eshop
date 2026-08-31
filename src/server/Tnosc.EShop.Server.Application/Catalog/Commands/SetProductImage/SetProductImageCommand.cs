// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using Tnosc.Lib.Application.Commands;

namespace Tnosc.EShop.Server.Application.Catalog.Commands.SetProductImage;

/// <summary>
/// Uploads a new image for an existing product, replacing any previous one.
/// </summary>
/// <param name="ProductId">The identifier of the product to set the image on.</param>
/// <param name="FileName">The uploaded file's original name.</param>
/// <param name="ContentType">The uploaded file's content type.</param>
/// <param name="Content">The uploaded file's bytes.</param>
public sealed record SetProductImageCommand(
    Guid ProductId,
    string FileName,
    string ContentType,
    byte[] Content) : ICommand;
