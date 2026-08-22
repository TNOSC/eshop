// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using Microsoft.AspNetCore.Components;

namespace Tnosc.EShop.Client.Web.Client.Features.Store.Assistant.Components;

/// <summary>
/// One turn of the assistant conversation, rendered as a bubble.
/// </summary>
/// <remarks>
/// Presentational: every parameter is a primitive, so this component owns no view model and no
/// service of its own — the panel that hosts it maps a <c>ChatMessageViewModel</c> onto these.
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
}
