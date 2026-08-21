// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.FluentUI.AspNetCore.Components;
using Tnosc.EShop.Client.Web.Client.Features.Store.Profile.Services;
using Tnosc.EShop.Client.Web.Client.Features.Store.Profile.ViewModels;
using Tnosc.EShop.Client.Web.Client.Infrastructure.Errors;
using Tnosc.EShop.Client.Web.Client.Infrastructure.Validation;
using Tnosc.Lib.Web.Components.Shared;
using Tnosc.Lib.Web.Contracts;
using Tnosc.Lib.Web.Errors;
using Tnosc.Lib.Web.Results;

namespace Tnosc.EShop.Client.Web.Client.Features.Store.Profile.Pages;

/// <summary>The caller's self-service view of their own profile: name, phone and delivery
/// addresses. Fetching and mutation are <see cref="IMyProfileService"/>'s responsibility.</summary>
public partial class MyProfilePage : ComponentBase
{
    private readonly MyProfileFormViewModel _profileModel = new();
    private readonly MyAddressFormViewModel _addressModel = new();
    private readonly List<string> _profileUnmappedMessages = [];
    private readonly List<string> _addressUnmappedMessages = [];

    private EditContext _profileEditContext = default!;
    private EditContext _addressEditContext = default!;
    private ValidationMessageStore _profileMessageStore = default!;
    private ValidationMessageStore _addressMessageStore = default!;
    private MyProfileViewModel? _profile;
    private ClientProblem? _problem;
    private ComponentState _state = ComponentState.Loading;
    private bool _isSavingProfile;
    private bool _isAddingAddress;

    [Inject]
    public IMyProfileService Service { get; set; } = null!;

    [Inject]
    public IDialogService DialogService { get; set; } = null!;

    [Inject]
    public INotificationService Notifications { get; set; } = null!;

    [Inject]
    public NavigationManager Navigation { get; set; } = null!;

    protected override void OnInitialized()
    {
        _profileEditContext = new EditContext(model: _profileModel);
        _profileMessageStore = new ValidationMessageStore(editContext: _profileEditContext);
        _addressEditContext = new EditContext(model: _addressModel);
        _addressMessageStore = new ValidationMessageStore(editContext: _addressEditContext);
    }

    protected override Task OnParametersSetAsync() => LoadAsync();

    private async Task LoadAsync()
    {
        _state = ComponentState.Loading;

        ClientResult<MyProfileViewModel> result = await Service.GetMyProfileAsync(cancellationToken: CancellationToken.None);

        if (result.IsSuccess)
        {
            _profile = result.Value;
            _problem = null;
            _profileModel.FirstName = _profile.FirstName;
            _profileModel.LastName = _profile.LastName;
            _profileModel.PhoneNumber = _profile.PhoneNumber;
        }
        else
        {
            _problem = result.Problem;
            _profile = null;
        }

        _state = ComponentState.Content;
    }

    private async Task SaveProfileAsync()
    {
        // OnSubmit runs unconditionally, unlike OnValidSubmit — required so a server-side message
        // from a previous attempt can be cleared here. If it stayed in the store until a validated
        // submit, EditContext.Validate() would never come back clean and OnValidSubmit would never
        // fire again, deadlocking the form after the first server-side rejection.
        _profileUnmappedMessages.Clear();
        _profileMessageStore.Clear();
        _profileEditContext.NotifyValidationStateChanged();

        if (!_profileEditContext.Validate())
        {
            return;
        }

        _isSavingProfile = true;

        try
        {
            ClientResult result = await Service.SaveProfileAsync(viewModel: _profileModel, cancellationToken: CancellationToken.None);

            if (result.IsSuccess)
            {
                await Notifications.ShowSuccessToastAsync(title: "Profile updated", message: "Your changes were saved.");
                await LoadAsync();
                return;
            }

            await HandleProfileFailureAsync(problem: result.Problem!);
        }
        finally
        {
            _isSavingProfile = false;
        }
    }

    private async Task HandleProfileFailureAsync(ClientProblem problem)
    {
        if (problem.Status is 400 or 409)
        {
            ClientValidation.ApplyFieldErrors(
                problem: problem,
                editContext: _profileEditContext,
                messageStore: _profileMessageStore,
                unmappedMessages: _profileUnmappedMessages);
            return;
        }

        await NotificationExtensions.NotifyFailureAsync(problem: problem, notifications: Notifications, navigation: Navigation, humanize: ErrorCodeMessages.Humanize);
    }

    private async Task AddAddressAsync()
    {
        _addressUnmappedMessages.Clear();
        _addressMessageStore.Clear();
        _addressEditContext.NotifyValidationStateChanged();

        if (!_addressEditContext.Validate())
        {
            return;
        }

        _isAddingAddress = true;

        try
        {
            ClientResult<Guid> result = await Service.AddAddressAsync(viewModel: _addressModel, cancellationToken: CancellationToken.None);

            if (result.IsSuccess)
            {
                _addressModel.Street = string.Empty;
                _addressModel.City = string.Empty;
                _addressModel.PostalCode = string.Empty;
                _addressModel.Country = string.Empty;
                await Notifications.ShowSuccessToastAsync(title: "Address added", message: "Your delivery address was saved.");
                await LoadAsync();
                return;
            }

            await HandleAddressFailureAsync(problem: result.Problem!);
        }
        finally
        {
            _isAddingAddress = false;
        }
    }

    private async Task HandleAddressFailureAsync(ClientProblem problem)
    {
        if (problem.Status is 400 or 409)
        {
            ClientValidation.ApplyFieldErrors(
                problem: problem,
                editContext: _addressEditContext,
                messageStore: _addressMessageStore,
                unmappedMessages: _addressUnmappedMessages);
            return;
        }

        await NotificationExtensions.NotifyFailureAsync(problem: problem, notifications: Notifications, navigation: Navigation, humanize: ErrorCodeMessages.Humanize);
    }

    private async Task SetDefaultAddressAsync(MyAddressListItemViewModel address)
    {
        ClientResult result = await Service.SetDefaultAddressAsync(addressId: address.Id, cancellationToken: CancellationToken.None);

        if (result.IsSuccess)
        {
            await LoadAsync();
            return;
        }

        await NotificationExtensions.NotifyFailureAsync(problem: result.Problem!, notifications: Notifications, navigation: Navigation, humanize: ErrorCodeMessages.Humanize);
    }

    private async Task RemoveAddressAsync(MyAddressListItemViewModel address)
    {
        DialogResult confirmation = await DialogService.ShowConfirmationAsync(
            message: $"Remove the address at {address.Street}?",
            title: "Remove address");

        if (confirmation.Cancelled)
        {
            return;
        }

        ClientResult result = await Service.RemoveAddressAsync(addressId: address.Id, cancellationToken: CancellationToken.None);

        if (result.IsSuccess)
        {
            await LoadAsync();
            return;
        }

        await NotificationExtensions.NotifyFailureAsync(problem: result.Problem!, notifications: Notifications, navigation: Navigation, humanize: ErrorCodeMessages.Humanize);
    }
}
