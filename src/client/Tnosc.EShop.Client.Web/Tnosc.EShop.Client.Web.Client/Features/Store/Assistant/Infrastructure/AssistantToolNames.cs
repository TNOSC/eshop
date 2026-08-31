// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

namespace Tnosc.EShop.Client.Web.Client.Features.Store.Assistant.Infrastructure;

/// <summary>
/// Names of the frontend (render-only) tools this client declares to the shopping assistant over
/// AG-UI. Shared between the declaration built in <c>ShoppingAssistantApi</c> and the match against
/// <c>FunctionCallContent.Name</c> in <c>ShoppingAssistantService</c>, so the two spellings cannot
/// drift apart — the same discipline as <c>McpToolNames</c> on the agent side.
/// </summary>
public static class AssistantToolNames
{
    /// <summary>
    /// A frontend tool the agent can call to have the shopper shown a product card instead of, or
    /// alongside, a sentence about the product. Never executed server-side.
    /// </summary>
    public const string RenderProductCard = "render_product_card";
}
