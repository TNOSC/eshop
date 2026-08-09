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
        var message = new OutboxMessage("test.event.v1", "{}");
        var firstAttempt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        message.MarkFailed("boom", firstAttempt);

        message.Attempts.ShouldBe(1);
        message.NextAttemptOnUtc.ShouldBe(firstAttempt);
        message.Error.ShouldBe("boom");
        message.ProcessedOnUtc.ShouldBeNull();

        DateTime secondAttempt = firstAttempt.AddMinutes(5);
        message.MarkFailed("boom again", secondAttempt);

        message.Attempts.ShouldBe(2);
        message.NextAttemptOnUtc.ShouldBe(secondAttempt);
        message.Error.ShouldBe("boom again");
    }

    [Fact]
    public void MarkProcessed_Should_ClearError()
    {
        var message = new OutboxMessage("test.event.v1", "{}");
        message.MarkFailed("boom", DateTime.UtcNow);

        var processedOnUtc = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        message.MarkProcessed(processedOnUtc);

        message.ProcessedOnUtc.ShouldBe(processedOnUtc);
        message.Error.ShouldBeNull();
    }
}
