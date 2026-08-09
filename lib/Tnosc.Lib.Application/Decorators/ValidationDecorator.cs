// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC). All rights reserved.
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Tnosc.Lib.Application.Commands;
using Tnosc.Lib.Application.Validations;
using Tnosc.Lib.Domain.Results;

namespace Tnosc.Lib.Application.Decorators;

/// <summary>
/// Provides validation decorators for command handlers.
/// </summary>
public static class ValidationDecorator
{
    /// <summary>
    /// Validation decorator for command handlers that return a response.
    /// </summary>
    /// <typeparam name="TCommand">The command type.</typeparam>
    /// <typeparam name="TResponse">The response type.</typeparam>
    /// <param name="innerHandler">The inner command handler.</param>
    /// <param name="validators">A collection of validators for the command.</param>
    public sealed class CommandHandler<TCommand, TResponse>(
        ICommandHandler<TCommand, TResponse> innerHandler,
        IEnumerable<IValidator<TCommand>> validators)
        : ICommandHandler<TCommand, TResponse>, IHandlerDecorator
        where TCommand : ICommand<TResponse>
    {
        /// <inheritdoc />
        public object InnerHandler => innerHandler;

        /// <summary>
        /// Handles the command after running validators. Returns validation errors if any.
        /// </summary>
        /// <param name="command">The command to handle.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        public async ValueTask<Result<TResponse>> HandleAsync(TCommand command, CancellationToken cancellationToken = default)
        {
            Error[] errors = await ValidateAsync(command, validators, cancellationToken);

            if (!errors.Any())
            {
                return await innerHandler.HandleAsync(command, cancellationToken);
            }

            return errors;
        }
    }

    /// <summary>
    /// Validation decorator for command handlers that do not return a response.
    /// </summary>
    /// <typeparam name="TCommand">The command type.</typeparam>
    /// <param name="innerHandler">The inner command handler.</param>
    /// <param name="validators">A collection of validators for the command.</param>
    public sealed class CommandBaseHandler<TCommand>(
        ICommandHandler<TCommand> innerHandler,
        IEnumerable<IValidator<TCommand>> validators)
        : ICommandHandler<TCommand>, IHandlerDecorator
        where TCommand : ICommand
    {
        /// <inheritdoc />
        public object InnerHandler => innerHandler;

        /// <summary>
        /// Handles the command after running validators. Returns validation errors if any.
        /// </summary>
        /// <param name="command">The command to handle.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        public async ValueTask<Result> HandleAsync(TCommand command, CancellationToken cancellationToken = default)
        {
            Error[] errors = await ValidateAsync(command, validators, cancellationToken);

            if (!errors.Any())
            {
                return await innerHandler.HandleAsync(command, cancellationToken);
            }

            return errors;
        }
    }

    /// <summary>
    /// Validates the command using the provided validators and returns an array of validation errors.
    /// </summary>
    /// <typeparam name="TCommand">The command type.</typeparam>
    /// <param name="command">The command to validate.</param>
    /// <param name="validators">A collection of validators for the command.</param>
    /// <param name="token">A cancellation token.</param>
    /// <returns>An array of validation errors; empty if none.</returns>
    private static async ValueTask<Error[]> ValidateAsync<TCommand>(
        TCommand command,
        IEnumerable<IValidator<TCommand>> validators,
        CancellationToken token = default)
    {
        if (!validators.Any())
        {
            return [];
        }

        Result[] validationResults = await Task.WhenAll(
            validators.Select(validator =>
                validator.ValidateAsync(command, token)
                .AsTask()));

        return [.. validationResults
            .Where(validationResult => validationResult.IsError)
            .SelectMany(validationResult => validationResult.Errors)];
    }
}
