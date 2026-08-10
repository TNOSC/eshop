// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC). All rights reserved.
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;

namespace Tnosc.Lib.Domain.Results;

/// <summary>
/// Represents a class containing utility methods for handling operation results.
/// </summary>
public class Result : IResult
{
    /// <summary>
    /// Gets a value indicating whether the state is a success.
    /// </summary>
    public bool IsSuccess => !Errors.Any();

    /// <summary>
    /// Gets a value indicating whether the state is error.
    /// </summary>
    public bool IsError => Errors.Any();

    /// <summary>
    /// Gets the collection of errors.
    /// </summary>
    public IEnumerable<Error> Errors { get; protected set; } = [];

    /// <summary>
    /// Represents a successful operation result.
    /// </summary>
    public static Result Success() => new Result();

    /// <summary>
    /// Gets the first error.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when no errors are present.</exception>
    public Error FirstError
    {
        get
        {
            if (!IsError)
            {
                throw new InvalidOperationException(message: "The FirstError property cannot be accessed when Errors property is empty. Check IsError before accessing FirstError.");
            }

            return Errors.First();
        }
    }

    /// <summary>
    /// Provides implicit conversion operators for the <see cref="Result"/> type.
    /// </summary>
    public static implicit operator Result(Error error)
        => new Result() { Errors = [error] };

    /// <summary>
    /// Provides implicit conversion operators for the <see cref="Result"/> type.
    /// </summary>
    public static implicit operator Result(Error[] errors)
        => new Result() { Errors = errors};

    /// <summary>
    /// Provides implicit conversion operators for the <see cref="Result"/> type.
    /// </summary>
    public static implicit operator Result(List<Error> errors)
        => new Result() { Errors = errors };
}

/// <summary>
/// Represents the result of an operation that either contains a value of type
/// <typeparamref name="TValue"/> when successful, or one or more <see cref="Error"/>
/// instances when the operation failed.
/// </summary>
/// <typeparam name="TValue">The type of the value contained in a successful result.</typeparam>
public class Result<TValue> : Result, IResult<TValue>
{
    private readonly TValue? _value;

    private Result(TValue value)
    {
        if (value is null)
        {
            throw new ArgumentNullException(paramName: nameof(value));
        }

        _value = value;
    }

    private Result(Error error) =>
        Errors = [error];

    private Result(IEnumerable<Error> errors)
    {
        ArgumentNullException.ThrowIfNull(argument: errors);

        if (!errors.Any())
        {
            throw new ArgumentException(message: "Cannot create an Result<TValue> from an empty collection of errors. Provide at least one error.", paramName: nameof(errors));
        }

        Errors = errors;
    }

    /// <summary>
    /// Gets the value.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when no value is present.</exception>
    public TValue Value
    {
        get
        {
            if (IsError)
            {
                throw new InvalidOperationException(message: "The Value property cannot be accessed when Errors property is not empty. Check IsSuccess or IsError before accessing the Value.");
            }

            return _value!;
        }
    }

    /// <summary>
    /// Defines an implicit conversion operator that constructs a <see cref="Result{TValue}"/>
    /// from a value of type <typeparamref name="TValue"/>.
    /// </summary>
    /// <param name="value">The value of type <typeparamref name="TValue"/> used to create a successful result.</param>
    /// <returns>A new <see cref="Result{TValue}"/> instance representing a successful outcome with the provided value.</returns>
    public static implicit operator Result<TValue>(TValue value)
        => new Result<TValue>(value: value);

    /// <summary>
    /// Defines an implicit conversion operator that constructs a <see cref="Result{TValue}"/>
    /// from an <see cref="Error"/> instance.
    /// </summary>
    /// <param name="error">The <see cref="Error"/> instance encapsulating the error information
    /// used to create a failure result.</param>
    /// <returns>A new <see cref="Result{TValue}"/> instance representing a failure with the provided error.</returns>
    public static implicit operator Result<TValue>(Error error)
        => new Result<TValue>(error: error);

    /// <summary>
    /// Defines implicit conversion operators for the <see cref="Result{TValue}"/> struct.
    /// </summary>
    /// <remarks>
    /// <remarks>
    /// These operators allow for implicit conversion between <typeparamref name="TValue"/>, <see cref="Error"/>,
    /// lists of <see cref="Error"/>, and arrays of <see cref="Error"/> into a <see cref="Result{TValue}"/> object.
    /// </remarks>
    /// lists of <see cref="Error"/>, and arrays of <see cref="Error"/> into a <see cref="Result{TValue}"/> object.
    /// </remarks>
    /// <example>
    /// This conversion simplifies the process of creating instances of <see cref="Result{TValue}"/>
    /// by allowing direct assignment from supported types.
    /// </example>
    public static implicit operator Result<TValue>(List<Error> errors)
        => new Result<TValue>(errors: errors);

    /// <summary>
    /// Provides implicit conversion operators for the <see cref="Result{TValue}"/> type.
    /// </summary>
    public static implicit operator Result<TValue>(Error[] errors)
        => new Result<TValue>(errors: errors);
}
