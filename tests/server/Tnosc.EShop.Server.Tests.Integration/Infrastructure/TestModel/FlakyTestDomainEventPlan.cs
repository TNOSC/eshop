// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC). All rights reserved.
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System.Threading;

namespace Tnosc.EShop.Server.Tests.Integration.Infrastructure.TestModel;

/// <summary>
/// A process-wide singleton telling <see cref="FlakyTestDomainEventHandler"/> how many times to fail
/// before succeeding, and counting how many times it was actually invoked.
/// </summary>
/// <remarks>
/// The handler itself is scoped and re-resolved per delivery attempt — <c>DomainEventsPublisher</c>
/// opens a fresh scope per event, and the retry decorator sits above a single resolved instance —
/// so the attempt count cannot live on the handler if a test wants to read it afterwards. It lives
/// here instead, cleared per test alongside <see cref="TestDomainEventSpy"/>.
/// </remarks>
public sealed class FlakyTestDomainEventPlan
{
    private int _invocations;

    /// <summary>
    /// Gets or sets the number of invocations that fail before the handler starts succeeding.
    /// Defaults to zero, so the handler succeeds immediately unless a test says otherwise.
    /// </summary>
    public int FailuresBeforeSuccess { get; set; }

    /// <summary>
    /// Gets the number of times the handler has been invoked since the last <see cref="Reset"/>.
    /// </summary>
    public int Invocations => Volatile.Read(location: ref _invocations);

    /// <summary>
    /// Records an invocation and reports whether it should fail.
    /// </summary>
    /// <returns><see langword="true"/> when this invocation is one of the planned failures.</returns>
    public bool RecordAndShouldFail() =>
        Interlocked.Increment(location: ref _invocations) <= FailuresBeforeSuccess;

    /// <summary>
    /// Clears the plan and the invocation count. Called by <see cref="IntegrationTestBase"/> before
    /// each test, so state never leaks across tests despite the singleton lifetime.
    /// </summary>
    public void Reset()
    {
        FailuresBeforeSuccess = 0;
        Volatile.Write(location: ref _invocations, value: 0);
    }
}
