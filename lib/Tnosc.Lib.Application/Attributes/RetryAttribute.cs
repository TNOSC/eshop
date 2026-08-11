// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;

namespace Tnosc.Lib.Application.Attributes;

/// <summary>
/// Specifies retry behavior for a message handler or component.
/// </summary>
/// <remarks>
/// <para>
/// Only a <see cref="Exceptions.BaseException"/> whose <c>IsRetriable</c> is set ever triggers a
/// retry — a conflict or a validation failure is a decision, not a blip, and retrying it would only
/// delay the same answer.
/// </para>
/// <para>
/// On a <b>command or query</b> handler the attribute tunes a default that is already on (three
/// attempts). On a <b>domain event</b> handler it is what turns retrying on at all: without it the
/// handler gets one attempt and the failure goes to the outbox's durable retry. Keep the count small
/// there — the total in-process delay is <c>200ms · (2^(n-1) − 1)</c>, and a handler that retries for
/// longer than the outbox's claim lease outlives its own claim.
/// </para>
/// <para>
/// Retrying a handler that is not <see cref="IdempotentAttribute"/> can re-apply whatever a failed
/// attempt already committed. Pair the two when the handler writes anything.
/// </para>
/// </remarks>
[AttributeUsage(AttributeTargets.Class)]
public sealed class RetryAttribute : Attribute
{
    /// <summary>
    /// Gets the maximum number of attempts — <b>total</b>, not additional ones.
    /// </summary>
    /// <remarks>
    /// <c>[Retry(3)]</c> means the handler runs at most three times: the first attempt plus two
    /// retries. The name predates a second consumer and is kept rather than renamed, since the value
    /// is compared as <c>attempt &lt; maxAttempts</c> in every decorator that reads it.
    /// </remarks>
    public int MaxRetries { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="RetryAttribute"/> class.
    /// </summary>
    /// <param name="maxRetries">Maximum total attempts, first one included. Default is 3.</param>
    public RetryAttribute(int maxRetries = 3) => MaxRetries = maxRetries;
}
