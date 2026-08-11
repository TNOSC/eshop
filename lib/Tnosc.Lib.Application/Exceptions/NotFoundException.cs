// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using Tnosc.Lib.Application.Observabilities;

namespace Tnosc.Lib.Application.Exceptions;

/// <summary>
/// Exception thrown when a requested resource is not found.
/// </summary>
public class NotFoundException : BaseException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NotFoundException"/> class with a specified error message, correlation ID, and inner exception.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="correlationId">An optional correlation identifier for tracing. When <c>null</c>, falls back to <see cref="CorrelationIdContext.Current"/>.</param>
    /// <param name="inner">An optional inner exception.</param>
    public NotFoundException(
        string message,
        string? correlationId,
        Exception? inner)
        : base(
            message: message,
            errorCode: "NOT_FOUND",
            errorCategory: "NotFound",
            isRetriable: false,
            severity: "Info",
            correlationId: correlationId,
            inner: inner) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="NotFoundException"/> class.
    /// </summary>
    internal NotFoundException()
        : base() { }

    /// <summary>
    /// Initializes a new instance of the <see cref="NotFoundException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    internal NotFoundException(string? message)
        : base(message: message) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="NotFoundException"/> class with a specified error message and inner exception.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="innerException">The inner exception that is the cause of this exception.</param>
    internal NotFoundException(string? message, Exception? innerException)
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
    internal NotFoundException(
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
