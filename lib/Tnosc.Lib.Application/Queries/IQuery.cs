// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC). All rights reserved.
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

namespace Tnosc.Lib.Application.Queries;

/// <summary>
/// Represents a query that produces a response of type <typeparamref name="TResponse"/>.
/// Implementations encapsulate request data for query handlers.
/// </summary>
/// <typeparam name="TResponse">The type returned by the query.</typeparam>
public interface IQuery<TResponse>;
