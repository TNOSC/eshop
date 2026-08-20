// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;
using Tnosc.Lib.Shared.Results;

namespace Tnosc.Lib.Application.Queries;

/// <summary>
/// Defines a handler that processes a query and returns a <see cref="Result{TResponse}"/>.
/// </summary>
/// <typeparam name="TQuery">The query type that implements <see cref="IQuery{TResponse}"/>.</typeparam>
/// <typeparam name="TResponse">The response type returned by the query.</typeparam>
public interface IQueryHandler<in TQuery, TResponse>
    where TQuery : IQuery<TResponse>
{
    /// <summary>
    /// Handles the specified query and returns a result containing the response or an error.
    /// </summary>
    /// <param name="query">The query to handle.</param>
    /// <param name="cancellationToken">A token to observe while waiting for the task to complete.</param>
    /// <returns>A <see cref="ValueTask{TResult}"/> representing the asynchronous operation that returns a <see cref="Result{TResponse}"/>.</returns>
    ValueTask<Result<TResponse>> HandleAsync(TQuery query, CancellationToken cancellationToken = default);
}
