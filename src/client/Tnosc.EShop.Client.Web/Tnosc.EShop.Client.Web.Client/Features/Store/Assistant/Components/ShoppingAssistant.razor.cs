// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.FluentUI.AspNetCore.Components;
using Tnosc.EShop.Client.Web.Client.Features.Store.Assistant.Services;
using Tnosc.EShop.Client.Web.Client.Features.Store.Assistant.ViewModels;
using Tnosc.Lib.Web.Components.Shared;
using Tnosc.Lib.Web.Contracts;
using Tnosc.Lib.Web.Results;
using Icons = Microsoft.FluentUI.AspNetCore.Components.Icons;

namespace Tnosc.EShop.Client.Web.Client.Features.Store.Assistant.Components;

/// <summary>
/// The storefront's assistant widget: a launcher that opens a docked conversation panel.
/// </summary>
/// <remarks>
/// <para>
/// Thin by design. Every decision about what to send, how to read the stream and what a failure means
/// lives in <see cref="IShoppingAssistantService"/>, which is testable without rendering anything.
/// </para>
/// <para>
/// It lives in the layout rather than on a page so the conversation survives navigating around the
/// storefront — a shopper asking about a product they just walked past should not have to start over.
/// </para>
/// </remarks>
public partial class ShoppingAssistant : ComponentBase
{
    /// <summary>
    /// The key combinations that send the composer's contents, matching what a chat surface is
    /// expected to do: Enter sends, and Ctrl+Enter does too for anyone in the habit of it.
    /// </summary>
    private static readonly KeyPress[] SendKeys =
    [
        KeyPress.For(KeyCode.Enter),
        KeyPress.For(KeyCode.Enter).AndCtrlKey(),
    ];

    private readonly ShoppingAssistantViewModel _viewModel = new();

    private ClientProblem? _problem;
    private bool _isOpen;
    private bool _isSending;

    // Nothing loads before the panel is usable, so it opens straight into Content. StatefulBoundary
    // still earns its place: it is what turns an exception thrown while rendering a reply into a
    // contained error instead of a torn-down layout.
    private readonly ComponentState _state = ComponentState.Content;

    /// <summary>Gets the injected assistant service.</summary>
    [Inject]
    public IShoppingAssistantService Service { get; set; } = null!;

    private Icon LauncherIcon => _isOpen
        ? new Icons.Regular.Size24.Dismiss()
        : new Icons.Regular.Size24.ChatSparkle();

    private void Toggle() => _isOpen = !_isOpen;

    private void Close() => _isOpen = false;

    private Task OnComposerKeyAsync(FluentKeyPressEventArgs args) => SendAsync();

    private async Task SendAsync()
    {
        if (_isSending)
        {
            return;
        }

        _isSending = true;
        _problem = null;

        ClientResult result = await Service.SendAsync(
            viewModel: _viewModel,
            onUpdatedAsync: OnStreamUpdatedAsync,
            cancellationToken: CancellationToken.None);

        // A returned problem is a normal outcome of a conversation turn, not a crash: it renders as
        // content through ErrorPanel. Only StatefulBoundary's own error boundary sets ComponentState.Error.
        _problem = result.IsSuccess ? null : result.Problem;
        _isSending = false;
    }

    private Task OnStreamUpdatedAsync()
    {
        StateHasChanged();
        return Task.CompletedTask;
    }
}
