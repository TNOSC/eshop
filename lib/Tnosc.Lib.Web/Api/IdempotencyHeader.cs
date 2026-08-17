// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

namespace Tnosc.Lib.Web.Api;

/// <summary>The HTTP header carrying an idempotency key on write requests.</summary>
public static class IdempotencyHeader
{
    /// <summary>The header name, matching the server's <c>Idempotency-Key</c> contract.</summary>
    public const string Name = "Idempotency-Key";
}
