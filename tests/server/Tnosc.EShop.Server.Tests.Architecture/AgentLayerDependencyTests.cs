// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NetArchTest.Rules;
using Shouldly;

namespace Tnosc.EShop.Server.Tests.Architecture;

/// <summary>
/// The agent stack's layering, mechanised so a forbidden reference fails the build rather than
/// surviving review.
/// </summary>
public sealed class AgentLayerDependencyTests
{
    private static readonly Assembly LibAgentAssembly = Tnosc.Lib.Agent.AssemblyReference.Assembly;
    private static readonly Assembly LibAgentRuntimeAssembly = Tnosc.Lib.Agent.Runtime.AssemblyReference.Assembly;
    private static readonly Assembly AgentDomainAssembly = Tnosc.EShop.Agent.Domain.AssemblyReference.Assembly;
    private static readonly Assembly AgentApplicationAssembly = Tnosc.EShop.Agent.Application.AssemblyReference.Assembly;
    private static readonly Assembly AgentApiAssembly = Tnosc.EShop.Agent.Api.AssemblyReference.Assembly;

    /// <summary>
    /// The whole reason the agent framework is split across two projects: this one names no AI type,
    /// so a domain project can reference it and stay pure.
    /// </summary>
    /// <remarks>
    /// This is the load-bearing assertion of the agent stack. Without it, the first person who wants a
    /// tool or a chat client on an agent definition adds the package, everything still builds, and the
    /// purity that made definitions testable with nothing loaded is gone with no signal.
    /// </remarks>
    [Fact]
    public void LibAgent_Should_Not_Depend_On_AnyAiOrHostingFramework()
    {
        string[] forbidden =
        [
            "Microsoft.Extensions.AI",
            "Microsoft.Agents.AI",
            "ModelContextProtocol",
            "Microsoft.AspNetCore",
            "Azure",
            "System.ClientModel",
            "Microsoft.Extensions.DependencyInjection",
        ];

        TestResult result = Types.InAssembly(assembly: LibAgentAssembly)
            .Should()
            .NotHaveDependencyOnAny(dependencies: forbidden)
            .GetResult();

        AssertPasses(
            result: result,
            ruleDescription: "Tnosc.Lib.Agent must name no AI, MCP, Azure, ASP.NET Core or DI type — that is what lets a pure domain reference it");
    }

    // The eShop agent definitions carry policy, not plumbing: no AI framework, no ASP.NET Core, and
    // nothing from an outer layer of their own stack.
    [Fact]
    public void AgentDomain_Should_Not_Depend_On_OuterLayers()
    {
        string[] forbidden =
        [
            "Tnosc.EShop.Agent.Application",
            "Tnosc.EShop.Agent.Api",
            "Tnosc.EShop.Agent.Infrastructure",
            "Tnosc.EShop.Agent.Host",
            "Microsoft.Extensions.AI",
            "Microsoft.Agents.AI",
            "ModelContextProtocol",
            "Microsoft.AspNetCore",
            "Azure",
        ];

        TestResult result = Types.InAssembly(assembly: AgentDomainAssembly)
            .Should()
            .NotHaveDependencyOnAny(dependencies: forbidden)
            .GetResult();

        AssertPasses(
            result: result,
            ruleDescription: "Agent.Domain must not depend on Application, Api, Infrastructure, Host, an AI framework, MCP, ASP.NET Core or Azure");
    }

    // Application orchestrates. It may name the agent-framework vocabulary the runtime ports are
    // written in, but never a concrete provider, a transport, or the web stack.
    [Fact]
    public void AgentApplication_Should_Not_Depend_On_Infrastructure_Or_Web()
    {
        string[] forbidden =
        [
            "Tnosc.EShop.Agent.Infrastructure",
            "Tnosc.EShop.Agent.Api",
            "Tnosc.EShop.Agent.Host",
            "Microsoft.AspNetCore",
            "ModelContextProtocol",
            "Azure",
        ];

        TestResult result = Types.InAssembly(assembly: AgentApplicationAssembly)
            .Should()
            .NotHaveDependencyOnAny(dependencies: forbidden)
            .GetResult();

        AssertPasses(
            result: result,
            ruleDescription: "Agent.Application must not depend on Infrastructure, Api, Host, ASP.NET Core, MCP or Azure");
    }

    // The protocol surface talks to the application, never around it to a provider or a transport.
    [Fact]
    public void AgentApi_Should_Not_Depend_On_Infrastructure()
    {
        string[] forbidden =
        [
            "Tnosc.EShop.Agent.Infrastructure",
            "Tnosc.EShop.Agent.Host",
            "ModelContextProtocol",
            "Azure",
        ];

        TestResult result = Types.InAssembly(assembly: AgentApiAssembly)
            .Should()
            .NotHaveDependencyOnAny(dependencies: forbidden)
            .GetResult();

        AssertPasses(
            result: result,
            ruleDescription: "Agent.Api must not depend on Infrastructure, Host, MCP or Azure");
    }

    // The runtime ports are allowed the AI vocabulary — that is why they are a separate project —
    // but still no provider, transport or web stack.
    [Fact]
    public void LibAgentRuntime_Should_Not_Depend_On_AProviderOrTransport()
    {
        string[] forbidden =
        [
            "ModelContextProtocol",
            "Microsoft.AspNetCore",
            "Azure",
            "System.ClientModel",
        ];

        TestResult result = Types.InAssembly(assembly: LibAgentRuntimeAssembly)
            .Should()
            .NotHaveDependencyOnAny(dependencies: forbidden)
            .GetResult();

        AssertPasses(
            result: result,
            ruleDescription: "Tnosc.Lib.Agent.Runtime must name the AI abstractions only, never a concrete provider or transport");
    }

    private static void AssertPasses(TestResult result, string ruleDescription)
    {
        IEnumerable<string> offenders = result.FailingTypeNames ?? [];

        result.IsSuccessful.ShouldBeTrue(
            customMessage: $"{ruleDescription}. Offending types: {string.Join(separator: ", ", values: offenders)}");
    }
}
