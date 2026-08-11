// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System.Threading;

namespace Tnosc.EShop.Server.Tests.Integration.Infrastructure.TestModel;

/// <summary>
/// A process-wide singleton controlling whether <see cref="FanOutFailingHandler"/> still fails, and
/// counting how often each fan-out handler ran.
/// </summary>
/// <remarks>
/// Both handlers are scoped and re-resolved per delivery, so the counts cannot live on them. Letting
/// a test flip <see cref="FailingHandlerShouldFail"/> to <see langword="false"/> is what makes a
/// realistic replay possible: fix the cause, then replay the dead letter.
/// </remarks>
public sealed class FanOutTestPlan
{
    private int _failingHandlerInvocations;
    private int _succeedingHandlerInvocations;

    /// <summary>
    /// Gets or sets a value indicating whether <see cref="FanOutFailingHandler"/> throws. Defaults to
    /// <see langword="true"/>.
    /// </summary>
    public bool FailingHandlerShouldFail { get; set; } = true;

    /// <summary>
    /// Gets how many times the failing handler has been invoked since the last <see cref="Reset"/>.
    /// </summary>
    public int FailingHandlerInvocations => Volatile.Read(location: ref _failingHandlerInvocations);

    /// <summary>
    /// Gets how many times the succeeding handler has been invoked since the last <see cref="Reset"/>.
    /// </summary>
    public int SucceedingHandlerInvocations => Volatile.Read(location: ref _succeedingHandlerInvocations);

    /// <summary>
    /// Records an invocation of the failing handler and reports whether it should throw.
    /// </summary>
    /// <returns><see langword="true"/> when the handler should throw.</returns>
    public bool RecordFailingInvocation()
    {
        Interlocked.Increment(location: ref _failingHandlerInvocations);

        return FailingHandlerShouldFail;
    }

    /// <summary>
    /// Records an invocation of the succeeding handler.
    /// </summary>
    public void RecordSucceedingInvocation() => Interlocked.Increment(location: ref _succeedingHandlerInvocations);

    /// <summary>
    /// Restores the defaults and clears both counts. Called by <see cref="IntegrationTestBase"/>
    /// before each test.
    /// </summary>
    public void Reset()
    {
        FailingHandlerShouldFail = true;
        Volatile.Write(location: ref _failingHandlerInvocations, value: 0);
        Volatile.Write(location: ref _succeedingHandlerInvocations, value: 0);
    }
}
