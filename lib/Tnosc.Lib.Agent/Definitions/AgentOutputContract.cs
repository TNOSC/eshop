// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using Tnosc.Lib.Domain.ValueObjects;
using Tnosc.Lib.Shared.Results;

namespace Tnosc.Lib.Agent.Definitions;

/// <summary>
/// The structured shape an agent is asked to return, or <see cref="None"/> when it answers in prose.
/// </summary>
/// <remarks>
/// <para>
/// Structured output is opt-in on purpose. Prose is the common case, and asking a model for a schema
/// while it is also calling tools is supported unevenly across providers — making every agent
/// declare a shape would buy fragility for agents that never needed one.
/// </para>
/// <para>
/// The contract carries a <see cref="Type"/> rather than a JSON schema string. <see cref="Type"/> is
/// a BCL primitive, so naming it costs this project no dependency, and it lets the infrastructure
/// layer generate the schema with whatever the provider SDK offers instead of hand-maintaining one.
/// </para>
/// </remarks>
public sealed record AgentOutputContract : ValueObject
{
    private AgentOutputContract(Type? outputType) => OutputType = outputType;

    /// <summary>
    /// Gets the declared output type, or <see langword="null"/> when the agent answers in prose.
    /// </summary>
    public Type? OutputType { get; }

    /// <summary>
    /// Gets a value indicating whether this contract declares a structured shape.
    /// </summary>
    public bool IsDeclared => OutputType is not null;

    /// <summary>
    /// Gets the contract for an agent that answers in prose.
    /// </summary>
    public static AgentOutputContract None { get; } = new(outputType: null);

    /// <summary>
    /// Creates a contract declaring <typeparamref name="TOutput"/> as the agent's output shape.
    /// </summary>
    /// <typeparam name="TOutput">The type the agent's answer must bind to.</typeparam>
    /// <returns>The created <see cref="AgentOutputContract"/>.</returns>
    public static AgentOutputContract For<TOutput>() => new(outputType: typeof(TOutput));

    /// <summary>
    /// Creates a contract declaring <paramref name="outputType"/> as the agent's output shape.
    /// </summary>
    /// <param name="outputType">The type the agent's answer must bind to.</param>
    /// <returns>
    /// The created <see cref="AgentOutputContract"/>, or
    /// <c>AgentOutputContract.OutputTypeNotSupported</c> when <paramref name="outputType"/> cannot
    /// carry a structured answer.
    /// </returns>
    public static Result<AgentOutputContract> Create(Type? outputType)
    {
        if (outputType is null)
        {
            return None;
        }

        // A schema needs named members to bind into. Primitives, strings and open generics have
        // none, and a model asked for such a "schema" returns something that never binds.
        bool isBindable = outputType is { IsClass: true, ContainsGenericParameters: false } or
                          { IsValueType: true, IsPrimitive: false, IsEnum: false } &&
                          outputType != typeof(string);

        if (!isBindable)
        {
            return AgentOutputContractErrors.OutputTypeNotSupported;
        }

        return new AgentOutputContract(outputType: outputType);
    }
}
