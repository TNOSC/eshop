// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tnosc.EShop.Server.Domain.Catalog.Products;
using Tnosc.Lib.Application.Validations;
using Tnosc.Lib.Shared.Results;

namespace Tnosc.EShop.Server.Application.Catalog.Commands.SetProductImage;

/// <summary>
/// Structural validation only — allowed content types and the maximum upload size are wire-level
/// constraints this handler enforces before ever calling <c>IProductImageStorage</c>, not domain
/// rules owned by <see cref="Product"/>.
/// </summary>
internal sealed class SetProductImageCommandValidator : IValidator<SetProductImageCommand>
{
    private const long MaxContentLength = 5 * 1024 * 1024;

    private static readonly string[] SupportedContentTypes = ["image/jpeg", "image/png", "image/webp"];

    /// <inheritdoc />
    public ValueTask<Result> ValidateAsync(SetProductImageCommand request, CancellationToken cancellationToken)
    {
        List<Error> errors = [];

        if (request.ProductId == Guid.Empty)
        {
            errors.Add(item: ProductErrors.IdRequired);
        }

        if (string.IsNullOrWhiteSpace(value: request.FileName))
        {
            errors.Add(item: ProductErrors.ImageFileNameRequired);
        }

        if (request.Content.Length == 0)
        {
            errors.Add(item: ProductErrors.ImageContentRequired);
        }
        else if (request.Content.Length > MaxContentLength)
        {
            errors.Add(item: ProductErrors.ImageContentTooLarge);
        }

        if (Array.IndexOf(array: SupportedContentTypes, value: request.ContentType) < 0)
        {
            errors.Add(item: ProductErrors.ImageContentTypeNotSupported);
        }

        return ValueTask.FromResult<Result>(result: errors);
    }
}
