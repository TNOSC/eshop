// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using Shouldly;
using Tnosc.Lib.Agent.Definitions;
using Tnosc.Lib.Shared.Results;
using Xunit;

namespace Tnosc.EShop.Agent.Tests.Unit.Definitions;

/// <summary>
/// <see cref="ModelParameters"/> bounds the two inference settings that are portable across providers.
/// </summary>
public sealed class ModelParametersTests
{
    [Theory]
    [InlineData(null, null)]
    [InlineData(0f, null)]
    [InlineData(2f, 1)]
    [InlineData(0.7f, 4096)]
    public void Create_Should_Succeed_When_SettingsAreInRange(float? temperature, int? maxOutputTokens)
    {
        // Act
        Result<ModelParameters> result = ModelParameters.Create(
            temperature: temperature,
            maxOutputTokens: maxOutputTokens);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.Temperature.ShouldBe(expected: temperature);
        result.Value.MaxOutputTokens.ShouldBe(expected: maxOutputTokens);
    }

    [Theory]
    [InlineData(-0.1f)]
    [InlineData(2.1f)]
    public void Create_Should_Fail_When_TemperatureIsOutOfRange(float temperature)
    {
        // Act
        Result<ModelParameters> result = ModelParameters.Create(
            temperature: temperature,
            maxOutputTokens: null);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe(expected: "ModelParameters.TemperatureOutOfRange");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Create_Should_Fail_When_MaxOutputTokensIsNotPositive(int maxOutputTokens)
    {
        // Act
        Result<ModelParameters> result = ModelParameters.Create(
            temperature: null,
            maxOutputTokens: maxOutputTokens);

        // Assert
        result.IsError.ShouldBeTrue();
        result.FirstError.Code.ShouldBe(expected: "ModelParameters.MaxOutputTokensNotPositive");
    }

    [Fact]
    public void Default_Should_Defer_Every_Setting_To_The_Provider()
    {
        // Act & Assert
        ModelParameters.Default.Temperature.ShouldBeNull();
        ModelParameters.Default.MaxOutputTokens.ShouldBeNull();
    }
}
