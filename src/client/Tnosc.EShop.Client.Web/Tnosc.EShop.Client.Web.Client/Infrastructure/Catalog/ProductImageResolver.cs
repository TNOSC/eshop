// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;

namespace Tnosc.EShop.Client.Web.Client.Infrastructure.Catalog;

/// <summary>
/// Resolves a stable illustrative image URL for a product from its SKU, standing in for a product
/// image the Catalog API does not expose. Deterministic, so a product shows the same image on the
/// card and on its detail page.
/// </summary>
public static class ProductImageResolver
{
    private const int ImageCount = 12;
    private const string Placeholder = "images/products/placeholder.svg";

    /// <summary>Resolves the relative image URL for the given SKU, falling back to a placeholder.</summary>
    /// <param name="sku">The product's SKU, or <see langword="null"/>/blank when unknown.</param>
    public static string For(string? sku)
    {
        if (string.IsNullOrWhiteSpace(value: sku))
        {
            return Placeholder;
        }

        int hash = Fnv1aHash(value: sku);
        int index = Math.Abs(hash % ImageCount) + 1;

        return $"images/products/{index:D2}.webp";
    }

    /// <summary>
    /// Hand-rolled FNV-1a over the SKU's ordinal bytes. Deterministic across processes, unlike
    /// <see cref="string.GetHashCode()"/>, which is randomised per process and would give the
    /// server-prerendered and WASM renders different images.
    /// </summary>
    private static int Fnv1aHash(string value)
    {
        const uint FnvOffsetBasis = 2166136261;
        const uint FnvPrime = 16777619;

        uint hash = FnvOffsetBasis;

        foreach (char character in value)
        {
            hash ^= character;
            hash *= FnvPrime;
        }

        return unchecked((int)hash);
    }
}
