// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC). All rights reserved.
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;
using Tnosc.Lib.Application.Attributes;
using Tnosc.Lib.Application.Commands;
using Tnosc.Lib.Application.Exceptions;
using Tnosc.Lib.Domain.Results;

namespace Tnosc.EShop.Server.Tests.Unit.LibApplication.Fakes;

/// <summary>
/// A fake handler decorated with all three attributes the B2 regression must still discover
/// once wrapped six layers deep by <c>AddApplication</c>'s command pipeline. Fails its first
/// three invocations with a retriable exception, so only <see cref="RetryAttribute"/>'s
/// configured five attempts (not the two-level-shallower default of three) can succeed.
/// </summary>
[Transactional]
[Retry(5)]
[CacheTag("catalog")]
internal sealed class FakeCommandHandler(CallCounter counter) : ICommandHandler<FakeCommand, string>
{
    public ValueTask<Result<string>> HandleAsync(FakeCommand command, CancellationToken cancellationToken = default)
    {
        counter.Count++;

        if (counter.Count < 4)
        {
            throw new TransientFailureException("Transient failure.", null, null);
        }

        return ValueTask.FromResult<Result<string>>("ok");
    }
}
