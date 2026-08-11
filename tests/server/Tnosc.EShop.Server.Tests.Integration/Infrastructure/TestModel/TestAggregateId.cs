// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using Tnosc.Lib.Domain;

namespace Tnosc.EShop.Server.Tests.Integration.Infrastructure.TestModel;

/// <summary>
/// Strongly-typed identifier for <see cref="TestAggregate"/>, a test-only aggregate used to exercise
/// the outbox / auditing pipeline end to end against a real Postgres instance.
/// </summary>
internal sealed record TestAggregateId : GuidEntityId, IEntityId<TestAggregateId, Guid>
{
    private TestAggregateId(Guid value)
        : base(value)
    {
    }

    /// <summary>
    /// Creates a new, random <see cref="TestAggregateId"/>.
    /// </summary>
    /// <returns>A new <see cref="TestAggregateId"/>.</returns>
    public static TestAggregateId New() => new(Guid.NewGuid());

    /// <inheritdoc />
    public static TestAggregateId From(Guid value) => new(value);
}
