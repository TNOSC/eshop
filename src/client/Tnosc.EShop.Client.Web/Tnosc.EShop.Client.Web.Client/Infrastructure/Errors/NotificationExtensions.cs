// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.FluentUI.AspNetCore.Components;
using Tnosc.Lib.Web.Contracts;

namespace Tnosc.EShop.Client.Web.Client.Infrastructure.Errors;

/// <summary>
/// Routes a failed API call to the right presentation, by status code — a redirect for an expired
/// session, a toast for everything else a caller can act on. Field-level validation (400 with an
/// <c>errors</c> dictionary) and the duplicate-SKU conflict are handled separately, by
/// <see cref="ValidationCodeFieldMap"/>, before a caller falls back to this for what is left.
/// </summary>
internal static class NotificationExtensions
{
    /// <summary>Presents a failed API call's problem details.</summary>
    /// <param name="problem">The problem returned by the failed call.</param>
    /// <param name="notifications">The toast service.</param>
    /// <param name="navigation">Used to redirect to login on a 401.</param>
    public static async Task NotifyFailureAsync(
        ApiProblem problem,
        INotificationService notifications,
        NavigationManager navigation)
    {
        switch (problem.Status)
        {
            case 401:
                // forceLoad is mandatory here too — see RedirectToLogin, which this mirrors for a
                // mid-page API failure rather than a route-level authorization failure.
                navigation.NavigateTo(
                    uri: $"bff/login?returnUrl={Uri.EscapeDataString(stringToEscape: navigation.Uri)}",
                    forceLoad: true);
                return;

            case 403:
                await notifications.ShowErrorToastAsync(title: "Not permitted", message: ErrorCodeMessages.Humanize(problem: problem));
                return;

            case 409:
                await notifications.ShowWarningToastAsync(title: "Conflict", message: ErrorCodeMessages.Humanize(problem: problem));
                return;

            default:
                await notifications.ShowErrorToastAsync(
                    title: "Something went wrong",
                    message: problem.TraceId is { } traceId
                        ? $"Reference {traceId}"
                        : ErrorCodeMessages.Humanize(problem: problem));
                return;
        }
    }
}
