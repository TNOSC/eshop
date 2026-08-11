// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Hybrid;
using Tnosc.Lib.Application.Attributes;
using Tnosc.Lib.Application.Queries;
using Tnosc.Lib.Domain.Results;

namespace Tnosc.Lib.Application.Decorators;

/// <summary>
/// Provides caching decorators for query handlers.
/// </summary>
public static class CacheableDecorator
{
    private static readonly ConcurrentDictionary<Type, CacheableAttribute?> CacheableCache = new();
    private static readonly ConcurrentDictionary<Type, PropertyInfo[]> CacheKeyPropertiesCache = new();
    private static readonly ConcurrentDictionary<Type, string[]> TagsCache = new();

    /// <summary>
    /// Caching decorator for query handlers, backed by <see cref="HybridCache"/>.
    /// </summary>
    /// <typeparam name="TQuery">The query type.</typeparam>
    /// <typeparam name="TResponse">The response type.</typeparam>
    /// <param name="innerHandler">The inner query handler.</param>
    /// <param name="cache">The hybrid cache used to store query results.</param>
    public sealed class QueryHandler<TQuery, TResponse>(
        IQueryHandler<TQuery, TResponse> innerHandler,
        HybridCache cache)
        : IQueryHandler<TQuery, TResponse>, IHandlerDecorator
        where TQuery : IQuery<TResponse>
    {
        /// <inheritdoc />
        public object InnerHandler => innerHandler;

        /// <summary>
        /// Handles the query, caching its result when the inner handler is marked <see cref="CacheableAttribute"/>.
        /// </summary>
        /// <param name="query">The query to handle.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        public async ValueTask<Result<TResponse>> HandleAsync(TQuery query, CancellationToken cancellationToken = default)
        {
            CacheableAttribute? attribute = CacheableCache.GetOrAdd(
                key: GetType(),
                valueFactory: _ => HandlerMetadata.Find<CacheableAttribute>(handler: this, messageType: typeof(TQuery)));

            if (attribute is null)
            {
                return await innerHandler.HandleAsync(query: query, cancellationToken: cancellationToken);
            }

            string cacheKey = BuildCacheKey(query: query);

            string[] tags = TagsCache.GetOrAdd(
                key: GetType(),
                valueFactory: _ => [.. HandlerMetadata.FindAll<CacheTagAttribute>(handler: this, messageType: typeof(TQuery)).Select(selector: a => a.Tag)]);

            HybridCacheEntryOptions options = new()
            {
                Expiration = TimeSpan.FromSeconds(seconds: attribute.DurationSeconds),
                LocalCacheExpiration = TimeSpan.FromSeconds(seconds: attribute.DurationSeconds),
            };

            try
            {
                TResponse value = await cache.GetOrCreateAsync(
                    key: cacheKey,
                    factory: async ct =>
                    {
                        Result<TResponse> result = await innerHandler.HandleAsync(query: query, cancellationToken: ct);

                        if (result.IsError)
                        {
                            throw new QueryResultErrorException(errors: [.. result.Errors]);
                        }

                        return result.Value;
                    },
                    options: options,
                    tags: tags,
                    cancellationToken: cancellationToken);

                return value;
            }
            catch (QueryResultErrorException ex)
            {
                return ex.Errors;
            }
        }

        private static string BuildCacheKey(TQuery query)
        {
            PropertyInfo[] properties = CacheKeyPropertiesCache.GetOrAdd(
                key: typeof(TQuery),
                valueFactory: static t => [.. t.GetProperties().Where(predicate: p => p.IsDefined(attributeType: typeof(CacheKeyAttribute)))]);

            if (properties.Length == 0)
            {
                return typeof(TQuery).FullName!;
            }

            return $"{typeof(TQuery).FullName}:{string.Join(separator: '|', values: properties.Select(selector: p => $"{p.Name}={p.GetValue(obj: query)}"))}";
        }
    }

    /// <summary>
    /// Signals that the inner query handler returned an error result, carrying the errors
    /// so they can be reconstructed into a <see cref="Result{TValue}"/> without caching them.
    /// Intentionally internal and without the standard exception constructors: it is only ever
    /// thrown and caught within <see cref="CacheableDecorator"/> and must never cross assembly boundaries.
    /// </summary>
    /// <param name="errors">The errors returned by the inner query handler.</param>
    [SuppressMessage("Design", "CA1064:Exceptions should be public", Justification = "Internal control-flow signal, never thrown across assembly boundaries.")]
    [SuppressMessage("Roslynator", "RCS1194:Implement exception constructors", Justification = "Internal control-flow signal, never thrown across assembly boundaries.")]
    [SuppressMessage("Design", "S3871:Exception types should be \"public\"", Justification = "Internal control-flow signal, never thrown across assembly boundaries.")]
    internal sealed class QueryResultErrorException(Error[] errors) : Exception
    {
        /// <summary>
        /// Gets the errors returned by the inner query handler.
        /// </summary>
        public Error[] Errors { get; } = errors;
    }
}
