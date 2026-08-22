// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using Tnosc.Lib.Shared.Results;

namespace Tnosc.Lib.Agent.Definitions;

/// <summary>
/// Every way a candidate <see cref="ModelParameters"/> can break its invariants.
/// </summary>
public static class ModelParametersErrors
{
    /// <summary>
    /// Gets the error returned when the sampling temperature is outside the supported range.
    /// </summary>
    public static Error TemperatureOutOfRange => Error.Validation(
        code: "ModelParameters.TemperatureOutOfRange",
        description: $"Temperature must be between 0 and {ModelParameters.MaxTemperature}.");

    /// <summary>
    /// Gets the error returned when the output-token ceiling is zero or negative.
    /// </summary>
    public static Error MaxOutputTokensNotPositive => Error.Validation(
        code: "ModelParameters.MaxOutputTokensNotPositive",
        description: "The maximum number of output tokens must be greater than zero.");
}
