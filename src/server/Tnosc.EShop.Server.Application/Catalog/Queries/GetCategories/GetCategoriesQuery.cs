// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using Tnosc.Lib.Application.Queries;

namespace Tnosc.EShop.Server.Application.Catalog.Queries.GetCategories;

/// <summary>
/// Reads every catalogue category. Rarely changes, so its handler is cached and invalidated by the
/// <c>catalog</c> cache tag.
/// </summary>
/// <remarks>
/// The response is an array rather than <c>IReadOnlyCollection&lt;CategoryDto&gt;</c> because C# forbids
/// user-defined conversions to or from an interface type, and <c>Result&lt;T&gt;</c> is only
/// constructible through its implicit conversion from <c>T</c>.
/// </remarks>
public sealed record GetCategoriesQuery : IQuery<CategoryDto[]>;
