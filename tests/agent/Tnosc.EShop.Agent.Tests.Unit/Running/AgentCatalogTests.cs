// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Tnosc.EShop.Agent.Application.Catalog;
using Tnosc.EShop.Agent.Application.Extensions;
using Tnosc.EShop.Agent.Domain.Agents;
using Tnosc.EShop.Agent.Tests.Unit.Infrastructure;
using Tnosc.Lib.Agent.Definitions;
using Xunit;

namespace Tnosc.EShop.Agent.Tests.Unit.Running;

/// <summary>
/// The catalogue is how a name becomes an agent, and how a duplicate name fails loudly.
/// </summary>
public sealed class AgentCatalogTests
{
    [Fact]
    public void TryGet_Should_Find_ARegisteredAgent()
    {
        // Arrange
        AgentCatalog catalog = CatalogOf(AgentTestFactory.Definition(name: "first"));

        // Act
        bool found = catalog.TryGet(agentName: "first", definition: out AgentDefinition? definition);

        // Assert
        found.ShouldBeTrue();
        definition!.Name.Value.ShouldBe(expected: "first");
    }

    [Fact]
    public void TryGet_Should_Miss_AnUnknownName()
    {
        // Arrange
        AgentCatalog catalog = CatalogOf(AgentTestFactory.Definition(name: "first"));

        // Act
        bool found = catalog.TryGet(agentName: "second", definition: out AgentDefinition? definition);

        // Assert
        found.ShouldBeFalse();
        definition.ShouldBeNull();
    }

    [Fact]
    public void TryGet_Should_Be_CaseSensitive()
    {
        // Names are matched ordinally because the same literal is also a route segment and a DI key.

        // Arrange
        AgentCatalog catalog = CatalogOf(AgentTestFactory.Definition(name: "first"));

        // Act & Assert
        catalog.TryGet(agentName: "First", definition: out _).ShouldBeFalse();
    }

    [Fact]
    public void Construction_Should_Throw_When_TwoAgentsShareAName()
    {
        // Fatal at startup on purpose: with a duplicate, one of the two would silently never be
        // reachable, and discovering that from a 404 later costs far more than refusing to start.

        // Act
        Action construct = () => CatalogOf(
            AgentTestFactory.Definition(name: "same"),
            AgentTestFactory.Definition(name: "same"));

        // Assert
        construct.ShouldThrow<InvalidOperationException>()
            .Message.ShouldContain(expected: "same");
    }

    [Fact]
    public void AddAgentApplication_Should_Discover_TheShippedAgents()
    {
        // The scan is the registration mechanism; if it stops finding agents, nothing else reports it.

        // Arrange
        ServiceCollection services = new();
        services.AddLogging();
        services.AddAgentApplication();

        using ServiceProvider provider = services.BuildServiceProvider();

        // Act
        IAgentCatalog catalog = provider.GetRequiredService<IAgentCatalog>();

        // Assert
        catalog.TryGet(agentName: AgentNames.ShoppingAssistant, definition: out _).ShouldBeTrue();
    }

    private static AgentCatalog CatalogOf(params AgentDefinition[] definitions)
    {
        List<IAgentDefinitionProvider> providers = [];

        foreach (AgentDefinition definition in definitions)
        {
            providers.Add(item: new StubProvider(definition: definition));
        }

        return new AgentCatalog(providers: providers);
    }

    private sealed class StubProvider(AgentDefinition definition) : IAgentDefinitionProvider
    {
        public AgentDefinition Definition => definition;
    }
}
