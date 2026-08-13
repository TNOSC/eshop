// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System.Text.Json;

namespace Tnosc.EShop.Server.Infrastructure.External.Redis.Basket;

/// <summary>
/// The single <see cref="JsonSerializerOptions"/> instance every basket document is written and read
/// with, so <see cref="RedisBasketRepository"/> and <see cref="RedisBasketReader"/> cannot drift onto
/// incompatible serialization shapes for the same stored document.
/// </summary>
internal static class BasketSerialization
{
    /// <summary>
    /// Gets the shared serializer options. Property names are left at their default (PascalCase, as
    /// declared) so the optimistic-concurrency Lua script in <see cref="RedisBasketRepository"/> can
    /// read the document's <c>Version</c> field by that same name.
    /// </summary>
    public static JsonSerializerOptions Options { get; } = new();
}
