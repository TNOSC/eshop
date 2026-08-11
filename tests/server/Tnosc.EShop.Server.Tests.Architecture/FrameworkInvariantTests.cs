// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC). All rights reserved.
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Mono.Cecil;
using Shouldly;
using Tnosc.EShop.Server.Tests.Architecture.Infrastructure;
using Tnosc.Lib.Domain;

namespace Tnosc.EShop.Server.Tests.Architecture;

/// <summary>
/// framework-level invariants that keep the decorator pipeline and the outbox's event-type 
/// registry correct.
/// </summary>
public sealed class FrameworkInvariantTests
{
    private const string DecoratorsNamespace = "Tnosc.Lib.Application.Decorators";
    private const string HandlerDecoratorInterface = "Tnosc.Lib.Application.Decorators.IHandlerDecorator";
    private const string CommandHandlerVoid = "Tnosc.Lib.Application.Commands.ICommandHandler`1";
    private const string CommandHandlerResult = "Tnosc.Lib.Application.Commands.ICommandHandler`2";
    private const string QueryHandler = "Tnosc.Lib.Application.Queries.IQueryHandler`2";
    private const string DomainEventHandler = "Tnosc.Lib.Application.DomainEvents.IDomainEventHandler`1";

    private static readonly Assembly[] AllAssemblies =
    [
        Tnosc.EShop.Server.Domain.AssemblyReference.Assembly,
        Tnosc.EShop.Server.Application.AssemblyReference.Assembly,
        Tnosc.EShop.Server.Api.AssemblyReference.Assembly,
        Tnosc.EShop.Server.Infrastructure.Persistence.AssemblyReference.Assembly,
        Tnosc.EShop.Server.Infrastructure.External.AssemblyReference.Assembly,
        Tnosc.EShop.Server.Infrastructure.Job.AssemblyReference.Assembly,
        typeof(IDomainEvent).Assembly, // Tnosc.Lib.Domain
        typeof(Tnosc.Lib.Application.Commands.ICommandHandler<>).Assembly, // Tnosc.Lib.Application
    ];

    // Every nested handler type under Tnosc.Lib.Application.Decorators implements
    // IHandlerDecorator — mechanically guards the B2 fix, so an eighth decorator that forgets
    // InnerHandler fails the build rather than silently disabling [Retry].
    [Fact]
    public void DecoratorNestedHandlers_Should_Implement_IHandlerDecorator()
    {
        Assembly libApplicationAssembly = typeof(Tnosc.Lib.Application.Commands.ICommandHandler<>).Assembly;

        List<string> violations = [];

        foreach (TypeDefinition outer in CecilAssemblyLoader.LoadTypes(assembly: libApplicationAssembly))
        {
            if (!string.Equals(a: outer.Namespace, b: DecoratorsNamespace, comparisonType: StringComparison.Ordinal))
            {
                continue;
            }

            foreach (TypeDefinition nested in outer.NestedTypes)
            {
                bool isHandler = nested.ImplementsInterface(interfaceFullName: CommandHandlerVoid) ||
                                  nested.ImplementsInterface(interfaceFullName: CommandHandlerResult) ||
                                  nested.ImplementsInterface(interfaceFullName: QueryHandler) ||
                                  nested.ImplementsInterface(interfaceFullName: DomainEventHandler);

                if (!isHandler)
                {
                    continue;
                }

                if (!nested.ImplementsInterface(interfaceFullName: HandlerDecoratorInterface))
                {
                    violations.Add(item: nested.FullName);
                }
            }
        }

        violations.ShouldBeEmpty(customMessage: $"Every nested handler under Tnosc.Lib.Application.Decorators must implement IHandlerDecorator: {string.Join(separator: ", ", values: violations)}");
    }

    // Domain-event names are unique — the compile-time counterpart of T4's startup
    // throw over the outbox's event-type registry.
    [Fact]
    public void DomainEventNames_Should_Be_Unique()
    {
        List<(string Name, Type Type)> domainEventNames = [.. AllAssemblies
            .SelectMany(selector: assembly => assembly.GetTypes())
            .Where(predicate: type => type is { IsAbstract: false, IsInterface: false } && typeof(IDomainEvent).IsAssignableFrom(c: type))
            .Select(selector: type => (Name: type.GetCustomAttribute<DomainEventNameAttribute>()?.Name ?? type.Name, Type: type))];

        List<IGrouping<string, (string Name, Type Type)>> duplicates = [.. domainEventNames
            .GroupBy(keySelector: entry => entry.Name, comparer: StringComparer.Ordinal)
            .Where(predicate: group => group.Count() > 1)];

        List<string> violations = [.. duplicates
            .Select(selector: group => $"'{group.Key}' used by {string.Join(separator: ", ", values: group.Select(selector: entry => entry.Type.FullName))}")];

        violations.ShouldBeEmpty(customMessage: $"Domain-event names must be unique: {string.Join(separator: "; ", values: violations)}");
    }
}
