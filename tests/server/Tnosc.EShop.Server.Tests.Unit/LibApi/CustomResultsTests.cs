// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC). All rights reserved.
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Tnosc.Lib.Api;
using Tnosc.Lib.Domain.Results;
using Xunit;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace Tnosc.EShop.Server.Tests.Unit.LibApi;

public sealed class CustomResultsTests
{
    public static TheoryData<ErrorType, int> ErrorTypeStatusCodes => new()
    {
        { ErrorType.Validation, StatusCodes.Status400BadRequest },
        { ErrorType.Unauthorized, StatusCodes.Status401Unauthorized },
        { ErrorType.Forbidden, StatusCodes.Status403Forbidden },
        { ErrorType.NotFound, StatusCodes.Status404NotFound },
        { ErrorType.Conflict, StatusCodes.Status409Conflict },
        { ErrorType.Failure, StatusCodes.Status500InternalServerError },
        { ErrorType.Unexpected, StatusCodes.Status500InternalServerError },
    };

    [Theory]
    [MemberData(nameof(ErrorTypeStatusCodes))]
    public async Task Problem_Should_MapToExpectedStatusCode_When_ErrorTypeIsNotCustom(ErrorType errorType, int expectedStatusCode)
    {
        Error error = errorType switch
        {
            ErrorType.Validation => Error.Validation(code: "Test.Validation", description: "invalid"),
            ErrorType.Unauthorized => Error.Unauthorized(code: "Test.Unauthorized", description: "unauthorized"),
            ErrorType.Forbidden => Error.Forbidden(code: "Test.Forbidden", description: "forbidden"),
            ErrorType.NotFound => Error.NotFound(code: "Test.NotFound", description: "not found"),
            ErrorType.Conflict => Error.Conflict(code: "Test.Conflict", description: "conflict"),
            ErrorType.Failure => Error.Failure(code: "Test.Failure", description: "failure"),
            ErrorType.Unexpected => Error.Unexpected(code: "Test.Unexpected", description: "unexpected"),
            _ => throw new ArgumentOutOfRangeException(nameof(errorType)),
        };

        int statusCode = await ExecuteAndGetStatusCodeAsync((Result)error);

        statusCode.ShouldBe(expectedStatusCode);
    }

    [Fact]
    public async Task Problem_Should_UseNumericType_When_ErrorTypeIsCustomAndInRange()
    {
        Result result = Error.Custom(type: 418, code: "Test.Teapot", description: "I'm a teapot");

        int statusCode = await ExecuteAndGetStatusCodeAsync(result);

        statusCode.ShouldBe(418);
    }

    [Theory]
    [InlineData(99)]
    [InlineData(600)]
    public async Task Problem_Should_FallBackTo500_When_CustomNumericTypeIsOutOfRange(int numericType)
    {
        Result result = Error.Custom(type: numericType, code: "Test.Custom", description: "custom");

        int statusCode = await ExecuteAndGetStatusCodeAsync(result);

        statusCode.ShouldBe(StatusCodes.Status500InternalServerError);
    }

    [Theory]
    [InlineData(100)]
    [InlineData(599)]
    public async Task Problem_Should_PassThroughNumericType_When_CustomNumericTypeIsAtBoundary(int numericType)
    {
        Result result = Error.Custom(type: numericType, code: "Test.Custom", description: "custom");

        int statusCode = await ExecuteAndGetStatusCodeAsync(result);

        statusCode.ShouldBe(numericType);
    }

    [Fact]
    public void Problem_Should_Throw_When_ResultIsSuccess()
    {
        var result = Result.Success();

        Should.Throw<InvalidOperationException>(() => CustomResults.Problem(result));
    }

    [Fact]
    public void Problem_Should_IncludeErrorsExtension_When_ErrorTypeIsValidation()
    {
        var error = Error.Validation(code: "Test.Validation", description: "invalid");
        Result result = error;

        var problemResult = CustomResults.Problem(result) as ProblemHttpResult;

        problemResult.ShouldNotBeNull();
        problemResult.ProblemDetails.Extensions.ShouldContainKey("errors");
        var errors = problemResult.ProblemDetails.Extensions["errors"] as Dictionary<string, string[]>;
        errors.ShouldNotBeNull();
        errors.ShouldContainKey(error.Code);
        errors[error.Code].ShouldBe([error.Description]);
    }

    private static async Task<int> ExecuteAndGetStatusCodeAsync(Result result)
    {
        IResult httpResult = CustomResults.Problem(result);
        var httpContext = new DefaultHttpContext
        {
            RequestServices = new ServiceCollection().AddLogging().BuildServiceProvider(),
            Response = { Body = new MemoryStream() },
        };

        await httpResult.ExecuteAsync(httpContext);

        return httpContext.Response.StatusCode;
    }
}
