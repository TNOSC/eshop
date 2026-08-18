// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.FluentUI.AspNetCore.Components;
using Tnosc.EShop.Client.Web.Client.Infrastructure.Errors;
using Tnosc.EShop.Client.Web.Client.Infrastructure.Validation;
using Tnosc.EShop.Client.Web.Contracts.Catalog;
using Tnosc.Lib.Web.Components.Shared;
using Tnosc.Lib.Web.Contracts;
using Tnosc.Lib.Web.Errors;
using Tnosc.Lib.Web.Results;
using Tnosc.EShop.Client.Web.Client.Features.Admin.Catalog.Services;
using Tnosc.EShop.Client.Web.Client.Features.Admin.Catalog.ViewModels;

namespace Tnosc.EShop.Client.Web.Client.Features.Admin.Catalog.Components;

/// <summary>
/// Creates a product. Requires an <c>Idempotency-Key</c> — see <see cref="_submissionKey"/> for why
/// it lives in component state rather than a <c>DelegatingHandler</c>. Validation and mapping are
/// <see cref="ICreateProductService"/>'s responsibility — this class only wires the form to it.
/// </summary>
public partial class CreateProductDialog : ComponentBase
{
    private static readonly string[] Currencies = ["USD", "EUR", "TND"];

    private readonly CreateProductViewModel _viewModel = new();
    private readonly List<string> _unmappedMessages = [];

    private EditContext _editContext = default!;
    private ValidationMessageStore _messageStore = default!;
    private IReadOnlyList<Category> _categories = [];
    private ComponentState _state = ComponentState.Loading;
    private ClientProblem? _categoriesProblem;
    private bool _isSubmitting;

    // A key minted per open dialog, rotated only once a response arrives — a user-driven retry
    // (clicking Create again after a business failure) is a new logical request and gets a new key;
    // a transport failure keeps the same key, so a retried send replays rather than duplicating.
    private Guid _submissionKey = Guid.CreateVersion7();

    [CascadingParameter]
    public IDialogInstance Dialog { get; set; } = default!;

    [Inject]
    public ICreateProductService Service { get; set; } = null!;

    [Inject]
    public INotificationService Notifications { get; set; } = null!;

    [Inject]
    public NavigationManager Navigation { get; set; } = null!;

    protected override void OnInitialized()
    {
        _editContext = new EditContext(model: _viewModel);
        _messageStore = new ValidationMessageStore(editContext: _editContext);
    }

    protected override async Task OnInitializedAsync()
    {
        ClientResult<IReadOnlyList<Category>> result = await Service.GetCategoriesAsync(
            cancellationToken: CancellationToken.None);

        if (result.IsSuccess)
        {
            _categories = result.Value;
        }
        else
        {
            _categoriesProblem = result.Problem;
        }

        _state = ComponentState.Content;
    }

    private async Task SubmitAsync()
    {
        // OnSubmit runs unconditionally, unlike OnValidSubmit — required so a server-side message
        // from a previous attempt can be cleared here. If it stayed in the store until a validated
        // submit, EditContext.Validate() would never come back clean and OnValidSubmit would never
        // fire again, deadlocking the form after the first server-side rejection.
        _unmappedMessages.Clear();
        _messageStore.Clear();
        _editContext.NotifyValidationStateChanged();

        if (!_editContext.Validate())
        {
            return;
        }

        _isSubmitting = true;

        try
        {
            ClientResult<Guid> result = await Service.SubmitAsync(
                viewModel: _viewModel,
                idempotencyKey: _submissionKey,
                cancellationToken: CancellationToken.None);

            // A response arrived — success or business failure — so the next attempt is a new
            // logical request and gets a new key.
            _submissionKey = Guid.CreateVersion7();

            if (result.IsSuccess)
            {
                await Notifications.ShowSuccessToastAsync(title: "Product created", message: _viewModel.Sku);
                await Dialog.CloseAsync(result: result.Value);
                return;
            }

            await HandleFailureAsync(problem: result.Problem!);
        }
        catch (HttpRequestException)
        {
            // No response arrived — the key is deliberately NOT rotated here, so a retry replays
            // this exact request instead of minting a second product.
            await Notifications.ShowErrorToastAsync(
                title: "Something went wrong",
                message: "Could not reach the server. Try again.");
        }
        finally
        {
            _isSubmitting = false;
        }
    }

    private async Task HandleFailureAsync(ClientProblem problem)
    {
        if (problem.Status is 400 or 409)
        {
            ClientValidation.ApplyFieldErrors(
                problem: problem,
                editContext: _editContext,
                messageStore: _messageStore,
                unmappedMessages: _unmappedMessages);
            return;
        }

        await NotificationExtensions.NotifyFailureAsync(problem: problem, notifications: Notifications, navigation: Navigation, humanize: ErrorCodeMessages.Humanize);
    }

    private async Task CancelAsync() => await Dialog.CancelAsync();
}
