// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC). All rights reserved.
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Text;
using Tnosc.Lib.Application.Observabilities;

namespace Tnosc.Lib.Application.Exceptions;

/// <summary>
/// Base class for application-specific exceptions that carries structured error metadata.
/// </summary>
public abstract class BaseException : Exception
{
    /// <summary>
    /// Gets the machine-readable error code.
    /// </summary>
    public string ErrorCode { get; }        // Machine-readable code

    /// <summary>
    /// Gets the error category (for example: InvalidRequest, Unauthorized, Conflict, SystemFailure).
    /// </summary>
    public string ErrorCategory { get; }    // One of: InvalidRequest, Unauthorized, Conflict, SystemFailure

    /// <summary>
    /// Gets a value indicating whether the operation that caused the error can be retried.
    /// </summary>
    public bool IsRetriable { get; }        // Can the operation be retried?

    /// <summary>
    /// Gets the severity level of the error (for example: Info, Warning, Critical).
    /// </summary>
    public string Severity { get; }         // Info, Warning, Critical

    /// <summary>
    /// Gets an optional correlation identifier for tracing/logging.
    /// </summary>
    public string? CorrelationId { get; }   // For tracing/logging

    /// <summary>
    /// Initializes a new instance of the <see cref="BaseException"/> class with structured error metadata.
    /// </summary>
    /// <param name="message">The human-readable error message.</param>
    /// <param name="errorCode">The machine-readable error code.</param>
    /// <param name="errorCategory">The error category (InvalidRequest, Unauthorized, Conflict, SystemFailure, etc.).</param>
    /// <param name="isRetriable">Whether the operation may be retried.</param>
    /// <param name="severity">The severity of the error (Info, Warning, Critical).</param>
    /// <param name="correlationId">An optional correlation id for tracing/logging. When omitted, falls back to <see cref="CorrelationIdContext.Current"/>.</param>
    /// <param name="inner">An optional inner exception.</param>
    protected BaseException(
        string message,
        string errorCode,
        string errorCategory,
        bool isRetriable,
        string severity,
        string? correlationId = null,
        Exception? inner = null)
        : base(message, inner)
    {
        ErrorCode = errorCode;
        ErrorCategory = errorCategory;
        IsRetriable = isRetriable;
        Severity = severity;
        CorrelationId = correlationId ?? CorrelationIdContext.Current;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BaseException"/> class.
    /// </summary>
    protected BaseException()
        : base()
    {
        ErrorCode = string.Empty;
        ErrorCategory = string.Empty;
        Severity = string.Empty;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BaseException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    protected BaseException(string? message)
        : base(message: message)
    {
        ErrorCode = string.Empty;
        ErrorCategory = string.Empty;
        Severity = string.Empty;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BaseException"/> class with a specified error message and a reference to the inner exception that is the cause of this exception.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="innerException">The exception that is the cause of the current exception, or null.</param>
    protected BaseException(string? message, Exception? innerException)
        : base(message: message, innerException: innerException)
    {
        ErrorCode = string.Empty;
        ErrorCategory = string.Empty;
        Severity = string.Empty;
    }
}
