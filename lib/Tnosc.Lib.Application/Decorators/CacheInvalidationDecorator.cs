// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC). All rights reserved.
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Hybrid;
using Tnosc.Lib.Application.Attributes;
using Tnosc.Lib.Application.Commands;
using Tnosc.Lib.Domain.Results;

namespace Tnosc.Lib.Application.Decorators;

/// <summary>
/// Provides cache invalidation decorators for command handlers.
/// </summary>
public static class CacheInvalidationDecorator
{
    private static readonly ConcurrentDictionary<Type, string[]> TagsCache = new();

    /// <summary>
    /// Cache invalidation decorator for command handlers that return a response.
    /// </summary>
    /// <typeparam name="TCommand">The command type.</typeparam>
    /// <typeparam name="TResponse">The response type.</typeparam>
    /// <param name="innerHandler">The inner command handler.</param>
    /// <param name="cache">The hybrid cache to invalidate entries in.</param>
    public sealed class CommandHandler<TCommand, TResponse>(
        ICommandHandler<TCommand, TResponse> innerHandler,
        HybridCache cache)
        : ICommandHandler<TCommand, TResponse>, IHandlerDecorator
        where TCommand : ICommand<TResponse>
    {
        /// <inheritdoc />
        public object InnerHandler => innerHandler;

        /// <summary>
        /// Handles the command and, on success, removes every cache entry tagged with the
        /// inner handler's <see cref="CacheTagAttribute"/> values.
        /// </summary>
        /// <param name="command">The command to handle.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        public async ValueTask<Result<TResponse>> HandleAsync(TCommand command, CancellationToken cancellationToken = default)
        {
            string[] tags = GetTags(this, typeof(TCommand));

            if (tags.Length == 0)
            {
                return await innerHandler.HandleAsync(command, cancellationToken);
            }

            Result<TResponse> result = await innerHandler.HandleAsync(command, cancellationToken);

            if (result.IsSuccess)
            {
                await InvalidateAsync(cache, tags, cancellationToken);
            }

            return result;
        }
    }

    /// <summary>
    /// Cache invalidation decorator for command handlers that do not return a response.
    /// </summary>
    /// <typeparam name="TCommand">The command type.</typeparam>
    /// <param name="innerHandler">The inner command handler.</param>
    /// <param name="cache">The hybrid cache to invalidate entries in.</param>
    public sealed class CommandBaseHandler<TCommand>(
        ICommandHandler<TCommand> innerHandler,
        HybridCache cache)
        : ICommandHandler<TCommand>, IHandlerDecorator
        where TCommand : ICommand
    {
        /// <inheritdoc />
        public object InnerHandler => innerHandler;

        /// <summary>
        /// Handles the command and, on success, removes every cache entry tagged with the
        /// inner handler's <see cref="CacheTagAttribute"/> values.
        /// </summary>
        /// <param name="command">The command to handle.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        public async ValueTask<Result> HandleAsync(TCommand command, CancellationToken cancellationToken = default)
        {
            string[] tags = GetTags(this, typeof(TCommand));

            if (tags.Length == 0)
            {
                return await innerHandler.HandleAsync(command, cancellationToken);
            }

            Result result = await innerHandler.HandleAsync(command, cancellationToken);

            if (result.IsSuccess)
            {
                await InvalidateAsync(cache, tags, cancellationToken);
            }

            return result;
        }
    }

    /// <summary>
    /// Resolves the <see cref="CacheTagAttribute"/> values declared on the unwrapped handler type,
    /// caching the result per outermost decorator type to avoid repeated attribute lookups.
    /// </summary>
    /// <param name="handler">The decorator instance wrapping the handler.</param>
    /// <param name="messageType">The command type the handler processes.</param>
    private static string[] GetTags(object handler, Type messageType) =>
        TagsCache.GetOrAdd(handler.GetType(), _ =>
            [.. HandlerMetadata.FindAll<CacheTagAttribute>(handler, messageType).Select(a => a.Tag)]);

    /// <summary>
    /// Removes every cache entry associated with the specified tags.
    /// </summary>
    /// <param name="cache">The hybrid cache to invalidate entries in.</param>
    /// <param name="tags">The tags to invalidate.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    private static async ValueTask InvalidateAsync(HybridCache cache, string[] tags, CancellationToken cancellationToken)
    {
        foreach (string tag in tags)
        {
            await cache.RemoveByTagAsync(tag, cancellationToken);
        }
    }
}
