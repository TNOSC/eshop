// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System.Collections.Generic;

namespace Tnosc.Lib.Shared.Results;

/// <summary>
/// Represents a result of an operation which encapsulates either a success or an error state.
/// </summary>
public interface IResult
{
    /// <summary>
    /// Gets the list of errors associated with the operation result.
    /// </summary>
    IEnumerable<Error>? Errors { get; }

    /// <summary>
    /// Indicates whether the operation result represents a success state.
    /// </summary>
    bool IsSuccess { get; }

    /// <summary>
    /// Gets a value indicating whether the result represents an error state.
    /// </summary>
    bool IsError { get; }
}

/// <summary>
/// Represents the result of an operation with the ability to define a value type
/// and encapsulate success or error states.
/// </summary>
/// <typeparam name="TValue">The type of the value returned on a successful result.</typeparam>
public interface IResult<out TValue> : IResult
{
    /// <summary>
    /// Gets the value associated with the operation result. This can represent the successful outcome of the operation.
    /// </summary>
    TValue? Value { get; }
}
