// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC). All rights reserved.
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using System.Reflection;
using Shouldly;
using Tnosc.EShop.Server.Tests.Unit.LibInfrastructurePersistence.Outbox.Fakes;
using Tnosc.Lib.Infrastructure.Persistence.Outbox;
using Xunit;

namespace Tnosc.EShop.Server.Tests.Unit.LibInfrastructurePersistence.Outbox;

public sealed class DomainEventTypeRegistryTests
{
    private static readonly Assembly FixtureAssembly = typeof(NamedTestDomainEvent).Assembly;

    [Fact]
    public void GetName_Should_UseAttributeName_When_DomainEventNameAttributeIsPresent()
    {
        var registry = new DomainEventTypeRegistry(FixtureAssembly);

        registry.GetName(typeof(NamedTestDomainEvent)).ShouldBe("test.registry.named-event.v1");
    }

    [Fact]
    public void GetName_Should_FallBackToTypeName_When_DomainEventNameAttributeIsAbsent()
    {
        var registry = new DomainEventTypeRegistry(FixtureAssembly);

        registry.GetName(typeof(UnnamedTestDomainEvent)).ShouldBe(nameof(UnnamedTestDomainEvent));
    }

    [Fact]
    public void TryResolve_Should_RoundTrip_ForARegisteredName()
    {
        var registry = new DomainEventTypeRegistry(FixtureAssembly);
        string name = registry.GetName(typeof(NamedTestDomainEvent));

        bool resolved = registry.TryResolve(name, out Type? domainEventType);

        resolved.ShouldBeTrue();
        domainEventType.ShouldBe(typeof(NamedTestDomainEvent));
    }

    [Fact]
    public void TryResolve_Should_ReturnFalse_When_NameIsUnknown()
    {
        var registry = new DomainEventTypeRegistry(FixtureAssembly);

        registry.TryResolve("nothing.registered.v1", out Type? domainEventType).ShouldBeFalse();
        domainEventType.ShouldBeNull();
    }

    [Fact]
    public void Constructor_Should_Throw_When_TwoDomainEventTypesShareTheSameContractName()
    {
        Assembly duplicateAssembly = DuplicateDomainEventAssemblyFactory.Build("test.duplicate.v1");

        Action act = () => _ = new DomainEventTypeRegistry(duplicateAssembly);

        InvalidOperationException exception = Should.Throw<InvalidOperationException>(act);
        exception.Message.ShouldContain("test.duplicate.v1");
        exception.Message.ShouldContain("FirstDuplicateDomainEvent");
        exception.Message.ShouldContain("SecondDuplicateDomainEvent");
    }
}
