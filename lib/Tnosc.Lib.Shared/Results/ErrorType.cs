// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

namespace Tnosc.Lib.Shared.Results;

/// <summary>
/// Defines the kinds of errors that a result can represent.
/// </summary>
public enum ErrorType
{
    /// <summary>Operation failed for a known reason.</summary>
    Failure,
    /// <summary>An unexpected error occurred.</summary>
    Unexpected,
    /// <summary>Input validation failed.</summary>
    Validation,
    /// <summary>Conflict with an existing resource or state.</summary>
    Conflict,
    /// <summary>Requested resource was not found.</summary>
    NotFound,
    /// <summary>Authentication is required or has failed.</summary>
    Unauthorized,
    /// <summary>Caller is not allowed to perform the operation.</summary>
    Forbidden,
    /// <summary>Custom or domain-specific error.</summary>
    Custom,
}
