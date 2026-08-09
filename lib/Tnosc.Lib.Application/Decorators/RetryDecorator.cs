// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC). All rights reserved.
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using System.Collections.Concurrent;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Tnosc.Lib.Application.Attributes;
using Tnosc.Lib.Application.Commands;
using Tnosc.Lib.Application.Exceptions;
using Tnosc.Lib.Application.Queries;
using Tnosc.Lib.Domain.Results;

namespace Tnosc.Lib.Application.Decorators;

/// <summary>
/// Provides retry decorators for command and query handlers.
/// </summary>
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
        : ICommandHandler<TCommand, TResponse>
        where TCommand : ICommand<TResponse>
    {
        /// <summary>
        /// Handles the command, retrying with backoff when the inner handler throws a retriable <see cref="BaseException"/>.
        /// </summary>
        /// <param name="command">The command to handle.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        public ValueTask<Result<TResponse>> HandleAsync(TCommand command, CancellationToken cancellationToken = default) =>
            ExecuteWithRetryAsync(
                ct => innerHandler.HandleAsync(command, ct),
                GetMaxAttempts(innerHandler),
                cancellationToken);
    }

    /// <summary>
    /// Retry decorator for command handlers that do not return a response.
    /// </summary>
    /// <typeparam name="TCommand">The command type.</typeparam>
    /// <param name="innerHandler">The inner command handler.</param>
    public sealed class CommandBaseHandler<TCommand>(ICommandHandler<TCommand> innerHandler)
        : ICommandHandler<TCommand>
        where TCommand : ICommand
    {
        /// <summary>
        /// Handles the command, retrying with backoff when the inner handler throws a retriable <see cref="BaseException"/>.
        /// </summary>
        /// <param name="command">The command to handle.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        public ValueTask<Result> HandleAsync(TCommand command, CancellationToken cancellationToken = default) =>
            ExecuteWithRetryAsync(
                ct => innerHandler.HandleAsync(command, ct),
                GetMaxAttempts(innerHandler),
                cancellationToken);
    }

    /// <summary>
    /// Retry decorator for query handlers.
    /// </summary>
    /// <typeparam name="TQuery">The query type.</typeparam>
    /// <typeparam name="TResponse">The response type.</typeparam>
    /// <param name="innerHandler">The inner query handler.</param>
    public sealed class QueryHandler<TQuery, TResponse>(IQueryHandler<TQuery, TResponse> innerHandler)
        : IQueryHandler<TQuery, TResponse>
        where TQuery : IQuery<TResponse>
    {
        /// <summary>
        /// Handles the query, retrying with backoff when the inner handler throws a retriable <see cref="BaseException"/>.
        /// </summary>
        /// <param name="query">The query to handle.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        public ValueTask<Result<TResponse>> HandleAsync(TQuery query, CancellationToken cancellationToken = default) =>
            ExecuteWithRetryAsync(
                ct => innerHandler.HandleAsync(query, ct),
                GetMaxAttempts(innerHandler),
                cancellationToken);
    }

    /// <summary>
    /// Resolves the maximum number of attempts for the specified handler, from its <see cref="RetryAttribute"/>
    /// if present, defaulting to 3 otherwise. Caches the reflection result per handler type.
    /// </summary>
    /// <param name="handler">The inner handler instance.</param>
    private static int GetMaxAttempts(object handler) =>
        RetryCache.GetOrAdd(handler.GetType(), static t => t.GetCustomAttribute<RetryAttribute>())?.MaxRetries ?? 3;

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
                var delay = TimeSpan.FromMilliseconds(200 * Math.Pow(2, attempt - 1));
                await Task.Delay(delay, cancellationToken);
            }
        }
    }
}
