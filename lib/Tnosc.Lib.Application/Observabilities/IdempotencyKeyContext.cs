// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC). All rights reserved.
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System.Threading;

namespace Tnosc.Lib.Application.Observabilities;

/// <summary>
/// Ambient, async-flowing idempotency key for the current logical call, so the caller's
/// <c>Idempotency-Key</c> reaches the decorator pipeline without every command, endpoint and handler
/// signature having to carry it.
/// </summary>
/// <remarks>
/// Populated from the <c>Idempotency-Key</c> request header by the host's request-context
/// middleware, exactly as <see cref="CorrelationIdContext"/> is populated from <c>Correlation-Id</c>.
/// The setter is public on purpose: a background job, an integration test or any other non-HTTP
/// caller supplies its own key the same way, which is what keeps
/// <see cref="Attributes.IdempotentAttribute"/> usable outside a request.
/// </remarks>
public static class IdempotencyKeyContext
{
    private static readonly AsyncLocal<string?> _current = new();

    /// <summary>
    /// Gets or sets the idempotency key for the current logical call context (for example, the
    /// current HTTP request), or <see langword="null"/> when the caller supplied none.
    /// </summary>
    public static string? Current
    {
        get => _current.Value;
        set => _current.Value = value;
    }
}
