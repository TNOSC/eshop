// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC). All rights reserved.
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Scrutor;
using Tnosc.Lib.Application.Commands;
using Tnosc.Lib.Application.Decorators;
using Tnosc.Lib.Application.DomainEvents;
using Tnosc.Lib.Application.Queries;
using Tnosc.Lib.Application.Validations;

namespace Tnosc.Lib.Application.Extensions;

/// <summary>
/// Provides helper methods for scanning types and extracting closed generic interfaces
/// that implement a specific open generic interface.
/// </summary>
public static class ScanExtensions
{
    /// <summary>
    /// Returns the closed generic interface types implemented by <paramref name="implementationType"/>
    /// whose open generic type definition equals <paramref name="openGenericInterface"/>.
    /// </summary>
    /// <param name="implementationType">The concrete type to inspect for implemented interfaces.</param>
    /// <param name="openGenericInterface">The open generic interface type to match (for example typeof(IRepository&lt;&gt;)).</param>
    /// <returns>
    /// An enumerable of closed generic interface <see cref="Type"/> instances implemented by
    /// <paramref name="implementationType"/> that have the specified open generic type definition.
    /// </returns>
    public static IEnumerable<Type> ClosedInterfacesOf(Type implementationType, Type openGenericInterface) =>
        implementationType.GetInterfaces()
            .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == openGenericInterface);
}
