// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC). All rights reserved.
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Tnosc.Lib.Application.Commands;
using Tnosc.Lib.Application.Queries;
using Tnosc.Lib.Domain.Results;

namespace Tnosc.Lib.Application.Decorators;

/// <summary>
/// Provides logging decorators for command and query handlers.
/// </summary>
public static class LoggingDecorator
{
    /// <summary>
    /// Logging decorator for command handlers that return a response.
    /// Logs processing start, successful completion and errors.
    /// </summary>
    public sealed class CommandHandler<TCommand, TResponse>(
        ICommandHandler<TCommand, TResponse> innerHandler,
        ILogger<CommandHandler<TCommand, TResponse>> logger)
        : ICommandHandler<TCommand, TResponse>
        where TCommand : ICommand<TResponse>
    {
        /// <summary>
        /// Handles the specified command and logs progress and errors.
        /// </summary>
        /// <param name="command">The command to handle.</param>
        /// <param name="cancellationToken">A cancellation token.</param>
        /// <returns>The result of the command handling.</returns>
        public async ValueTask<Result<TResponse>> HandleAsync(TCommand command, CancellationToken cancellationToken = default)
        {
            string commandName = typeof(TCommand).Name;

            logger.LogInformation("Processing command {Command}", commandName);

            Result<TResponse> result = await innerHandler.HandleAsync(command, cancellationToken);

            if (result.IsSuccess)
            {
                logger.LogInformation("Completed command {Command}", commandName);
            }
            else
            {
                logger.LogError("Completed command {Command} with errors {@Errors}", commandName, result.Errors);
            }

            return result;
        }
    }

    /// <summary>
    /// Logging decorator for command handlers that do not return a response.
    /// Logs processing start, successful completion and errors.
    /// </summary>
    public sealed class CommandBaseHandler<TCommand>(
        ICommandHandler<TCommand> innerHandler,
        ILogger<CommandBaseHandler<TCommand>> logger)
        : ICommandHandler<TCommand>
        where TCommand : ICommand
    {
        /// <summary>
        /// Handles the specified command and logs progress and errors.
        /// </summary>
        /// <param name="command">The command to handle.</param>
        /// <param name="cancellationToken">A cancellation token.</param>
        /// <returns>The result of the command handling.</returns>
        public async ValueTask<Result> HandleAsync(TCommand command, CancellationToken cancellationToken = default)
        {
            string commandName = typeof(TCommand).Name;

            logger.LogInformation("Processing command {Command}", commandName);

            Result result = await innerHandler.HandleAsync(command, cancellationToken);

            if (result.IsSuccess)
            {
                logger.LogInformation("Completed command {Command}", commandName);
            }
            else
            {
                logger.LogError("Completed command {Command} with errors {@Errors}", commandName, result.Errors);
            }

            return result;
        }
    }

    /// <summary>
    /// Logging decorator for query handlers.
    /// Logs processing start, successful completion and errors.
    /// </summary>
    public sealed class QueryHandler<TQuery, TResponse>(
        IQueryHandler<TQuery, TResponse> innerHandler,
        ILogger<QueryHandler<TQuery, TResponse>> logger)
        : IQueryHandler<TQuery, TResponse>
        where TQuery : IQuery<TResponse>
    {
        /// <summary>
        /// Handles the specified query and logs progress and errors.
        /// </summary>
        /// <param name="query">The query to handle.</param>
        /// <param name="cancellationToken">A cancellation token.</param>
        /// <returns>The result of the query handling.</returns>
        public async ValueTask<Result<TResponse>> HandleAsync(TQuery query, CancellationToken cancellationToken = default)
        {
            string queryName = typeof(TQuery).Name;

            logger.LogInformation("Processing query {Query}", queryName);

            Result<TResponse> result = await innerHandler.HandleAsync(query, cancellationToken);

            if (result.IsSuccess)
            {
                logger.LogInformation("Completed query {Query}", queryName);
            }
            else
            {
                logger.LogError("Completed query {Query} with errors {@Errors}", queryName, result.Errors);
            }

            return result;
        }
    }
}
