// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NetArchTest.Rules;
using Shouldly;
using Tnosc.Lib.Api.Abstractions;
using Tnosc.Lib.Application.Commands;
using Tnosc.Lib.Domain;

namespace Tnosc.EShop.Server.Tests.Architecture;

/// <summary>
/// Which layers and bounded contexts are allowed to reference which, mechanised with NetArchTest so a forbidden reference fails the
/// build instead of surviving code review.
/// </summary>
public sealed class LayerDependencyTests
{
    private static readonly Assembly DomainAssembly = Tnosc.EShop.Server.Domain.AssemblyReference.Assembly;
    private static readonly Assembly ApplicationAssembly = Tnosc.EShop.Server.Application.AssemblyReference.Assembly;
    private static readonly Assembly ApiAssembly = Tnosc.EShop.Server.Api.AssemblyReference.Assembly;
    private static readonly Assembly PersistenceAssembly = Tnosc.EShop.Server.Infrastructure.Persistence.AssemblyReference.Assembly;
    private static readonly Assembly ExternalAssembly = Tnosc.EShop.Server.Infrastructure.External.AssemblyReference.Assembly;
    private static readonly Assembly JobAssembly = Tnosc.EShop.Server.Infrastructure.Job.AssemblyReference.Assembly;
    private static readonly Assembly SharedAssembly = Tnosc.EShop.Server.Shared.AssemblyReference.Assembly;

    private static readonly Assembly LibDomainAssembly = typeof(IEntityId).Assembly;
    private static readonly Assembly LibApplicationAssembly = typeof(ICommandHandler<>).Assembly;
    private static readonly Assembly LibApiAssembly = typeof(IApiEndpoint).Assembly;
    private static readonly Assembly LibHostAssembly = Assembly.Load(assemblyString: "Tnosc.Lib.Host");

    private static readonly string[] EfCorePatterns = ["Microsoft.EntityFrameworkCore"];

    private static readonly string[] Contexts = ["Catalog", "Basket", "Ordering", "Payment", "Identity"];

    // Server.Domain depends on none of: Server.Application, Server.Api,
    // Server.Infrastructure.*, Server.Host, EF Core, ASP.NET Core, Npgsql, System.Data.
    [Fact]
    public void Domain_Should_Not_Depend_On_OuterLayers()
    {
        string[] forbidden =
        [
            "Tnosc.EShop.Server.Application",
            "Tnosc.EShop.Server.Api",
            "Tnosc.EShop.Server.Infrastructure",
            "Tnosc.EShop.Server.Host",
            "Microsoft.EntityFrameworkCore",
            "Microsoft.AspNetCore",
            "Npgsql",
            "System.Data",
        ];

        TestResult result = Types.InAssembly(assembly: DomainAssembly)
            .Should()
            .NotHaveDependencyOnAny(dependencies: forbidden)
            .GetResult();

        AssertPasses(result: result, ruleDescription: "Server.Domain must not depend on Application, Api, Infrastructure, Host, EF Core, ASP.NET Core, Npgsql or System.Data");
    }

    // Server.Application depends on none of: Server.Infrastructure.*, Server.Api,
    // Server.Host, EF Core, ASP.NET Core.
    [Fact]
    public void Application_Should_Not_Depend_On_Infrastructure_Api_Host_Or_Web_Frameworks()
    {
        string[] forbidden =
        [
            "Tnosc.EShop.Server.Infrastructure",
            "Tnosc.EShop.Server.Api",
            "Tnosc.EShop.Server.Host",
            "Microsoft.EntityFrameworkCore",
            "Microsoft.AspNetCore",
        ];

        TestResult result = Types.InAssembly(assembly: ApplicationAssembly)
            .Should()
            .NotHaveDependencyOnAny(dependencies: forbidden)
            .GetResult();

        AssertPasses(result: result, ruleDescription: "Server.Application must not depend on Infrastructure, Api, Host, EF Core or ASP.NET Core");
    }

    // Server.Api does not depend on Server.Infrastructure.* — endpoints see only
    // Application contracts.
    [Fact]
    public void Api_Should_Not_Depend_On_Infrastructure()
    {
        TestResult result = Types.InAssembly(assembly: ApiAssembly)
            .Should()
            .NotHaveDependencyOnAny(dependencies: "Tnosc.EShop.Server.Infrastructure")
            .GetResult();

        AssertPasses(result: result, ruleDescription: "Server.Api must not depend on Server.Infrastructure.*");
    }

