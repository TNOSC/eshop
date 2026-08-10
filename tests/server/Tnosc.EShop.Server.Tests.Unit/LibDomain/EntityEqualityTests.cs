// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC). All rights reserved.
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using Shouldly;
using Xunit;

namespace Tnosc.EShop.Server.Tests.Unit.LibDomain;

public sealed class EntityEqualityTests
{
    [Fact]
    public void Entities_Should_BeEqual_When_SameTypeAndSameId()
    {
        var id = TestEntityId.From(value: Guid.NewGuid());
        var first = new FirstTestEntity { Id = id };
        var second = new FirstTestEntity { Id = id };
        object secondAsObject = second;

        (first == second).ShouldBeTrue();
        first.Equals(other: second).ShouldBeTrue();
        first.Equals(obj: secondAsObject).ShouldBeTrue();
    }

    [Fact]
    public void Entities_Should_NotBeEqual_When_DifferentTypeButSameId()
    {
        var id = TestEntityId.From(value: Guid.NewGuid());
        var first = new FirstTestEntity { Id = id };
        var second = new SecondTestEntity { Id = id };
        object firstAsObject = first;
        object secondAsObject = second;

        second.Equals(obj: firstAsObject).ShouldBeFalse();
        first.Equals(obj: secondAsObject).ShouldBeFalse();
    }

    [Fact]
    public void Entities_Should_NotBeEqual_When_DifferentIds()
    {
        var first = new FirstTestEntity { Id = TestEntityId.From(value: Guid.NewGuid()) };
        var second = new FirstTestEntity { Id = TestEntityId.From(value: Guid.NewGuid()) };
        object secondAsObject = second;

        (first == second).ShouldBeFalse();
        first.Equals(other: second).ShouldBeFalse();
        first.Equals(obj: secondAsObject).ShouldBeFalse();
    }
}
