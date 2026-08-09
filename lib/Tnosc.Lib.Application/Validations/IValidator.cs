// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC). All rights reserved.
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Tnosc.Lib.Domain.Results;

namespace Tnosc.Lib.Application.Validations;

/// <summary>
/// Validator interface for validating requests before handler execution.
/// Validators are automatically discovered during assembly scanning and executed by ValidationInterceptor.
/// </summary>
/// <typeparam name="T">The type of request to validate (contravariant to support validation of base types)</typeparam>
public interface IValidator<in T>
{
    /// <summary>
    /// Validates the specified request asynchronously.
    /// </summary>
    /// <param name="request">The request to validate (typically a command or query)</param>
    /// <param name="cancellationToken">Cancellation token for async operations</param>
    /// <returns>
    /// A <see cref="Result"/> indicating success or failure.
    /// If validation fails, the result contains all validation errors.
    /// </returns>
    ValueTask<Result> ValidateAsync(T request, CancellationToken cancellationToken);
}
