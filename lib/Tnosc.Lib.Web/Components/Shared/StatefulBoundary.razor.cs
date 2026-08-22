// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using Microsoft.AspNetCore.Components;

namespace Tnosc.Lib.Web.Components.Shared;

/// <summary>
/// Renders a component's body according to its <see cref="ComponentState"/>: a <see cref="LoadingPanel"/>
/// while <see cref="ComponentState.Loading"/>, <see cref="ChildContent"/> once
/// <see cref="ComponentState.Content"/>, and an <see cref="ErrorPanel"/> — or a caller-supplied
/// <see cref="ErrorContent"/> — if rendering that content throws and is caught by the wrapped
/// <see cref="LoggingErrorBoundary"/>.
/// </summary>
public partial class StatefulBoundary : ComponentBase
{
    /// <summary>
    /// The fallback rendered while <see cref="State"/> is <see cref="ComponentState.Loading"/> and no
    /// <see cref="LoadingContent"/> was supplied. Built by hand rather than declared as markup because
    /// a <see cref="RenderFragment"/> field cannot be expressed in the <c>.razor</c> half without an
    /// inline code block, which this codebase keeps out of its views.
    /// </summary>
    private static readonly RenderFragment DefaultLoading = builder =>
    {
        builder.OpenComponent<LoadingPanel>(sequence: 0);
        builder.CloseComponent();
    };

    private LoggingErrorBoundary? _errorBoundary;

    /// <summary>Gets or sets the component's current lifecycle state.</summary>
    [Parameter]
    [EditorRequired]
    public ComponentState State { get; set; }

    /// <summary>Gets or sets the content rendered while <see cref="State"/> is <see cref="ComponentState.Content"/>.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>Gets or sets the content rendered while <see cref="State"/> is <see cref="ComponentState.Loading"/>. Defaults to a <see cref="LoadingPanel"/>.</summary>
    [Parameter]
    public RenderFragment? LoadingContent { get; set; }

    /// <summary>Gets or sets the content rendered when the wrapped boundary catches an exception. Defaults to an <see cref="ErrorPanel"/>.</summary>
    [Parameter]
    public RenderFragment<Exception>? ErrorContent { get; set; }

    /// <summary>Gets or sets the callback invoked with <see cref="ComponentState.Error"/> when the wrapped boundary catches an exception.</summary>
    [Parameter]
    public EventCallback<ComponentState> StateChanged { get; set; }

    /// <summary>Resets the wrapped error boundary so <see cref="ChildContent"/> renders again.</summary>
    public void Recover() => _errorBoundary?.Recover();

    /// <inheritdoc />
    protected override void OnAfterRender(bool firstRender)
    {
        if (firstRender && _errorBoundary is not null)
        {
            _errorBoundary.OnError = exception => _ = StateChanged.InvokeAsync(ComponentState.Error);
        }
    }
}
