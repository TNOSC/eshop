// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC). All rights reserved.
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using System.Collections.Concurrent;
using System.Reflection;

namespace Tnosc.Lib.Application.Decorators;

/// <summary>
/// Resolves attributes declared on a handler that may itself be wrapped by one or more
/// nested <see cref="IHandlerDecorator"/> instances. Attribute lookup always unwraps the
/// chain down to the real handler type first, so an attribute keeps working no matter how
/// deep the decorator pipeline nests the handler it governs.
/// </summary>
internal static class HandlerMetadata
{
    private static readonly ConcurrentDictionary<Type, Type> UnwrappedTypeCache = new();

    /// <summary>
    /// Finds the first <typeparamref name="TAttribute"/> declared on the unwrapped handler
    /// type, falling back to <paramref name="messageType"/> when none is found.
    /// </summary>
    /// <typeparam name="TAttribute">The attribute type to look for.</typeparam>
    /// <param name="handler">The (possibly decorated) handler instance.</param>
    /// <param name="messageType">The command or query type the handler processes.</param>
    /// <returns>The resolved attribute, or <see langword="null"/> if none is declared.</returns>
    public static TAttribute? Find<TAttribute>(object handler, Type messageType)
        where TAttribute : Attribute =>
        Unwrap(handler: handler).GetCustomAttribute<TAttribute>() ?? messageType.GetCustomAttribute<TAttribute>();

    /// <summary>
    /// Finds every <typeparamref name="TAttribute"/> declared on the unwrapped handler type,
    /// falling back to <paramref name="messageType"/> when none is found.
    /// </summary>
    /// <typeparam name="TAttribute">The attribute type to look for.</typeparam>
    /// <param name="handler">The (possibly decorated) handler instance.</param>
    /// <param name="messageType">The command or query type the handler processes.</param>
    /// <returns>The resolved attributes; empty when none is declared.</returns>
    public static TAttribute[] FindAll<TAttribute>(object handler, Type messageType)
        where TAttribute : Attribute
    {
        TAttribute[] handlerAttributes = [.. Unwrap(handler: handler).GetCustomAttributes<TAttribute>()];

        return handlerAttributes.Length > 0
            ? handlerAttributes
            : [.. messageType.GetCustomAttributes<TAttribute>()];
    }

    /// <summary>
    /// Unwraps a chain of <see cref="IHandlerDecorator"/> instances down to the concrete
    /// handler type, caching the result per outermost decorator type.
    /// </summary>
    /// <param name="handler">The (possibly decorated) handler instance.</param>
    /// <returns>The concrete, unwrapped handler type.</returns>
    public static Type Unwrap(object handler) =>
        UnwrappedTypeCache.GetOrAdd(key: handler.GetType(), valueFactory: _ =>
        {
            object current = handler;

            while (current is IHandlerDecorator decorator)
            {
                current = decorator.InnerHandler;
            }

            return current.GetType();
        });
}
