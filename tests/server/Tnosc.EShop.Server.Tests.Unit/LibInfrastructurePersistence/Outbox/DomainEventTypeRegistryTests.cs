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
        var registry = new DomainEventTypeRegistry(assemblies: FixtureAssembly);

        registry.GetName(domainEventType: typeof(NamedTestDomainEvent)).ShouldBe(expected: "test.registry.named-event.v1");
    }

    [Fact]
    public void GetName_Should_FallBackToTypeName_When_DomainEventNameAttributeIsAbsent()
    {
        var registry = new DomainEventTypeRegistry(assemblies: FixtureAssembly);

        registry.GetName(domainEventType: typeof(UnnamedTestDomainEvent)).ShouldBe(expected: nameof(UnnamedTestDomainEvent));
    }

    [Fact]
    public void TryResolve_Should_RoundTrip_ForARegisteredName()
    {
        var registry = new DomainEventTypeRegistry(assemblies: FixtureAssembly);
        string name = registry.GetName(domainEventType: typeof(NamedTestDomainEvent));

        bool resolved = registry.TryResolve(name: name, domainEventType: out Type? domainEventType);

        resolved.ShouldBeTrue();
        domainEventType.ShouldBe(expected: typeof(NamedTestDomainEvent));
    }

    [Fact]
    public void TryResolve_Should_ReturnFalse_When_NameIsUnknown()
    {
        var registry = new DomainEventTypeRegistry(assemblies: FixtureAssembly);

        registry.TryResolve(name: "nothing.registered.v1", domainEventType: out Type? domainEventType).ShouldBeFalse();
        domainEventType.ShouldBeNull();
    }

    [Fact]
    public void Constructor_Should_Throw_When_TwoDomainEventTypesShareTheSameContractName()
    {
        Assembly duplicateAssembly = DuplicateDomainEventAssemblyFactory.Build(sharedName: "test.duplicate.v1");

        Action act = () => _ = new DomainEventTypeRegistry(assemblies: duplicateAssembly);

        InvalidOperationException exception = Should.Throw<InvalidOperationException>(actual: act);
        exception.Message.ShouldContain(expected: "test.duplicate.v1");
        exception.Message.ShouldContain(expected: "FirstDuplicateDomainEvent");
        exception.Message.ShouldContain(expected: "SecondDuplicateDomainEvent");
    }
}
