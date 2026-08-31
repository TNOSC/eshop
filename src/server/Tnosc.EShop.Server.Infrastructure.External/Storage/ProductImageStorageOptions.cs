// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System.ComponentModel.DataAnnotations;

namespace Tnosc.EShop.Server.Infrastructure.External.Storage;

/// <summary>
/// Bounds where product images are physically stored, bound from the <c>"ProductImageStorage"</c>
/// configuration section.
/// </summary>
public sealed class ProductImageStorageOptions
{
    /// <summary>
    /// The configuration section this class binds to.
    /// </summary>
    public const string SectionName = "ProductImageStorage";

    /// <summary>
    /// Gets or sets the blob container product images are uploaded into. Created on first use with
    /// public blob-level read access if it does not already exist. Defaults to
    /// <c>"product-images"</c>.
    /// </summary>
    [Required]
    public string ContainerName { get; set; } = "product-images";

    /// <summary>
    /// Gets or sets the browser-reachable base URL to rewrite an uploaded blob's URL onto — the same
    /// class of problem as the AppHost's Keycloak <c>KC_HOSTNAME</c> pinning: Azurite's endpoint
    /// inside the Aspire container network is not an address a browser can load an <c>&lt;img&gt;</c>
    /// from. Set for local development (e.g. <c>http://127.0.0.1:10000/devstoreaccount1</c>) by the
    /// AppHost; left <see langword="null"/> in production, where <see cref="Azure.Storage.Blobs.BlobClient.Uri"/>
    /// is already a single public DNS name a browser can reach directly.
    /// </summary>
    public string? PublicBaseUrl { get; set; }
}
