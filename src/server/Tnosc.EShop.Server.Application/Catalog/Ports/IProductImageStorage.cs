// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Tnosc.EShop.Server.Application.Catalog.Ports;

/// <summary>
/// The application's contract for physical product-image storage. The application owns this port;
/// <c>Server.Infrastructure.External</c> owns the one adapter that implements it today,
/// <c>BlobProductImageStorage</c>, backed by Azure Blob Storage (Azurite locally).
/// </summary>
public interface IProductImageStorage
{
    /// <summary>
    /// Uploads image content for a product, replacing nothing — a previous image is the caller's own
    /// responsibility to delete via <see cref="DeleteAsync"/> before calling this again.
    /// </summary>
    /// <param name="productId">The identifier of the product the image belongs to.</param>
    /// <param name="fileName">The uploaded file's original name.</param>
    /// <param name="contentType">The uploaded file's content type.</param>
    /// <param name="content">The uploaded file's bytes.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>The publicly reachable URL the uploaded image is now available at.</returns>
    ValueTask<string> UploadAsync(
        Guid productId,
        string fileName,
        string contentType,
        byte[] content,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a previously uploaded image.
    /// </summary>
    /// <param name="imageUrl">The URL previously returned by <see cref="UploadAsync"/>.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
#pragma warning disable CA1054 // imageUrl is a flat wire-format string like every other DTO field, never System.Uri.
    ValueTask DeleteAsync(string imageUrl, CancellationToken cancellationToken = default);
#pragma warning restore CA1054
}
