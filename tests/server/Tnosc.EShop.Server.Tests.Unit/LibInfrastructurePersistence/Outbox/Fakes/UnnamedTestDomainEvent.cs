// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using Tnosc.Lib.Domain;

namespace Tnosc.EShop.Server.Tests.Unit.LibInfrastructurePersistence.Outbox.Fakes;

internal sealed record UnnamedTestDomainEvent(Guid Id, DateTime OccurredOnUtc) : IDomainEvent;
