// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System.Collections.Generic;
using Tnosc.EShop.Client.Web.Client.Features.Store.Catalog.ViewModels;

namespace Tnosc.EShop.Client.Web.Client.Features.Store.Assistant.ViewModels;

/// <summary>
/// One turn in the assistant conversation, as the panel renders it. Carries no DataAnnotations: only
/// the draft the shopper types is user input, and that lives on
/// <see cref="ShoppingAssistantViewModel"/>.
/// </summary>
public sealed class ChatMessageViewModel
{
    /// <summary>Gets a value indicating whether this turn was written by the shopper.</summary>
    public bool IsFromShopper { get; init; }

    /// <summary>Gets or sets the turn's text, appended to as the assistant's reply streams in.</summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether the assistant is looking something up rather than
    /// writing. Set while a tool call is in flight, which is the only moment a reply produces no text
    /// for a noticeable stretch — without it the panel looks stalled.
    /// </summary>
    public bool IsLookingUp { get; set; }

    /// <summary>
    /// Gets or sets the products the assistant chose to show alongside its text, from a
    /// <c>render_product_card</c> tool call. Empty for a turn that answered in prose only.
    /// </summary>
    public IReadOnlyList<ProductSummaryViewModel> Products { get; set; } = [];
}
