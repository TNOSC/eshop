// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC). All rights reserved.
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;

namespace Tnosc.Lib.Infrastructure.Persistence.Idempotency;

/// <summary>
/// One idempotency key a command handler has answered, together with the response to replay when the
/// same key arrives again.
/// </summary>
/// <remarks>
/// Rows are written by <see cref="IdempotencyStore{TContext}"/> through raw, conflict-tolerant SQL
/// rather than the change tracker — a duplicate key must not throw, because a failed insert aborts
/// the transaction the claim shares with the handler's own writes. This type therefore exists to
/// model the table for migrations and to read a row back on replay, not to mutate one.
/// </remarks>
public sealed class IdempotencyRequest
{
    /// <summary>
    /// Initializes a new instance of the <see cref="IdempotencyRequest"/> class.
    /// </summary>
    /// <remarks>
    /// EF Core materializes rows through this constructor by matching parameter names to properties.
    /// Production code never calls it — claims are written as raw SQL — but a test that needs a row
    /// in a specific state can.
    /// </remarks>
    /// <param name="key">The caller-supplied idempotency key.</param>
    /// <param name="handler">The full type name of the handler the key is scoped to.</param>
    /// <param name="requestHash">The hash of the request payload that claimed the key.</param>
    /// <param name="response">The serialized response, or <see langword="null"/> when there is none.</param>
    /// <param name="responseType">The full type name the response was recorded as.</param>
    /// <param name="createdOnUtc">The UTC date and time at which the key was claimed.</param>
    /// <param name="expiresOnUtc">The UTC date and time from which the row may be collected.</param>
    public IdempotencyRequest(
        string key,
        string handler,
        string requestHash,
        string? response,
        string? responseType,
        DateTime createdOnUtc,
        DateTime expiresOnUtc)
    {
        Key = key;
        Handler = handler;
        RequestHash = requestHash;
        Response = response;
        ResponseType = responseType;
        CreatedOnUtc = createdOnUtc;
        ExpiresOnUtc = expiresOnUtc;
    }

    /// <summary>
    /// Gets the caller-supplied idempotency key.
    /// </summary>
    public string Key { get; }

    /// <summary>
    /// Gets the full type name of the handler this key is scoped to, so two handlers cannot collide
    /// on one caller-supplied key.
    /// </summary>
    public string Handler { get; }

    /// <summary>
    /// Gets the hash of the request payload that originally claimed this key, used to tell a genuine
    /// retry apart from the same key reused for different content.
    /// </summary>
    public string RequestHash { get; }

    /// <summary>
    /// Gets the serialized response recorded by the original run, or <see langword="null"/> for a
    /// handler that returns no response.
    /// </summary>
    public string? Response { get; }

    /// <summary>
    /// Gets the full type name the response was recorded as, used to refuse a replay whose shape no
    /// longer matches what the handler returns.
    /// </summary>
    public string? ResponseType { get; }

    /// <summary>
    /// Gets the UTC date and time at which the key was claimed.
    /// </summary>
    public DateTime CreatedOnUtc { get; }

    /// <summary>
    /// Gets the UTC date and time from which the row may be deleted by the cleanup service.
    /// </summary>
    /// <remarks>
    /// Expiry bounds the table's size; it does not license reuse of the key. A row that has expired
    /// but not yet been collected still blocks its key, which is the safe direction to fail in.
    /// </remarks>
    public DateTime ExpiresOnUtc { get; }
}
