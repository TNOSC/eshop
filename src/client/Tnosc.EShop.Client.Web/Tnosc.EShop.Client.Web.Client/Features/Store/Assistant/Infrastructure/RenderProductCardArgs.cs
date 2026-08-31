// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using System.Collections.Generic;

namespace Tnosc.EShop.Client.Web.Client.Features.Store.Assistant.Infrastructure;

/// <summary>
/// The shape of a <see cref="AssistantToolNames.RenderProductCard"/> call's arguments: what the
/// declared tool's JSON schema describes, and what a matching <c>FunctionCallContent</c> is parsed
/// back into. One type on both sides of the wire keeps the schema and the parsing from drifting
/// apart.
/// </summary>
/// <param name="Products">The products the assistant chose to show, in the order to display them.</param>
internal sealed record RenderProductCardArgs(IReadOnlyList<RenderProductCardArgs.ProductArg> Products)
{
    /// <summary>One product's display data, as the model reports it from its own tool lookup.</summary>
    internal sealed record ProductArg(
        Guid Id,
        string Sku,
        string Name,
        decimal PriceAmount,
        string PriceCurrency,
        int StockQuantity);
}
