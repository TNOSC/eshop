// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC). All rights reserved.
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using Tnosc.Lib.Domain;

namespace Tnosc.EShop.Server.Tests.Unit.LibDomain;

internal sealed record TestEntityId : GuidEntityId, IEntityId<TestEntityId, Guid>
{
    private TestEntityId(Guid value)
        : base(value)
    {
    }

    public static TestEntityId From(Guid value)
        => new TestEntityId(value);
}
