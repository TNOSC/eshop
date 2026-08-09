// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC). All rights reserved.
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Text;

namespace Tnosc.Lib.Domain.Results;

/// <summary>
/// Represents a domain error with a code, description and type.
/// </summary>
public readonly record struct Error
{
    private Error(
        string code,
        string description,
        ErrorType type)
    {
        Code = code;
        Description = description;
        Type = type;
    }

    private Error(
        string code,
        string description,
        int numericType)
    {
        Code = code;
        Description = description;
        NumericType = numericType;

        Type = ErrorType.Custom;
    }

    /// <summary>
    /// Gets the unique error code.
    /// </summary>
    public string Code { get; }

    /// <summary>
    /// Gets the error description.
    /// </summary>
    public string Description { get; }

    /// <summary>
    /// Gets the error type.
    /// </summary>
    public ErrorType Type { get; }

    /// <summary>
    /// Gets the numeric value of the type.
    /// </summary>
    public int NumericType { get; }

    /// <summary>
    /// Creates a failure error with the specified code and description.
    /// </summary>
    /// <param name="code">Unique error code.</param>
    /// <param name="description">Human-readable description of the error.</param>
    public static Error Failure(string code, string description) =>
        new Error(
            code: code,
            description: description,
            type: ErrorType.Failure);

    /// <summary>
    /// Creates an unexpected error with the specified code and description.
    /// </summary>
    /// <param name="code">Unique error code.</param>
    /// <param name="description">Human-readable description of the error.</param>
    public static Error Unexpected(string code, string description) =>
        new Error(
            code: code,
            description: description,
            type: ErrorType.Unexpected);

    /// <summary>
    /// Creates a validation error with the specified code and description.
    /// </summary>
    /// <param name="code">Unique error code.</param>
    /// <param name="description">Human-readable description of the error.</param>
    public static Error Validation(string code, string description) =>
        new Error(
            code: code,
            description: description,
            type: ErrorType.Validation);

    /// <summary>
    /// Creates a conflict error with the specified code and description.
    /// </summary>
    /// <param name="code">Unique error code.</param>
    /// <param name="description">Human-readable description of the error.</param>
    public static Error Conflict(string code, string description) =>
        new Error(
            code: code,
            description: description,
            type: ErrorType.Conflict);

    /// <summary>
    /// Creates a not-found error with the specified code and description.
    /// </summary>
    /// <param name="code">Unique error code.</param>
    /// <param name="description">Human-readable description of the error.</param>
    public static Error NotFound(string code, string description) =>
        new Error(
            code: code,
            description: description,
            type: ErrorType.NotFound);

    /// <summary>
    /// Creates an unauthorized error with the specified code and description.
    /// </summary>
    /// <param name="code">Unique error code.</param>
    /// <param name="description">Human-readable description of the error.</param>
    public static Error Unauthorized(string code, string description) =>
        new Error(
            code: code,
            description: description,
            type: ErrorType.Unauthorized);

    /// <summary>
    /// Creates a forbidden error with the specified code and description.
    /// </summary>
    /// <param name="code">Unique error code.</param>
    /// <param name="description">Human-readable description of the error.</param>
    public static Error Forbidden(string code, string description) =>
        new Error(
            code: code,
            description: description,
            type: ErrorType.Forbidden);

    /// <summary>
    /// Creates a custom-type error with the specified numeric type, code and description.
    /// </summary>
    /// <param name="type">Numeric custom type identifier.</param>
    /// <param name="code">Unique error code.</param>
    /// <param name="description">Human-readable description of the error.</param>
    public static Error Custom(int type, string code, string description) =>
        new Error(
            code: code,
            description: description,
            numericType: type);

    /// <summary>
    /// Determines whether this instance and another specified Error object have the same value.
    /// </summary>
    /// <param name="other">The Error to compare to this instance.</param>
    /// <returns>true if the value of the other parameter is the same as this instance; otherwise, false.</returns>
    public bool Equals(Error other) =>
        Type == other.Type &&
               NumericType == other.NumericType &&
               string.Equals(
                   a: Code,
                   b: other.Code,
                   comparisonType: StringComparison.Ordinal) &&
               string.Equals(
                   a: Description,
                   b: other.Description,
                   comparisonType: StringComparison.Ordinal);

    /// <summary>
    /// Returns the hash code for this instance.
    /// </summary>
    public override int GetHashCode()
        => HashCode.Combine(
            value1: Code,
            value2: Description,
            value3: Type,
            value4: NumericType);
}
