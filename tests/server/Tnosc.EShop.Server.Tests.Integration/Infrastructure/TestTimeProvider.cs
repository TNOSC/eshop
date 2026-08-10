// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC). All rights reserved.
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;

namespace Tnosc.EShop.Server.Tests.Integration.Infrastructure;

/// <summary>
/// A manually-advanced <see cref="TimeProvider"/> stub, registered in place of
/// <see cref="TimeProvider.System"/> in the integration test service provider so backoff and
/// audit-stamping assertions never depend on wall-clock timing or <c>Task.Delay</c>.
/// </summary>
public sealed class TestTimeProvider : TimeProvider
{
    private DateTimeOffset _utcNow = DateTimeOffset.UtcNow;

    /// <inheritdoc />
    public override DateTimeOffset GetUtcNow() => _utcNow;

    /// <summary>
    /// Sets the value <see cref="GetUtcNow"/> returns.
    /// </summary>
    /// <param name="utcNow">The UTC date and time to report from now on.</param>
    public void SetUtcNow(DateTimeOffset utcNow) => _utcNow = utcNow;

    /// <summary>
    /// Advances the value <see cref="GetUtcNow"/> returns by <paramref name="delta"/>.
    /// </summary>
    /// <param name="delta">The amount of time to advance by.</param>
    public void Advance(TimeSpan delta) => _utcNow += delta;
}
