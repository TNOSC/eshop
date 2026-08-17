// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using Microsoft.AspNetCore.Components;

namespace Tnosc.Lib.Web.Components.Shared;

/// <summary>Displays a generic error message inside a <c>FluentMessageBar</c>.</summary>
public partial class ErrorPanel : ComponentBase
{
    /// <summary>Gets or sets the error message to display.</summary>
    [Parameter]
    [EditorRequired]
    public string Message { get; set; } = string.Empty;
}
