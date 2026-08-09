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
using Tnosc.Lib.Application.Abstractions.Persistence;
using Tnosc.Lib.Application.Attributes;
using Tnosc.Lib.Application.Commands;
using Tnosc.Lib.Domain.Results;

namespace Tnosc.Lib.Application.Decorators;

/// <summary>
/// Provides transaction decorators for command handlers.
/// </summary>
public static class TransactionDecorator
{
    private static readonly ConcurrentDictionary<Type, bool> TransactionalCache = new();

    /// <summary>
    /// Determines whether the specified handler's concrete type is marked <see cref="TransactionalAttribute"/>,
    /// caching the reflection result per handler type to avoid repeated attribute lookups.
    /// </summary>
    /// <param name="handler">The inner command handler instance.</param>
    private static bool IsTransactional(object handler) =>
        TransactionalCache.GetOrAdd(handler.GetType(), static t => t.GetCustomAttribute<TransactionalAttribute>() is not null);

    /// <summary>
    /// Transaction decorator for command handlers that return a response.
    /// </summary>
    /// <typeparam name="TCommand">The command type.</typeparam>
    /// <typeparam name="TResponse">The response type.</typeparam>
    /// <param name="innerHandler">The inner command handler.</param>
    /// <param name="unitOfWork">The unit of work used to manage the transaction.</param>
    public sealed class CommandHandler<TCommand, TResponse>(
        ICommandHandler<TCommand, TResponse> innerHandler,
        IUnitOfWork unitOfWork)
        : ICommandHandler<TCommand, TResponse>
        where TCommand : ICommand<TResponse>
    {
        /// <summary>
        /// Handles the command within a transaction when the inner handler is marked <see cref="TransactionalAttribute"/>.
        /// Commits the transaction on success and rolls it back on error or exception.
        /// </summary>
        /// <param name="command">The command to handle.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        public async ValueTask<Result<TResponse>> HandleAsync(TCommand command, CancellationToken cancellationToken = default)
        {
            if (!IsTransactional(innerHandler))
            {
                return await innerHandler.HandleAsync(command, cancellationToken);
            }

            await unitOfWork.BeginTransactionAsync(cancellationToken);

            try
            {
                Result<TResponse> result = await innerHandler.HandleAsync(command, cancellationToken);

                if (result.IsSuccess)
                {
                    await unitOfWork.CommitTransactionAsync(cancellationToken);
                }
                else
                {
                    await unitOfWork.RollbackTransactionAsync(cancellationToken);
                }

                return result;
            }
            catch
            {
                await unitOfWork.RollbackTransactionAsync(cancellationToken);
                throw;
            }
        }
    }

    /// <summary>
    /// Transaction decorator for command handlers that do not return a response.
    /// </summary>
    /// <typeparam name="TCommand">The command type.</typeparam>
    /// <param name="innerHandler">The inner command handler.</param>
    /// <param name="unitOfWork">The unit of work used to manage the transaction.</param>
    public sealed class CommandBaseHandler<TCommand>(
        ICommandHandler<TCommand> innerHandler,
        IUnitOfWork unitOfWork)
        : ICommandHandler<TCommand>
        where TCommand : ICommand
    {
        /// <summary>
        /// Handles the command within a transaction when the inner handler is marked <see cref="TransactionalAttribute"/>.
        /// Commits the transaction on success and rolls it back on error or exception.
        /// </summary>
        /// <param name="command">The command to handle.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        public async ValueTask<Result> HandleAsync(TCommand command, CancellationToken cancellationToken = default)
        {
            if (!IsTransactional(innerHandler))
            {
                return await innerHandler.HandleAsync(command, cancellationToken);
            }

            await unitOfWork.BeginTransactionAsync(cancellationToken);

            try
            {
                Result result = await innerHandler.HandleAsync(command, cancellationToken);

                if (result.IsSuccess)
                {
                    await unitOfWork.CommitTransactionAsync(cancellationToken);
                }
                else
                {
                    await unitOfWork.RollbackTransactionAsync(cancellationToken);
                }

                return result;
            }
            catch
            {
                await unitOfWork.RollbackTransactionAsync(cancellationToken);
                throw;
            }
        }
    }
}
