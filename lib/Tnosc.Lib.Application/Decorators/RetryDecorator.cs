// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Tnosc.Lib.Application.Attributes;
using Tnosc.Lib.Application.Commands;
using Tnosc.Lib.Application.DomainEvents;
using Tnosc.Lib.Application.Exceptions;
using Tnosc.Lib.Application.Queries;
using Tnosc.Lib.Domain;
using Tnosc.Lib.Shared.Results;

namespace Tnosc.Lib.Application.Decorators;

/// <summary>
/// Provides retry decorators for command, query and domain event handlers.
/// </summary>
/// <remarks>
/// Command and query handlers retry by default (three attempts) because retrying is already narrowly
/// scoped — only a <see cref="BaseException"/> whose <see cref="BaseException.IsRetriable"/> is set
/// ever qualifies. Domain event handlers are <b>strictly opt-in</b>: without
/// <see cref="RetryAttribute"/> they get a single attempt, because the outbox is already retrying
/// them durably and adding a second, invisible retry layer to every handler is not a default anyone
/// asked for.
/// </remarks>
public static class RetryDecorator
{
    private static readonly ConcurrentDictionary<Type, RetryAttribute?> RetryCache = new();

    /// <summary>
    /// Retry decorator for command handlers that return a response.
    /// </summary>
    /// <typeparam name="TCommand">The command type.</typeparam>
    /// <typeparam name="TResponse">The response type.</typeparam>
    /// <param name="innerHandler">The inner command handler.</param>
    public sealed class CommandHandler<TCommand, TResponse>(ICommandHandler<TCommand, TResponse> innerHandler)
        : ICommandHandler<TCommand, TResponse>, IHandlerDecorator
        where TCommand : ICommand<TResponse>
    {
        /// <inheritdoc />
        public object InnerHandler => innerHandler;

        /// <summary>
        /// Handles the command, retrying with backoff when the inner handler throws a retriable <see cref="BaseException"/>.
        /// </summary>
        /// <param name="command">The command to handle.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        public ValueTask<Result<TResponse>> HandleAsync(TCommand command, CancellationToken cancellationToken = default) =>
            ExecuteWithRetryAsync(
                action: ct => innerHandler.HandleAsync(command: command, cancellationToken: ct),
                maxAttempts: GetMaxAttempts(handler: this, messageType: typeof(TCommand), defaultAttempts: DefaultAttempts),
                cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Retry decorator for command handlers that do not return a response.
    /// </summary>
    /// <typeparam name="TCommand">The command type.</typeparam>
    /// <param name="innerHandler">The inner command handler.</param>
    public sealed class CommandBaseHandler<TCommand>(ICommandHandler<TCommand> innerHandler)
        : ICommandHandler<TCommand>, IHandlerDecorator
        where TCommand : ICommand
    {
        /// <inheritdoc />
        public object InnerHandler => innerHandler;

        /// <summary>
        /// Handles the command, retrying with backoff when the inner handler throws a retriable <see cref="BaseException"/>.
        /// </summary>
        /// <param name="command">The command to handle.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        public ValueTask<Result> HandleAsync(TCommand command, CancellationToken cancellationToken = default) =>
            ExecuteWithRetryAsync(
                action: ct => innerHandler.HandleAsync(command: command, cancellationToken: ct),
                maxAttempts: GetMaxAttempts(handler: this, messageType: typeof(TCommand), defaultAttempts: DefaultAttempts),
                cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Retry decorator for query handlers.
    /// </summary>
    /// <typeparam name="TQuery">The query type.</typeparam>
    /// <typeparam name="TResponse">The response type.</typeparam>
    /// <param name="innerHandler">The inner query handler.</param>
    public sealed class QueryHandler<TQuery, TResponse>(IQueryHandler<TQuery, TResponse> innerHandler)
        : IQueryHandler<TQuery, TResponse>, IHandlerDecorator
        where TQuery : IQuery<TResponse>
    {
        /// <inheritdoc />
        public object InnerHandler => innerHandler;

        /// <summary>
        /// Handles the query, retrying with backoff when the inner handler throws a retriable <see cref="BaseException"/>.
        /// </summary>
        /// <param name="query">The query to handle.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        public ValueTask<Result<TResponse>> HandleAsync(TQuery query, CancellationToken cancellationToken = default) =>
            ExecuteWithRetryAsync(
                action: ct => innerHandler.HandleAsync(query: query, cancellationToken: ct),
                maxAttempts: GetMaxAttempts(handler: this, messageType: typeof(TQuery), defaultAttempts: DefaultAttempts),
                cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Retry decorator for domain event handlers.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The <b>fast</b> retry in front of the outbox's slow one. A blip that would clear in
    /// milliseconds is absorbed here; if the attempts are exhausted the exception still propagates to
    /// <c>OutboxProcessor</c>, which fails the message and retries it durably with its own backoff.
    /// The two layers compose — this one never replaces the outbox as the durable guarantee.
    /// </para>
    /// <para>
    /// Registered <b>outside</b> <see cref="IdempotencyDecorator"/>, which is not a preference: the
    /// idempotency decorator opens a transaction per invocation, and a retry inside it would run on
    /// a transaction Postgres has already aborted, where every later statement fails with
    /// <c>25P02</c>. From outside, a failed attempt rolls the inbox claim back together with the
    /// handler's partial work, so the next attempt starts from a clean slate.
    /// </para>
    /// <para>
    /// Keep the attempt count low. Total in-process delay is <c>200ms · (2^(n-1) − 1)</c>, and a
    /// handler that retries for longer than the outbox's claim lease outlives its own claim, letting
    /// another processor pick the same message up while this one is still working.
    /// </para>
    /// </remarks>
    /// <typeparam name="TEvent">The domain event type.</typeparam>
    /// <param name="innerHandler">The inner domain event handler.</param>
    public sealed class DomainEventHandler<TEvent>(IDomainEventHandler<TEvent> innerHandler)
        : IDomainEventHandler<TEvent>, IHandlerDecorator
        where TEvent : IDomainEvent
    {
        /// <inheritdoc />
        public object InnerHandler => innerHandler;

        /// <summary>
        /// Handles the domain event, retrying with backoff when the inner handler throws a retriable
        /// <see cref="BaseException"/> and is marked <see cref="RetryAttribute"/>. Without that
        /// attribute the handler gets a single attempt and the exception propagates to the outbox.
        /// </summary>
        /// <param name="event">The domain event to handle.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        public ValueTask HandleAsync(TEvent @event, CancellationToken cancellationToken = default) =>
            RunWithRetryAsync(
                action: ct => innerHandler.HandleAsync(@event: @event, cancellationToken: ct),
                maxAttempts: GetMaxAttempts(handler: this, messageType: typeof(TEvent), defaultAttempts: NoRetryAttempts),
                cancellationToken: cancellationToken);

        /// <summary>
        /// Executes an <paramref name="action"/> that produces no result, retrying on the same terms
        /// as its generic counterpart.
        /// </summary>
        /// <remarks>
        /// Delegates rather than repeating the loop, so the retry condition and backoff schedule stay
        /// defined in exactly one place. A domain event handler returns a bare
        /// <see cref="ValueTask"/>, which the generic overload cannot express.
        /// </remarks>
        /// <param name="action">The operation to execute.</param>
        /// <param name="maxAttempts">The maximum number of attempts.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        private static async ValueTask RunWithRetryAsync(
            Func<CancellationToken, ValueTask> action,
            int maxAttempts,
            CancellationToken cancellationToken) =>
            await ExecuteWithRetryAsync<bool>(
                action: async token =>
                {
                    await action(token);

                    return true;
                },
                maxAttempts: maxAttempts,
                cancellationToken: cancellationToken);
    }

    /// <summary>
    /// The number of attempts a command or query handler gets when it declares no <see cref="RetryAttribute"/>.
    /// </summary>
    private const int DefaultAttempts = 3;

    /// <summary>
    /// The number of attempts a domain event handler gets when it declares no <see cref="RetryAttribute"/>
    /// — one, meaning no retry at all.
    /// </summary>
    private const int NoRetryAttempts = 1;

    /// <summary>
    /// Resolves the maximum number of attempts for the specified handler, from its <see cref="RetryAttribute"/>
    /// if present, falling back to <paramref name="defaultAttempts"/> otherwise. Caches the reflection
    /// result per outermost decorator type.
    /// </summary>
    /// <param name="handler">The decorator instance wrapping the handler.</param>
    /// <param name="messageType">The command, query or event type the handler processes.</param>
    /// <param name="defaultAttempts">The attempt count to use when the handler declares no attribute.</param>
    /// <remarks>
    /// The default is applied <b>after</b> the cache lookup, so one cache serves pipelines whose
    /// unmarked defaults differ. Keyed on the <b>unwrapped handler</b> type, not the decorator's:
    /// several domain event handlers subscribe to one event and therefore share a closed decorator
    /// type, so keying on that would give every sibling the first handler's <c>[Retry]</c>.
    /// </remarks>
    private static int GetMaxAttempts(object handler, Type messageType, int defaultAttempts) =>
        RetryCache.GetOrAdd(key: HandlerChain.Unwrap(handler: handler), valueFactory: _ => HandlerMetadata.Find<RetryAttribute>(handler: handler, messageType: messageType))?.MaxRetries ?? defaultAttempts;

    /// <summary>
    /// Executes <paramref name="action"/>, retrying with exponential backoff when it throws a
    /// <see cref="BaseException"/> whose <see cref="BaseException.IsRetriable"/> is <see langword="true"/>.
    /// </summary>
    /// <typeparam name="TResult">The result type.</typeparam>
    /// <param name="action">The operation to execute.</param>
    /// <param name="maxAttempts">The maximum number of attempts.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    private static async ValueTask<TResult> ExecuteWithRetryAsync<TResult>(
        Func<CancellationToken, ValueTask<TResult>> action,
        int maxAttempts,
        CancellationToken cancellationToken)
    {
        int attempt = 0;

        while (true)
        {
            attempt++;

            try
            {
                return await action(cancellationToken);
            }
            catch (BaseException ex) when (ex.IsRetriable && attempt < maxAttempts)
            {
                var delay = TimeSpan.FromMilliseconds(value: 200 * Math.Pow(x: 2, y: attempt - 1));
                await Task.Delay(delay: delay, cancellationToken: cancellationToken);
            }
        }
    }
}
