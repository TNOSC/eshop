// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC). All rights reserved.
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;
using Tnosc.Lib.Domain.Results;

namespace Tnosc.Lib.Application.Commands;

/// <summary>
/// Handles a command of type <typeparamref name="TCommand"/> and returns a <see cref="Result"/>.
/// </summary>
/// <typeparam name="TCommand">The command type to handle.</typeparam>
public interface ICommandHandler<in TCommand>
    where TCommand : ICommand
{
    /// <summary>
    /// Handles the specified command.
    /// </summary>
    /// <param name="command">The command to handle.</param>
    /// <param name="cancellationToken">A token to observe while waiting for the task to complete.</param>
    /// <returns>A <see cref="ValueTask{Result}"/> that represents the outcome of the operation.</returns>
    ValueTask<Result> HandleAsync(TCommand command, CancellationToken cancellationToken = default);
}

/// <summary>
/// Handles a command of type <typeparamref name="TCommand"/> and returns a response of type <typeparamref name="TResponse"/> wrapped in a <see cref="Result{TResponse}"/>.
/// </summary>
/// <typeparam name="TCommand">The command type to handle.</typeparam>
/// <typeparam name="TResponse">The response type returned by the command.</typeparam>
public interface ICommandHandler<in TCommand, TResponse>
    where TCommand : ICommand<TResponse>
{
    /// <summary>
    /// Handles the specified command and returns a response.
    /// </summary>
    /// <param name="command">The command to handle.</param>
    /// <param name="cancellationToken">A token to observe while waiting for the task to complete.</param>
    /// <returns>A <see cref="ValueTask"/> containing the <see cref="Result{TResponse}"/> response.</returns>
    ValueTask<Result<TResponse>> HandleAsync(TCommand command, CancellationToken cancellationToken = default);
}
