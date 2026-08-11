// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;

namespace Tnosc.Lib.Application.Decorators;

/// <summary>
/// Resolves the real handler behind a chain of <see cref="IHandlerDecorator"/> wrappers, and the
/// durable name it is known by.
/// </summary>
/// <remarks>
/// <para>
/// The name this produces is a <b>contract between projects that cannot see each other</b>. The inbox
/// records it in <c>outbox.processed_events.handler</c> when a handler claims an event, and the
/// dead-letter queue records it in <c>outbox.dead_letters.handler</c> when one fails permanently —
/// written from <c>Tnosc.Lib.Application</c> and <c>Tnosc.Lib.Infrastructure.Persistence</c>
/// respectively. If the two ever computed it differently, a replayed dead letter would not match its
/// own inbox claim: nothing would fail, the handler would simply be skipped or re-run silently.
/// One definition, used by both sides, makes that impossible.
/// </para>
/// <para>
/// <b>Unwrapping is deliberately not cached by decorator type.</b> A closed decorator type does
/// identify one handler for commands and queries, where DI registers a single handler per closed
/// interface — but <b>not</b> for domain events, where several handlers subscribe to the same event
/// and therefore share the closed decorator type
/// <c>IdempotencyDecorator.DomainEventHandler&lt;TEvent&gt;</c>. Caching on it would hand every
/// sibling the first one's name and attributes: they would share a single inbox claim, so only one
/// of them would ever run. Walking the chain is a couple of type checks and no allocation — far
/// cheaper than the database round trip it precedes. Callers that want to memoise something derived
/// from it must key on the <b>unwrapped</b> type this returns, never on the decorator's.
/// </para>
/// </remarks>
public static class HandlerChain
{
    /// <summary>
    /// Unwraps a chain of <see cref="IHandlerDecorator"/> instances down to the concrete handler type.
    /// </summary>
    /// <param name="handler">The (possibly decorated) handler instance.</param>
    /// <returns>The concrete, unwrapped handler type.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="handler"/> is <see langword="null"/>.</exception>
    public static Type Unwrap(object handler)
    {
        ArgumentNullException.ThrowIfNull(argument: handler);

        object current = handler;

        while (current is IHandlerDecorator decorator)
        {
            current = decorator.InnerHandler;
        }

        return current.GetType();
    }

    /// <summary>
    /// Gets the durable name the unwrapped handler is recorded under by the inbox and the
    /// dead-letter queue.
    /// </summary>
    /// <param name="handler">The (possibly decorated) handler instance.</param>
    /// <returns>The unwrapped handler's full type name.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="handler"/> is <see langword="null"/>.</exception>
    public static string NameOf(object handler)
    {
        Type unwrapped = Unwrap(handler: handler);

        return unwrapped.FullName ?? unwrapped.Name;
    }
}
