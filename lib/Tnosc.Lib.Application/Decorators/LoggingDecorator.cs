// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
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
        : ICommandHandler<TCommand, TResponse>, IHandlerDecorator
        where TCommand : ICommand<TResponse>
    {
        /// <inheritdoc />
        public object InnerHandler => innerHandler;

        /// <summary>
        /// Handles the specified command and logs progress and errors.
        /// </summary>
        /// <param name="command">The command to handle.</param>
        /// <param name="cancellationToken">A cancellation token.</param>
        /// <returns>The result of the command handling.</returns>
        public async ValueTask<Result<TResponse>> HandleAsync(TCommand command, CancellationToken cancellationToken = default)
        {
            string commandName = typeof(TCommand).Name;

            logger.LogInformation(message: "Processing command {Command}", args: commandName);

            Result<TResponse> result = await innerHandler.HandleAsync(command: command, cancellationToken: cancellationToken);

            if (result.IsSuccess)
            {
                logger.LogInformation(message: "Completed command {Command}", args: commandName);
            }
            else
            {
                logger.LogError(message: "Completed command {Command} with errors {@Errors}", commandName, result.Errors);
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
        : ICommandHandler<TCommand>, IHandlerDecorator
        where TCommand : ICommand
    {
        /// <inheritdoc />
        public object InnerHandler => innerHandler;

        /// <summary>
        /// Handles the specified command and logs progress and errors.
        /// </summary>
        /// <param name="command">The command to handle.</param>
        /// <param name="cancellationToken">A cancellation token.</param>
        /// <returns>The result of the command handling.</returns>
        public async ValueTask<Result> HandleAsync(TCommand command, CancellationToken cancellationToken = default)
        {
            string commandName = typeof(TCommand).Name;

            logger.LogInformation(message: "Processing command {Command}", args: commandName);

            Result result = await innerHandler.HandleAsync(command: command, cancellationToken: cancellationToken);

            if (result.IsSuccess)
            {
                logger.LogInformation(message: "Completed command {Command}", args: commandName);
            }
            else
            {
                logger.LogError(message: "Completed command {Command} with errors {@Errors}", commandName, result.Errors);
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
        : IQueryHandler<TQuery, TResponse>, IHandlerDecorator
        where TQuery : IQuery<TResponse>
    {
        /// <inheritdoc />
        public object InnerHandler => innerHandler;

        /// <summary>
        /// Handles the specified query and logs progress and errors.
        /// </summary>
        /// <param name="query">The query to handle.</param>
        /// <param name="cancellationToken">A cancellation token.</param>
        /// <returns>The result of the query handling.</returns>
        public async ValueTask<Result<TResponse>> HandleAsync(TQuery query, CancellationToken cancellationToken = default)
        {
            string queryName = typeof(TQuery).Name;

            logger.LogInformation(message: "Processing query {Query}", args: queryName);

            Result<TResponse> result = await innerHandler.HandleAsync(query: query, cancellationToken: cancellationToken);

            if (result.IsSuccess)
            {
                logger.LogInformation(message: "Completed query {Query}", args: queryName);
            }
            else
            {
                logger.LogError(message: "Completed query {Query} with errors {@Errors}", queryName, result.Errors);
            }

            return result;
        }
    }
}
