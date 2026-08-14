// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System.Threading.Tasks;

namespace Tnosc.EShop.Client.Web.Client.Layout.Store;

public partial class ThemeToggle
{
    private async Task ToggleThemeAsync() => await ThemeService.SwitchThemeAsync();
}
