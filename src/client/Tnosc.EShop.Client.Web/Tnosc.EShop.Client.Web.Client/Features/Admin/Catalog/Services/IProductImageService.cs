// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Tnosc.Lib.Web.Results;

namespace Tnosc.EShop.Client.Web.Client.Features.Admin.Catalog.Services;

/// <summary>
/// <see cref="Components.ProductImageDialog"/>'s component service — the only place that touches
/// <see cref="Tnosc.EShop.Client.Web.Client.Infrastructure.Api.ICatalogApi"/> for that dialog.
/// </summary>
public interface IProductImageService
{
    /// <summary>Uploads a new image for a product, replacing any previous one.</summary>
    /// <param name="productId">The product to set the image on.</param>
    /// <param name="content">The image file's content stream.</param>
    /// <param name="fileName">The image file's original name.</param>
    /// <param name="contentType">The image file's content type.</param>
    /// <param name="cancellationToken">The token observed while the call is in flight.</param>
    Task<ClientResult> UploadAsync(Guid productId, Stream content, string fileName, string contentType, CancellationToken cancellationToken);

    /// <summary>Removes a product's image.</summary>
    /// <param name="productId">The product to remove the image from.</param>
    /// <param name="cancellationToken">The token observed while the call is in flight.</param>
    Task<ClientResult> RemoveAsync(Guid productId, CancellationToken cancellationToken);
}
