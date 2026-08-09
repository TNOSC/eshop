// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC). All rights reserved.
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using Tnosc.Lib.Application.Observabilities;

namespace Tnosc.Lib.Application.Exceptions;

/// <summary>
/// Exception thrown when a request is invalid or malformed.
/// </summary>
public class InvalidRequestException : BaseException
{
    /// <summary>
    /// Gets the validation errors associated with the invalid request.
    /// </summary>
    public IReadOnlyDictionary<string, string[]> ValidationErrors { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="InvalidRequestException"/> class with a specified error message, validation errors, inner exception, and correlation ID.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="validationErrors">A dictionary containing validation errors, where the key is the field name and the value is an array of error messages.</param>
    /// <param name="inner">An optional inner exception.</param>
    /// <param name="correlationId">An optional correlation identifier for tracing. When omitted, falls back to <see cref="CorrelationIdContext.Current"/>.</param>
    public InvalidRequestException(
        string message,
        IReadOnlyDictionary<string, string[]> validationErrors,
        Exception? inner = null,
        string? correlationId = null)
        : base(
            message: message,
            errorCode: "INVALID_REQUEST",
            errorCategory: "InvalidRequest",
            isRetriable: false,
            severity: "Warning",
            correlationId: correlationId,
            inner: inner) 
    {
        ValidationErrors = validationErrors;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="InvalidRequestException"/> class.
    /// </summary>
    internal InvalidRequestException()
        : base() { }

    /// <summary>
    /// Initializes a new instance of the <see cref="InvalidRequestException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    internal InvalidRequestException(string? message)
        : base(message: message) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="InvalidRequestException"/> class with a specified error message and inner exception.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="innerException">The inner exception that is the cause of this exception.</param>
    internal InvalidRequestException(string? message, Exception? innerException)
        : base(message: message, innerException: innerException) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="NotFoundException"/> class with detailed error information.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="errorCode">The error code.</param>
    /// <param name="errorCategory">The error category.</param>
    /// <param name="isRetriable">A value indicating whether the exception is retriable.</param>
    /// <param name="severity">The severity level of the error.</param>
    /// <param name="correlationId">An optional correlation identifier for tracing.</param>
    /// <param name="inner">An optional inner exception.</param>
    internal InvalidRequestException(
        string message,
        string errorCode,
        string errorCategory,
        bool isRetriable,
        string severity,
        string? correlationId = null,
        Exception? inner = null)
        : base(
            message: message,
            errorCode: errorCode,
            errorCategory: errorCategory,
            isRetriable: isRetriable,
            severity: severity,
            correlationId: correlationId,
            inner: inner) { }
}
