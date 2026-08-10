// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC). All rights reserved.
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using Tnosc.Lib.Domain;
using Tnosc.Lib.Domain.Results;

namespace Tnosc.EShop.Server.Domain.Catalog.Categories;

/// <summary>
/// A grouping products are filed under for browsing and search.
/// </summary>
public sealed class Category : AggregateRoot<CategoryId>
{
    /// <summary>
    /// The maximum number of characters a category name may contain.
    /// </summary>
    public const int NameMaxLength = 200;

    private Category()
    {
        // EF.
    }

    /// <summary>
    /// Gets the category's display name.
    /// </summary>
    public string Name { get; private set; } = null!;

    /// <summary>
    /// Creates a category.
    /// </summary>
    /// <param name="name">The category's display name.</param>
    /// <returns>The created category, or a <c>Category.NameRequired</c> validation error.</returns>
    public static Result<Category> Create(string? name)
    {
        if (string.IsNullOrWhiteSpace(value: name))
        {
            return CategoryErrors.NameRequired;
        }

        var category = new Category
        {
            Id = CategoryId.New(),
            Name = name,
        };

        category.IncrementVersion();

        return category;
    }

    /// <summary>
    /// Renames the category.
    /// </summary>
    /// <param name="name">The category's new display name.</param>
    /// <returns>Success, or a <c>Category.NameRequired</c> validation error.</returns>
    public Result Rename(string? name)
    {
        if (string.IsNullOrWhiteSpace(value: name))
        {
            return CategoryErrors.NameRequired;
        }

        Name = name;
        IncrementVersion();

        return Result.Success();
    }
}
