// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC). All rights reserved.
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using Tnosc.Lib.Domain;
using Tnosc.Lib.Domain.Results;

namespace Tnosc.EShop.Server.Domain.Catalog.Brands;

/// <summary>
/// A manufacturer or label products are attributed to.
/// </summary>
public sealed class Brand : AggregateRoot<BrandId>
{
    /// <summary>
    /// The maximum number of characters a brand name may contain.
    /// </summary>
    public const int NameMaxLength = 200;

    private Brand()
    {
        // EF.
    }

    /// <summary>
    /// Gets the brand's display name.
    /// </summary>
    public string Name { get; private set; } = null!;

    /// <summary>
    /// Creates a brand.
    /// </summary>
    /// <param name="name">The brand's display name.</param>
    /// <returns>The created brand, or a <c>Brand.NameRequired</c> validation error.</returns>
    public static Result<Brand> Create(string? name)
    {
        if (string.IsNullOrWhiteSpace(value: name))
        {
            return BrandErrors.NameRequired;
        }

        var brand = new Brand
        {
            Id = BrandId.New(),
            Name = name,
        };

        brand.IncrementVersion();

        return brand;
    }

    /// <summary>
    /// Renames the brand.
    /// </summary>
    /// <param name="name">The brand's new display name.</param>
    /// <returns>Success, or a <c>Brand.NameRequired</c> validation error.</returns>
    public Result Rename(string? name)
    {
        if (string.IsNullOrWhiteSpace(value: name))
        {
            return BrandErrors.NameRequired;
        }

        Name = name;
        IncrementVersion();

        return Result.Success();
    }
}
