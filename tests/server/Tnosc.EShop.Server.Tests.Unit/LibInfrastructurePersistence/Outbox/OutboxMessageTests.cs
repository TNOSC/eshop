// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC). All rights reserved.
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using Shouldly;
using Tnosc.Lib.Infrastructure.Persistence.Outbox;
using Xunit;

namespace Tnosc.EShop.Server.Tests.Unit.LibInfrastructurePersistence.Outbox;

public sealed class OutboxMessageTests
{
    [Fact]
    public void MarkFailed_Should_IncrementAttempts_And_SetNextAttemptOnUtc()
    {
        var message = new OutboxMessage(type: "test.event.v1", content: "{}");
        var firstAttempt = new DateTime(year: 2026, month: 1, day: 1, hour: 0, minute: 0, second: 0, kind: DateTimeKind.Utc);

        message.MarkFailed(error: "boom", nextAttemptOnUtc: firstAttempt);

        message.Attempts.ShouldBe(expected: 1);
        message.NextAttemptOnUtc.ShouldBe(expected: firstAttempt);
        message.Error.ShouldBe(expected: "boom");
        message.ProcessedOnUtc.ShouldBeNull();

        DateTime secondAttempt = firstAttempt.AddMinutes(value: 5);
        message.MarkFailed(error: "boom again", nextAttemptOnUtc: secondAttempt);

        message.Attempts.ShouldBe(expected: 2);
        message.NextAttemptOnUtc.ShouldBe(expected: secondAttempt);
        message.Error.ShouldBe(expected: "boom again");
    }

    [Fact]
    public void MarkProcessed_Should_ClearError()
    {
        var message = new OutboxMessage(type: "test.event.v1", content: "{}");
        message.MarkFailed(error: "boom", nextAttemptOnUtc: DateTime.UtcNow);

        var processedOnUtc = new DateTime(year: 2026, month: 1, day: 1, hour: 0, minute: 0, second: 0, kind: DateTimeKind.Utc);
        message.MarkProcessed(processedOnUtc: processedOnUtc);

        message.ProcessedOnUtc.ShouldBe(expected: processedOnUtc);
        message.Error.ShouldBeNull();
    }
}
