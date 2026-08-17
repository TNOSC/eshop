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

namespace Tnosc.Lib.Web.Errors;

/// <summary>
/// Routes a failed API call to the right presentation, by status code — a redirect for an expired
/// session, a toast for everything else a caller can act on. Field-level validation (400 with an
/// <c>errors</c> dictionary) is expected to be handled separately, by the caller's own field-mapping
/// logic, before falling back to this for what is left.
/// </summary>
public static class NotificationExtensions
{
    /// <summary>Presents a failed API call's problem details.</summary>
    /// <param name="problem">The problem returned by the failed call.</param>
    /// <param name="notifications">The toast service.</param>
    /// <param name="navigation">Used to redirect to login on a 401.</param>
    /// <param name="humanize">Turns a problem into human-readable text for its host application's own error vocabulary.</param>
    /// <param name="loginRoute">
    /// The BFF login route to redirect to on a 401. Defaults to the <c>Tnosc.Lib.Web.Bff</c> convention;
    /// override when a host uses a different path.
    /// </param>
    public static async Task NotifyFailureAsync(
        ApiProblem problem,
        INotificationService notifications,
        NavigationManager navigation,
        Func<ApiProblem, string> humanize,
        string loginRoute = "bff/login")
    {
        ArgumentNullException.ThrowIfNull(argument: humanize);

        switch (problem.Status)
        {
            case 401:
                // forceLoad is mandatory here too — a mid-page API failure needs the same full-page
                // navigation a route-level authorization failure uses, so the server-rendered login
                // challenge runs instead of a client-side route change.
                navigation.NavigateTo(
                    uri: $"{loginRoute}?returnUrl={Uri.EscapeDataString(stringToEscape: navigation.Uri)}",
                    forceLoad: true);
                return;

            case 403:
                await notifications.ShowErrorToastAsync(title: "Not permitted", message: humanize(problem));
                return;

            case 409:
                await notifications.ShowWarningToastAsync(title: "Conflict", message: humanize(problem));
                return;

            default:
                await notifications.ShowErrorToastAsync(
                    title: "Something went wrong",
                    message: problem.TraceId is { } traceId
                        ? $"Reference {traceId}"
                        : humanize(problem));
                return;
        }
    }
}
