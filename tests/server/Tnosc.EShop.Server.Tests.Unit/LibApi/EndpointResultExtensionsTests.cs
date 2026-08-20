// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Tnosc.Lib.Api.Extensions;
using Tnosc.Lib.Shared.Results;
using Xunit;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace Tnosc.EShop.Server.Tests.Unit.LibApi;

public sealed class EndpointResultExtensionsTests
{
    [Fact]
    public async Task ToHttp_Should_ReturnDefaultStatusCode_When_ResultIsSuccessAndNoStatusCodeGiven()
    {
        var result = Result.Success();

        IResult httpResult = result.ToHttp();

        int statusCode = await ExecuteAndGetStatusCodeAsync(httpResult: httpResult);
        statusCode.ShouldBe(expected: StatusCodes.Status204NoContent);
    }

    [Fact]
    public async Task ToHttp_Should_ReturnGivenStatusCode_When_ResultIsSuccess()
    {
        var result = Result.Success();

        IResult httpResult = result.ToHttp(successStatusCode: StatusCodes.Status200OK);

        int statusCode = await ExecuteAndGetStatusCodeAsync(httpResult: httpResult);
        statusCode.ShouldBe(expected: StatusCodes.Status200OK);
    }

    [Fact]
    public async Task ToHttp_Should_ReturnProblem_When_ResultIsFailure()
    {
        Result result = Error.NotFound(code: "Test.NotFound", description: "not found");

        IResult httpResult = result.ToHttp();

        int statusCode = await ExecuteAndGetStatusCodeAsync(httpResult: httpResult);
        statusCode.ShouldBe(expected: StatusCodes.Status404NotFound);
    }

    [Fact]
    public async Task ToHttpOfValue_Should_InvokeOnSuccess_When_ResultIsSuccess()
    {
        Result<int> result = 42;

        IResult httpResult = result.ToHttp(onSuccess: static value => Results.Ok(value: value));

        int statusCode = await ExecuteAndGetStatusCodeAsync(httpResult: httpResult);
        statusCode.ShouldBe(expected: StatusCodes.Status200OK);
    }

    [Fact]
    public async Task ToHttpOfValue_Should_ReturnProblem_When_ResultIsFailure()
    {
        Result<int> result = Error.Conflict(code: "Test.Conflict", description: "conflict");

        IResult httpResult = result.ToHttp(onSuccess: static value => Results.Ok(value: value));

        int statusCode = await ExecuteAndGetStatusCodeAsync(httpResult: httpResult);
        statusCode.ShouldBe(expected: StatusCodes.Status409Conflict);
    }

    [Fact]
    public async Task ToCreated_Should_SetLocationHeader_When_ResultIsSuccess()
    {
        Result<int> result = 42;

        IResult httpResult = result.ToCreated(location: static value => $"/items/{value}");

        var httpContext = new DefaultHttpContext
        {
            RequestServices = new ServiceCollection().AddLogging().BuildServiceProvider(),
            Response = { Body = new MemoryStream() },
        };
        await httpResult.ExecuteAsync(httpContext: httpContext);

        httpContext.Response.StatusCode.ShouldBe(expected: StatusCodes.Status201Created);
        httpContext.Response.Headers.Location.ToString().ShouldBe(expected: "/items/42");
    }

    [Fact]
    public async Task ToCreated_Should_ReturnProblem_When_ResultIsFailure()
    {
        Result<int> result = Error.Validation(code: "Test.Validation", description: "invalid");

        IResult httpResult = result.ToCreated(location: static value => $"/items/{value}");

        int statusCode = await ExecuteAndGetStatusCodeAsync(httpResult: httpResult);
        statusCode.ShouldBe(expected: StatusCodes.Status400BadRequest);
    }

    private static async Task<int> ExecuteAndGetStatusCodeAsync(IResult httpResult)
    {
        var httpContext = new DefaultHttpContext
        {
            RequestServices = new ServiceCollection().AddLogging().BuildServiceProvider(),
            Response = { Body = new MemoryStream() },
        };

        await httpResult.ExecuteAsync(httpContext: httpContext);

        return httpContext.Response.StatusCode;
    }
}
