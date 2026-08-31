// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Components;
using Tnosc.EShop.Client.Web.Client.Features.Store.Catalog.ViewModels;

namespace Tnosc.EShop.Client.Web.Client.Features.Store.Assistant.Components;

/// <summary>
/// One turn of the assistant conversation, rendered as a bubble.
/// </summary>
/// <remarks>
/// Presentational: no Contracts DTO reaches it — <see cref="Products"/> is the same display view
/// model <c>ProductCard</c> already takes — and it owns no view model or service of its own. The
/// panel that hosts it maps a <c>ChatMessageViewModel</c> onto these parameters.
/// </remarks>
public partial class ChatBubble : ComponentBase
{
    /// <summary>Gets or sets a value indicating whether this turn was written by the shopper.</summary>
    [Parameter]
    public bool IsFromShopper { get; set; }

    /// <summary>Gets or sets the turn's text.</summary>
    [Parameter]
    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether the assistant is looking something up rather than
    /// writing, which is rendered in place of the text.
    /// </summary>
    [Parameter]
    public bool IsLookingUp { get; set; }

    /// <summary>Gets or sets the products the assistant chose to show alongside this turn's text.</summary>
    [Parameter]
    public IReadOnlyList<ProductSummaryViewModel> Products { get; set; } = [];

    /// <summary>
    /// Gets or sets the callback invoked when the shopper adds one of <see cref="Products"/> to the
    /// basket from this bubble. Forwarded to the panel that hosts the conversation, which owns basket
    /// access — the bubble itself stays presentational.
    /// </summary>
    [Parameter]
    public EventCallback<Guid> OnAddToBasket { get; set; }
}
