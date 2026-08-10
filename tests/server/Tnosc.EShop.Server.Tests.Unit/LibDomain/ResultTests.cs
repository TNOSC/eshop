// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC). All rights reserved.
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using Shouldly;
using Tnosc.Lib.Domain.Results;
using Xunit;

namespace Tnosc.EShop.Server.Tests.Unit.LibDomain;

public sealed class ResultTests
{
    [Fact]
    public void Result_Should_ImplementIResult()
    {
        var result = Result.Success();

        (result is IResult).ShouldBeTrue();
    }

    [Fact]
    public void Result_Should_ConvertImplicitly_When_FromError()
    {
        var error = Error.Failure(code: "Test.Failure", description: "failed");

        Result result = error;

        result.IsError.ShouldBeTrue();
        result.FirstError.ShouldBe(expected: error);
    }

    [Fact]
    public void ResultOfValue_Should_ConvertImplicitly_When_FromValue()
    {
        Result<int> result = 42;

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(expected: 42);
    }

    [Fact]
    public void ResultOfValue_Should_ConvertImplicitly_When_FromError()
    {
        var error = Error.NotFound(code: "Test.NotFound", description: "not found");

        Result<int> result = error;

        result.IsError.ShouldBeTrue();
        result.Errors.ShouldContain(expected: error);
    }

    [Fact]
    public void FirstError_Should_Throw_When_ResultIsSuccess()
    {
        var result = Result.Success();

        Should.Throw<InvalidOperationException>(actual: () => result.FirstError);
    }

    [Fact]
    public void Value_Should_Throw_When_ResultOfValueIsError()
    {
        Result<int> result = Error.Failure(code: "Test.Failure", description: "failed");

        Should.Throw<InvalidOperationException>(actual: () => result.Value);
    }
}