    // Server.Infrastructure.Persistence does not depend on Server.Api, Server.Host,
    // Server.Infrastructure.External.
    [Fact]
    public void Persistence_Should_Not_Depend_On_Api_Host_Or_External()
    {
        string[] forbidden =
        [
            "Tnosc.EShop.Server.Api",
            "Tnosc.EShop.Server.Host",
            "Tnosc.EShop.Server.Infrastructure.External",
        ];

        TestResult result = Types.InAssembly(assembly: PersistenceAssembly)
            .Should()
            .NotHaveDependencyOnAny(dependencies: forbidden)
            .GetResult();

        AssertPasses(result: result, ruleDescription: "Server.Infrastructure.Persistence must not depend on Api, Host or Infrastructure.External");
    }

    // No assembly outside Server.Infrastructure.Persistence and
    // Lib.Infrastructure.Persistence references EF Core — the mechanical form of "keep EF Core
    // hidden behind repository contracts".
    [Fact]
    public void Only_Persistence_Assemblies_Should_Depend_On_EfCore()
    {
        (string Name, Assembly Assembly)[] nonPersistenceAssemblies =
        [
            ("Server.Domain", DomainAssembly),
            ("Server.Application", ApplicationAssembly),
            ("Server.Api", ApiAssembly),
            ("Server.Infrastructure.External", ExternalAssembly),
            ("Server.Infrastructure.Job", JobAssembly),
            ("Server.Shared", SharedAssembly),
            ("Lib.Domain", LibDomainAssembly),
            ("Lib.Application", LibApplicationAssembly),
            ("Lib.Api", LibApiAssembly),
            ("Lib.Host", LibHostAssembly),
        ];

        List<string> violations = [];

        foreach ((string name, Assembly assembly) in nonPersistenceAssemblies)
        {
            TestResult result = Types.InAssembly(assembly: assembly)
                .Should()
                .NotHaveDependencyOnAny(dependencies: EfCorePatterns)
                .GetResult();

            if (!result.IsSuccessful)
            {
                violations.AddRange(collection: (result.FailingTypeNames ?? []).Select(selector: t => $"{name}: {t}"));
            }
        }

        violations.ShouldBeEmpty(customMessage: $"EF Core must stay hidden behind repository contracts, but these types reference it directly: {string.Join(separator: ", ", values: violations)}");
    }

    // Bounded-context isolation — types under "...Catalog" don't depend on
    // "...Basket"/"...Ordering"/"...Payment"/"...Identity", pairwise across all five contexts and
    // all layers.
    [Fact]
    public void Contexts_Should_Not_Depend_On_Each_Other()
    {
        (string Layer, Assembly Assembly)[] layers =
        [
            ("Server.Domain", DomainAssembly),
            ("Server.Application", ApplicationAssembly),
            ("Server.Api", ApiAssembly),
            ("Server.Infrastructure.Persistence", PersistenceAssembly),
            ("Server.Infrastructure.External", ExternalAssembly),
            ("Server.Infrastructure.Job", JobAssembly),
        ];

        List<string> violations = [];

        foreach ((string layer, Assembly assembly) in layers)
        {
            foreach (string context in Contexts)
            {
                foreach (string otherContext in Contexts)
                {
                    if (string.Equals(a: context, b: otherContext, comparisonType: StringComparison.Ordinal))
                    {
                        continue;
                    }

                    TestResult result = Types.InAssembly(assembly: assembly)
                        .That()
                        .ResideInNamespaceContaining(name: $".{context}")
                        .Should()
                        .NotHaveDependencyOnAny(dependencies: $".{otherContext}")
                        .GetResult();

                    if (!result.IsSuccessful)
                    {
                        violations.AddRange(collection: (result.FailingTypeNames ?? [])
                            .Select(selector: t => $"{layer}: {t} ({context} depends on {otherContext})"));
                    }
                }
            }
        }

        violations.ShouldBeEmpty(customMessage: $"Bounded contexts must stay isolated, but found cross-context dependencies: {string.Join(separator: ", ", values: violations)}");
    }

    private static void AssertPasses(TestResult result, string ruleDescription)
    {
        string offenders = string.Join(separator: ", ", values: result.FailingTypeNames ?? []);
        result.IsSuccessful.ShouldBeTrue(customMessage: $"{ruleDescription}. Offending type(s): {offenders}");
    }
}
