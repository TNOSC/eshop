// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;

namespace Tnosc.Lib.Infrastructure.Persistence.Publishers;

/// <summary>
/// The outcome of publishing a batch of domain events: every handler was attempted, and these are
/// the ones that threw.
/// </summary>
/// <remarks>
/// A report rather than an exception, deliberately. One handler failing says nothing about its
/// siblings, so aborting delivery on the first throw would punish handlers that have no relationship
/// to the failure. Returning the failures also lets the caller record <b>which</b> handler broke,
/// which is what makes a per-handler dead-letter queue possible at all.
/// </remarks>
/// <remarks>
/// Initializes a new instance of the <see cref="DomainEventDeliveryReport"/> class.
/// </remarks>
/// <param name="failures">The handlers that threw, in the order they were attempted.</param>
public sealed class DomainEventDeliveryReport(IReadOnlyList<DomainEventHandlerFailure> failures)
{
    /// <summary>
    /// A report for a batch in which every handler succeeded.
    /// </summary>
    public static readonly DomainEventDeliveryReport Success = new(failures: []);

    /// <summary>
    /// Gets the handlers that threw, in the order they were attempted. Empty when delivery was clean.
    /// </summary>
    public IReadOnlyList<DomainEventHandlerFailure> Failures { get; } = failures;

    /// <summary>
    /// Gets a value indicating whether any handler failed.
    /// </summary>
    public bool HasFailures => Failures.Count > 0;

    /// <summary>
    /// Builds a single human-readable summary of every failure, for the outbox message's error column.
    /// </summary>
    /// <returns>One line per failed handler.</returns>
    public string Describe() =>
        string.Join(
            separator: System.Environment.NewLine,
            values: Failures.Select(selector: static failure => $"[{failure.HandlerName}] {failure.Exception}"));
}
