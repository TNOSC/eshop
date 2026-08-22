// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

namespace Tnosc.EShop.Server.Shared.Catalog;

/// <summary>
/// MCP tool names shared by the Catalog bounded context's <c>[McpServerTool]</c> methods and any
/// agent that references a tool by name, so the protocol-level name declared on the tool and the
/// name an agent's allow-list filters against cannot drift apart.
/// </summary>
/// <remarks>
/// Without an explicit <c>Name</c>, the MCP SDK derives a tool's protocol name from its C# method
/// name — snake_cased, including the <c>Async</c> suffix (<c>ListProductsAsync</c> becomes
/// <c>list_products_async</c>). Relying on that derivation instead of naming the tool explicitly
/// means the name is an implementation detail nobody chose, and any allow-list written against a
/// guessed name silently matches nothing.
/// </remarks>
public static class McpToolNames
{
    /// <summary>Lists products from the catalogue.</summary>
    public const string ListProducts = "catalog_list_products";

    /// <summary>Adds a new product to the catalogue.</summary>
    public const string CreateProduct = "catalog_create_product";
}
