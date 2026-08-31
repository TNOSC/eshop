// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Tnosc.EShop.Server.Application.Catalog.Ports;

namespace Tnosc.EShop.Server.Infrastructure.External.Storage;

/// <summary>
/// <see cref="IProductImageStorage"/> over Azure Blob Storage — Azurite locally, a real storage
/// account in production, per the <see cref="BlobServiceClient"/> Aspire wires up in
/// <c>Server.Host</c>. The container is created with public blob-level read access on first use, so
/// <see cref="UploadAsync"/> can return a permanent, plain URL rather than a SAS token that would need
/// regenerating on every read — a deliberate simplification for this reference implementation.
/// </summary>
/// <param name="blobServiceClient">
/// The Azure Storage client, registered by <c>AddAzureBlobServiceClient</c> in <c>Server.Host</c>.
/// </param>
/// <param name="options">The container name and optional browser-facing base URL.</param>
internal sealed class BlobProductImageStorage(BlobServiceClient blobServiceClient, ProductImageStorageOptions options)
    : IProductImageStorage
{
    /// <inheritdoc />
    public async ValueTask<string> UploadAsync(
        Guid productId,
        string fileName,
        string contentType,
        byte[] content,
        CancellationToken cancellationToken = default)
    {
        BlobContainerClient container = blobServiceClient.GetBlobContainerClient(blobContainerName: options.ContainerName);
        await container.CreateIfNotExistsAsync(publicAccessType: PublicAccessType.Blob, cancellationToken: cancellationToken);

        string blobName = string.Create(
            provider: CultureInfo.InvariantCulture,
            handler: $"{productId}/{Guid.CreateVersion7()}-{fileName}");

        BlobClient blob = container.GetBlobClient(blobName: blobName);

        using MemoryStream stream = new(buffer: content, writable: false);
        await blob.UploadAsync(
            content: stream,
            httpHeaders: new BlobHttpHeaders { ContentType = contentType },
            cancellationToken: cancellationToken);

        return BuildPublicUrl(blobName: blobName, blobUri: blob.Uri);
    }

    /// <inheritdoc />
    public async ValueTask DeleteAsync(string imageUrl, CancellationToken cancellationToken = default)
    {
        BlobContainerClient container = blobServiceClient.GetBlobContainerClient(blobContainerName: options.ContainerName);
        string blobName = ExtractBlobName(containerName: options.ContainerName, imageUrl: imageUrl);

        await container.GetBlobClient(blobName: blobName)
            .DeleteIfExistsAsync(cancellationToken: cancellationToken);
    }

    // Azurite's blob endpoint inside the Aspire container network is not an address a browser can
    // load an <img> from — PublicBaseUrl rewrites onto the host-mapped one for local development. A
    // real storage account has a single public DNS name already, so PublicBaseUrl stays unset there
    // and the client's own URI is used as-is.
    private string BuildPublicUrl(string blobName, Uri blobUri) =>
        options.PublicBaseUrl is null
            ? blobUri.ToString()
            : string.Create(
                provider: CultureInfo.InvariantCulture,
                handler: $"{options.PublicBaseUrl.TrimEnd('/')}/{options.ContainerName}/{blobName}");

    private static string ExtractBlobName(string containerName, string imageUrl)
    {
        string marker = string.Create(provider: CultureInfo.InvariantCulture, handler: $"/{containerName}/");
        int index = imageUrl.IndexOf(value: marker, comparisonType: StringComparison.Ordinal);

        return index < 0
            ? imageUrl
            : imageUrl[(index + marker.Length)..];
    }
}
