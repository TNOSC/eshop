// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using Tnosc.Lib.Application.Contexts;

namespace Tnosc.Lib.Application.Exceptions;

/// <summary>
/// Exception thrown when a conflict occurs (for example, when attempting an operation
/// that cannot be completed because it would conflict with the current state).
/// </summary>
public class ConflictException : BaseException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ConflictException"/> class with a specified error message, correlation ID, and inner exception.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="correlationId">An optional correlation identifier for tracing. When <c>null</c>, falls back to <see cref="CorrelationIdContext.Current"/>.</param>
    /// <param name="inner">An optional inner exception.</param>
    /// <param name="isRetriable">A value indicating whether the exception is retriable.</param>
    public ConflictException(
        string message,
        string? correlationId,
        Exception? inner,
        bool isRetriable = false)
        : base(
            message: message,
            errorCode: "CONFLICT",
            errorCategory: "Conflict",
            isRetriable: isRetriable,
            severity: "Warning",
            correlationId: correlationId,
            inner: inner) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConflictException"/> class.
    /// </summary>
    internal ConflictException()
        : base() { }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConflictException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    internal ConflictException(string? message)
        : base(message: message) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConflictException"/> class with a specified error message and inner exception.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="innerException">The inner exception that is the cause of this exception.</param>
    internal ConflictException(string? message, Exception? innerException)
        : base(message: message, innerException: innerException) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConflictException"/> class with detailed error information.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="errorCode">The error code.</param>
    /// <param name="errorCategory">The error category.</param>
    /// <param name="isRetriable">A value indicating whether the exception is retriable.</param>
    /// <param name="severity">The severity level of the error.</param>
    /// <param name="correlationId">An optional correlation identifier for tracing.</param>
    /// <param name="inner">An optional inner exception.</param>
    internal ConflictException(
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
