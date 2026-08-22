// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using Tnosc.Lib.Domain.ValueObjects;
using Tnosc.Lib.Shared.Results;

namespace Tnosc.Lib.Agent.Definitions;

/// <summary>
/// The inference knobs an agent definition may set, independent of which model provider serves it.
/// </summary>
/// <remarks>
/// Only the two settings that are portable across every provider are modelled here. Provider
/// specifics — penalties, top-k, reasoning effort — deliberately stay out: putting them here would
/// make this type name a provider, which is the one thing this project must never do.
/// </remarks>
public sealed record ModelParameters : ValueObject
{
    /// <summary>
    /// The largest sampling temperature any supported provider accepts.
    /// </summary>
    public const float MaxTemperature = 2.0f;

    private ModelParameters(float? temperature, int? maxOutputTokens)
    {
        Temperature = temperature;
        MaxOutputTokens = maxOutputTokens;
    }

    /// <summary>
    /// Gets the sampling temperature, or <see langword="null"/> to accept the provider's default.
    /// </summary>
    public float? Temperature { get; }

    /// <summary>
    /// Gets the output-token ceiling, or <see langword="null"/> to accept the provider's default.
    /// </summary>
    public int? MaxOutputTokens { get; }

    /// <summary>
    /// Gets parameters that defer every setting to the provider's own defaults.
    /// </summary>
    public static ModelParameters Default { get; } = new(temperature: null, maxOutputTokens: null);

    /// <summary>
    /// Creates a <see cref="ModelParameters"/>, validating each supplied setting.
    /// </summary>
    /// <param name="temperature">
    /// The sampling temperature, between <c>0</c> and <see cref="MaxTemperature"/> inclusive, or
    /// <see langword="null"/> for the provider's default.
    /// </param>
    /// <param name="maxOutputTokens">
    /// A positive output-token ceiling, or <see langword="null"/> for the provider's default.
    /// </param>
    /// <returns>
    /// The created <see cref="ModelParameters"/>, or <c>ModelParameters.TemperatureOutOfRange</c> /
    /// <c>ModelParameters.MaxOutputTokensNotPositive</c> when a setting is out of bounds.
    /// </returns>
    public static Result<ModelParameters> Create(float? temperature, int? maxOutputTokens)
    {
        if (temperature is < 0 or > MaxTemperature)
        {
            return ModelParametersErrors.TemperatureOutOfRange;
        }

        if (maxOutputTokens is <= 0)
        {
            return ModelParametersErrors.MaxOutputTokensNotPositive;
        }

        return new ModelParameters(temperature: temperature, maxOutputTokens: maxOutputTokens);
    }
}
