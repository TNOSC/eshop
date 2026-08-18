// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.FluentUI.AspNetCore.Components;
using Tnosc.EShop.Client.Web.Client.Features.Admin.Identity.Services;
using Tnosc.EShop.Client.Web.Client.Features.Admin.Identity.ViewModels;
using Tnosc.EShop.Client.Web.Client.Infrastructure.Errors;
using Tnosc.Lib.Web.Components.Shared;
using Tnosc.Lib.Web.Contracts;
using Tnosc.Lib.Web.Errors;
using Tnosc.Lib.Web.Results;

namespace Tnosc.EShop.Client.Web.Client.Features.Admin.Identity.Pages;

/// <summary>An operator's view of one customer: profile, addresses and deactivation. Fetching and
/// mutation are <see cref="IAdminCustomerDetailService"/>'s responsibility.</summary>
public partial class AdminCustomerDetailPage : ComponentBase
{
    private readonly CustomerProfileViewModel _profileModel = new();
    private readonly CustomerAddressViewModel _addressModel = new();

    private EditContext _profileEditContext = default!;
    private EditContext _addressEditContext = default!;
    private CustomerDetailViewModel? _customer;
    private ClientProblem? _problem;
    private ComponentState _state = ComponentState.Loading;
    private bool _isSavingProfile;
    private bool _isAddingAddress;

    [Inject]
    public IAdminCustomerDetailService Service { get; set; } = null!;

    [Inject]
    public IDialogService DialogService { get; set; } = null!;

    [Inject]
    public INotificationService Notifications { get; set; } = null!;

    [Inject]
    public NavigationManager Navigation { get; set; } = null!;

    [Parameter]
    public Guid Id { get; set; }

    protected override void OnInitialized()
    {
        _profileEditContext = new EditContext(model: _profileModel);
        _addressEditContext = new EditContext(model: _addressModel);
    }

    protected override Task OnParametersSetAsync() => LoadAsync();

    private async Task LoadAsync()
    {
        _state = ComponentState.Loading;

        ClientResult<CustomerDetailViewModel> result = await Service.GetCustomerByIdAsync(id: Id, cancellationToken: CancellationToken.None);

        if (result.IsSuccess)
        {
            _customer = result.Value;
            _problem = null;
            _profileModel.FirstName = _customer.FirstName;
            _profileModel.LastName = _customer.LastName;
            _profileModel.PhoneNumber = _customer.PhoneNumber;
        }
        else
        {
            _problem = result.Problem;
            _customer = null;
        }

        _state = ComponentState.Content;
    }

    private async Task SaveProfileAsync()
    {
        _isSavingProfile = true;

        try
        {
            ClientResult result = await Service.SaveProfileAsync(
                id: Id,
                viewModel: _profileModel,
                cancellationToken: CancellationToken.None);

            if (result.IsSuccess)
            {
                await Notifications.ShowSuccessToastAsync(title: "Profile updated", message: _customer!.Email);
                await LoadAsync();
                return;
            }

            await NotificationExtensions.NotifyFailureAsync(problem: result.Problem!, notifications: Notifications, navigation: Navigation, humanize: ErrorCodeMessages.Humanize);
        }
        finally
        {
            _isSavingProfile = false;
        }
    }

    private async Task AddAddressAsync()
    {
        _isAddingAddress = true;

        try
        {
            ClientResult<Guid> result = await Service.AddAddressAsync(
                id: Id,
                viewModel: _addressModel,
                cancellationToken: CancellationToken.None);

            if (result.IsSuccess)
            {
                _addressModel.Street = string.Empty;
                _addressModel.City = string.Empty;
                _addressModel.PostalCode = string.Empty;
                _addressModel.Country = string.Empty;
                await Notifications.ShowSuccessToastAsync(title: "Address added", message: _customer!.Email);
                await LoadAsync();
                return;
            }

            await NotificationExtensions.NotifyFailureAsync(problem: result.Problem!, notifications: Notifications, navigation: Navigation, humanize: ErrorCodeMessages.Humanize);
        }
        finally
        {
            _isAddingAddress = false;
        }
    }

    private async Task SetDefaultAddressAsync(CustomerAddressListItemViewModel address)
    {
        ClientResult result = await Service.SetDefaultAddressAsync(
            id: Id,
            addressId: address.Id,
            cancellationToken: CancellationToken.None);

        if (result.IsSuccess)
        {
            await LoadAsync();
            return;
        }

        await NotificationExtensions.NotifyFailureAsync(problem: result.Problem!, notifications: Notifications, navigation: Navigation, humanize: ErrorCodeMessages.Humanize);
    }

    private async Task RemoveAddressAsync(CustomerAddressListItemViewModel address)
    {
        DialogResult confirmation = await DialogService.ShowConfirmationAsync(
            message: $"Remove the address at {address.Street}?",
            title: "Remove address");

        if (confirmation.Cancelled)
        {
            return;
        }

        ClientResult result = await Service.RemoveAddressAsync(
            id: Id,
            addressId: address.Id,
            cancellationToken: CancellationToken.None);

        if (result.IsSuccess)
        {
            await LoadAsync();
            return;
        }

        await NotificationExtensions.NotifyFailureAsync(problem: result.Problem!, notifications: Notifications, navigation: Navigation, humanize: ErrorCodeMessages.Humanize);
    }

    private async Task DeactivateCustomerAsync()
    {
        DialogResult confirmation = await DialogService.ShowConfirmationAsync(
            message: "Deactivate this customer's profile?",
            title: "Deactivate customer");

        if (confirmation.Cancelled)
        {
            return;
        }

        ClientResult result = await Service.DeactivateCustomerAsync(id: Id, cancellationToken: CancellationToken.None);

        if (result.IsSuccess)
        {
            await Notifications.ShowSuccessToastAsync(title: "Customer deactivated", message: _customer!.Email);
            await LoadAsync();
            return;
        }

        if (result.Problem!.Status == 409)
        {
            await Notifications.ShowWarningToastAsync(
                title: "Conflict",
                message: ErrorCodeMessages.Humanize(problem: result.Problem));
            return;
        }

        await NotificationExtensions.NotifyFailureAsync(problem: result.Problem, notifications: Notifications, navigation: Navigation, humanize: ErrorCodeMessages.Humanize);
    }
}
