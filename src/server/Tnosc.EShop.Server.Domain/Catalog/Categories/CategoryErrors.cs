// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC). All rights reserved.
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using Tnosc.Lib.Domain.Results;

namespace Tnosc.EShop.Server.Domain.Catalog.Categories;

/// <summary>
/// Every failure a caller can get back about a <see cref="Category"/>, defined once.
/// </summary>
public static class CategoryErrors
{
    /// <summary>
    /// No category carries the requested identifier.
    /// </summary>
    /// <param name="categoryId">The identifier that was looked up.</param>
    public static Error NotFound(Guid categoryId) => Error.NotFound(
        code: "Category.NotFound",
        description: $"Category {categoryId} was not found.");

    /// <summary>
    /// Gets the error returned when a category name is missing.
    /// </summary>
    public static Error NameRequired => Error.Validation(
        code: "Category.NameRequired",
        description: "A category name is required.");
}
