// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC). All rights reserved.
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;
using Tnosc.Lib.Application.Attributes;
using Tnosc.Lib.Application.Queries;
using Tnosc.Lib.Domain.Results;

namespace Tnosc.EShop.Server.Tests.Unit.LibApplication.Fakes;

/// <summary>
/// A fake query handler marked <see cref="CacheableAttribute"/>. In the query pipeline
/// <c>Cacheable</c> wraps <c>Retry</c> which wraps this handler, so discovering the attribute
/// requires unwrapping one level of nested decorator.
/// </summary>
[Cacheable(60)]
internal sealed class FakeQueryHandler(CallCounter counter) : IQueryHandler<FakeQuery, int>
{
    public ValueTask<Result<int>> HandleAsync(FakeQuery query, CancellationToken cancellationToken = default)
    {
        counter.Count++;
        return ValueTask.FromResult<Result<int>>(counter.Count);
    }
}
