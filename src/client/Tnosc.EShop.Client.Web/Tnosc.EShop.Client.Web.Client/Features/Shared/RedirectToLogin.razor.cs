// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;

namespace Tnosc.EShop.Client.Web.Client.Features.Shared;

public partial class RedirectToLogin
{
    protected override void OnInitialized()
    {
        // forceLoad is mandatory: without it, Blazor's enhanced navigation intercepts the request
        // and the OIDC challenge never reaches the server. BlazorDisableThrowNavigationException
        // makes NavigateTo return normally during static SSR instead of throwing rather than
        // aborting rendering, which is why nothing in this component depends on code after this call.
        Navigation.NavigateTo(
            uri: $"bff/login?returnUrl={Uri.EscapeDataString(stringToEscape: Navigation.Uri)}",
            forceLoad: true);
    }
}
