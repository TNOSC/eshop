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
using Tnosc.EShop.Client.Web.Client.Infrastructure.Api;
using Tnosc.EShop.Client.Web.Client.Infrastructure.Errors;
using Tnosc.EShop.Client.Web.Contracts.Catalog;
using Tnosc.Lib.Web.Api;
using Tnosc.Lib.Web.Contracts;
using Tnosc.Lib.Web.Errors;

namespace Tnosc.EShop.Client.Web.Client.Features.Admin.Catalog;

/// <summary>
/// Creates a product. Requires an <c>Idempotency-Key</c> — see <see cref="_submissionKey"/> for why
/// it lives in component state rather than a <c>DelegatingHandler</c>.
/// </summary>
public partial class CreateProductDialog : ComponentBase
{
    private static readonly string[] Currencies = ["USD", "EUR", "TND"];

    private static readonly IReadOnlyDictionary<string, string[]> EmptyErrors =
        new Dictionary<string, string[]>(comparer: StringComparer.Ordinal);

    private readonly CreateProductModel _model = new();
    private readonly List<string> _unmappedMessages = [];

    private EditContext _editContext = default!;
    private ValidationMessageStore _messageStore = default!;
    private IReadOnlyList<Category> _categories = [];
    private bool _isSubmitting;

    // A key minted per open dialog, rotated only once a response arrives — a user-driven retry
    // (clicking Create again after a business failure) is a new logical request and gets a new key;
    // a transport failure keeps the same key, so a retried send replays rather than duplicating.
    private Guid _submissionKey = Guid.CreateVersion7();

    [CascadingParameter]
    public IDialogInstance Dialog { get; set; } = default!;

    [Inject]
    public ICatalogApi CatalogApi { get; set; } = null!;

    [Inject]
    public INotificationService Notifications { get; set; } = null!;

    [Inject]
    public NavigationManager Navigation { get; set; } = null!;

    protected override void OnInitialized()
    {
        _editContext = new EditContext(model: _model);
        _messageStore = new ValidationMessageStore(editContext: _editContext);
    }

    protected override async Task OnInitializedAsync()
    {
        ApiResult<IReadOnlyList<Category>> result = await CatalogApi.GetCategoriesAsync(
            cancellationToken: CancellationToken.None);

        if (result.IsSuccess)
        {
            _categories = result.Value;
        }
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
            ApiResult<Guid> result = await CatalogApi.CreateProductAsync(
                request: _model.ToRequest(),
                idempotencyKey: _submissionKey,
                cancellationToken: CancellationToken.None);

            // A response arrived — success or business failure — so the next attempt is a new
            // logical request and gets a new key.
            _submissionKey = Guid.CreateVersion7();

            if (result.IsSuccess)
            {
                await Notifications.ShowSuccessToastAsync(title: "Product created", message: _model.Sku);
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

    private async Task HandleFailureAsync(ApiProblem problem)
    {
        if (problem.Status is 400 or 409)
        {
            ApplyServerValidation(problem: problem);
            return;
        }

        await NotificationExtensions.NotifyFailureAsync(problem: problem, notifications: Notifications, navigation: Navigation, humanize: ErrorCodeMessages.Humanize);
    }

    private void ApplyServerValidation(ApiProblem problem)
    {
        IReadOnlyDictionary<string, string[]> errors = problem.Errors
            ?? (problem.Title is { } singleCode
                ? new Dictionary<string, string[]>(comparer: StringComparer.Ordinal)
                {
                    [singleCode] = [problem.Detail ?? singleCode],
                }
                : EmptyErrors);

        foreach ((string code, string[] messages) in errors)
        {
            if (ValidationCodeFieldMap.TryResolveField(errorCode: code, fieldName: out string? field))
            {
                _messageStore.Add(fieldIdentifier: _editContext.Field(fieldName: field), messages: messages);
            }
            else
            {
                _unmappedMessages.AddRange(collection: messages);
            }
        }

        _editContext.NotifyValidationStateChanged();
    }

    private async Task CancelAsync() => await Dialog.CancelAsync();
}
