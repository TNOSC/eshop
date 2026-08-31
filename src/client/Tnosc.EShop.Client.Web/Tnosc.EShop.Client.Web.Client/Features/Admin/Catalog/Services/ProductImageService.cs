// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Tnosc.EShop.Client.Web.Client.Infrastructure.Api;
using Tnosc.Lib.Web.Results;

namespace Tnosc.EShop.Client.Web.Client.Features.Admin.Catalog.Services;

/// <inheritdoc cref="IProductImageService" />
internal sealed class ProductImageService(ICatalogApi catalogApi) : IProductImageService
{
    public Task<ClientResult> UploadAsync(Guid productId, Stream content, string fileName, string contentType, CancellationToken cancellationToken) =>
        catalogApi.UploadProductImageAsync(
            productId: productId,
            content: content,
            fileName: fileName,
            contentType: contentType,
            cancellationToken: cancellationToken);

    public Task<ClientResult> RemoveAsync(Guid productId, CancellationToken cancellationToken) =>
        catalogApi.DeleteProductImageAsync(productId: productId, cancellationToken: cancellationToken);
}
