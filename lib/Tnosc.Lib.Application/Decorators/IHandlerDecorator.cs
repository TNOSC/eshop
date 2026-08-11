// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

namespace Tnosc.Lib.Application.Decorators;

/// <summary>
/// Implemented by every handler decorator so <see cref="HandlerMetadata"/> can unwrap a
/// chain of nested decorators down to the real handler instance, regardless of how deep
/// the decorator is nested.
/// </summary>
public interface IHandlerDecorator
{
    /// <summary>
    /// Gets the handler or decorator wrapped by this decorator.
    /// </summary>
    object InnerHandler { get; }
}
