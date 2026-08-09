// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC). All rights reserved.
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using Tnosc.Lib.Application.Attributes;
using Tnosc.Lib.Application.Queries;

namespace Tnosc.EShop.Server.Tests.Unit.LibApplication.Fakes;

internal sealed record FakeQuery([property: CacheKey] int Key) : IQuery<int>;
