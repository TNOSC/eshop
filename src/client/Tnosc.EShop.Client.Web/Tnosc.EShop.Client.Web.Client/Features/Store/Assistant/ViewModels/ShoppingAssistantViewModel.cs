// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Tnosc.EShop.Client.Web.Client.Features.Store.Assistant.ViewModels;

/// <summary>
/// The assistant panel's state: the conversation so far, the thread it belongs to, and the draft the
/// shopper is typing.
/// </summary>
public sealed class ShoppingAssistantViewModel
{
    /// <summary>
    /// Gets the conversation's continuation identifier, generated once per panel.
    /// </summary>
    /// <remarks>
    /// Version 7 so it is unique without being guessable in sequence. It is not a credential either
    /// way — the agent host isolates conversations by the caller's Keycloak subject, so presenting
    /// another shopper's thread id does not reach their conversation.
    /// </remarks>
    public string ThreadId { get; } = Guid.CreateVersion7().ToString();

    /// <summary>Gets the conversation so far, oldest turn first.</summary>
    public IList<ChatMessageViewModel> Messages { get; } = [];

    /// <summary>Gets or sets the question the shopper is currently typing.</summary>
    [Required(ErrorMessage = "Type a question first.")]
    [StringLength(maximumLength: 1000, ErrorMessage = "Keep the question under 1000 characters.")]
    public string? Draft { get; set; }
}
