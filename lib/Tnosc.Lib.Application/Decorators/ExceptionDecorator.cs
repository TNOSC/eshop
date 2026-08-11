// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Tnosc.Lib.Application.Commands;
using Tnosc.Lib.Application.Exceptions;
using Tnosc.Lib.Application.Queries;
using Tnosc.Lib.Domain.Results;

namespace Tnosc.Lib.Application.Decorators;

/// <summary>
/// Provides exception decorators for command and query handlers that catch any exception
/// thrown by the inner handler and map it to a failure <see cref="Result"/>, so the
/// application layer always returns a <see cref="Result"/> to its callers instead of
/// letting exceptions propagate.
/// </summary>
/// <remarks>
/// When composing decorators, <see cref="ExceptionDecorator"/> should be the outermost
/// decorator (closest to the caller), wrapping <c>TransactionDecorator</c> and
/// <c>RetryDecorator</c>. Those decorators rely on observing the raw exception
/// (to roll back a transaction or decide whether to retry), so exceptions must still
/// reach them unconverted; this decorator is the last line of defense that guarantees
/// nothing escapes past the application boundary.
/// </remarks>
public static class ExceptionDecorator
{
    /// <summary>
    /// Exception decorator for command handlers that return a response.
    /// Catches any exception thrown by the inner handler and maps it to a failure result.
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
        /// Handles the specified command, converting any thrown exception into a failure result.
        /// </summary>
        /// <param name="command">The command to handle.</param>
        /// <param name="cancellationToken">A cancellation token.</param>
        /// <returns>The result of the command handling, or a mapped failure result if an exception was thrown.</returns>
        public async ValueTask<Result<TResponse>> HandleAsync(TCommand command, CancellationToken cancellationToken = default)
        {
            try
            {
                return await innerHandler.HandleAsync(command: command, cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogError(exception: ex, message: "Unhandled exception processing command {Command}", args: typeof(TCommand).Name);
                return MapToErrors(ex: ex);
            }
        }
    }

    /// <summary>
    /// Exception decorator for command handlers that do not return a response.
    /// Catches any exception thrown by the inner handler and maps it to a failure result.
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
        /// Handles the specified command, converting any thrown exception into a failure result.
        /// </summary>
        /// <param name="command">The command to handle.</param>
        /// <param name="cancellationToken">A cancellation token.</param>
        /// <returns>The result of the command handling, or a mapped failure result if an exception was thrown.</returns>
        public async ValueTask<Result> HandleAsync(TCommand command, CancellationToken cancellationToken = default)
        {
            try
            {
                return await innerHandler.HandleAsync(command: command, cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogError(exception: ex, message: "Unhandled exception processing command {Command}", args: typeof(TCommand).Name);
                return MapToErrors(ex: ex);
            }
        }
    }

    /// <summary>
    /// Exception decorator for query handlers.
    /// Catches any exception thrown by the inner handler and maps it to a failure result.
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
        /// Handles the specified query, converting any thrown exception into a failure result.
        /// </summary>
        /// <param name="query">The query to handle.</param>
        /// <param name="cancellationToken">A cancellation token.</param>
        /// <returns>The result of the query handling, or a mapped failure result if an exception was thrown.</returns>
        public async ValueTask<Result<TResponse>> HandleAsync(TQuery query, CancellationToken cancellationToken = default)
        {
            try
            {
                return await innerHandler.HandleAsync(query: query, cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogError(exception: ex, message: "Unhandled exception processing query {Query}", args: typeof(TQuery).Name);
                return MapToErrors(ex: ex);
            }
        }
    }

    /// <summary>
    /// Maps an exception to one or more <see cref="Error"/> instances, preferring the structured
    /// metadata carried by <see cref="BaseException"/> subtypes and falling back to
    /// <see cref="Error.Unexpected(string, string)"/> for anything else.
    /// </summary>
    /// <param name="ex">The exception to map.</param>
    /// <returns>An array of errors describing the exception.</returns>
    private static Error[] MapToErrors(Exception ex) =>
        ex switch
        {
            NotFoundException notFound => [Error.NotFound(code: notFound.ErrorCode, description: notFound.Message)],
            ConflictException conflict => [Error.Conflict(code: conflict.ErrorCode, description: conflict.Message)],
            UnauthorizedException unauthorized => [Error.Unauthorized(code: unauthorized.ErrorCode, description: unauthorized.Message)],
            InvalidRequestException invalidRequest => MapValidationErrors(ex: invalidRequest),
            TransientFailureException transientFailure => [Error.Unexpected(code: transientFailure.ErrorCode, description: transientFailure.Message)],
            BaseException baseException => [Error.Unexpected(
                code: string.IsNullOrEmpty(value: baseException.ErrorCode) ? "UNEXPECTED" : baseException.ErrorCode,
                description: baseException.Message ?? "An unexpected error occurred.")],
            _ => [Error.Unexpected(code: "UNEXPECTED", description: ex.Message)],
        };

    /// <summary>
    /// Flattens the field-level validation errors carried by an <see cref="InvalidRequestException"/>
    /// into one <see cref="Error"/> per field/message pair.
    /// </summary>
    /// <param name="ex">The invalid request exception to map.</param>
    /// <returns>An array of validation errors; a single fallback error if none are present.</returns>
    private static Error[] MapValidationErrors(InvalidRequestException ex)
    {
        if (ex.ValidationErrors is null || ex.ValidationErrors.Count == 0)
        {
            return [Error.Validation(code: ex.ErrorCode, description: ex.Message)];
        }

        return [.. ex.ValidationErrors
            .SelectMany(selector: field => field.Value.Select(selector: message => Error.Validation(code: field.Key, description: message)))];
    }
}
