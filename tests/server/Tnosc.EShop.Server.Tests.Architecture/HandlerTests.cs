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

namespace Tnosc.EShop.Server.Tests.Architecture;

/// <summary>
/// command handlers, query handlers, domain-event handlers and endpoints 
/// keep the shape and placement the decorator pipeline and
/// the CQRS split depend on.
/// </summary>
public sealed class HandlerTests
{
    private const string CommandHandlerVoid = "Tnosc.Lib.Application.Commands.ICommandHandler`1";
    private const string CommandHandlerResult = "Tnosc.Lib.Application.Commands.ICommandHandler`2";
    private const string QueryHandler = "Tnosc.Lib.Application.Queries.IQueryHandler`2";
    private const string DomainEventHandler = "Tnosc.Lib.Application.DomainEvents.IDomainEventHandler`1";
    private const string ApiEndpoint = "Tnosc.Lib.Api.Abstractions.IApiEndpoint";

    private static readonly Assembly ApplicationAssembly = Tnosc.EShop.Server.Application.AssemblyReference.Assembly;
    private static readonly Assembly PersistenceAssembly = Tnosc.EShop.Server.Infrastructure.Persistence.AssemblyReference.Assembly;
    private static readonly Assembly ApiAssembly = Tnosc.EShop.Server.Api.AssemblyReference.Assembly;

    private static readonly Assembly[] ServerAssemblies =
    [
        Tnosc.EShop.Server.Domain.AssemblyReference.Assembly,
        ApplicationAssembly,
        ApiAssembly,
        PersistenceAssembly,
        Tnosc.EShop.Server.Infrastructure.External.AssemblyReference.Assembly,
        Tnosc.EShop.Server.Infrastructure.Job.AssemblyReference.Assembly,
    ];

    // Types implementing ICommandHandler<>, ICommandHandler<,> or IQueryHandler<,> are sealed.
    [Fact]
    public void Handlers_Should_Be_Sealed()
    {
        List<string> violations = [];

        foreach (Assembly assembly in ServerAssemblies)
        {
            foreach (TypeDefinition type in CecilAssemblyLoader.LoadTypes(assembly: assembly))
            {
                if (!IsCommandOrQueryHandler(type: type))
                {
                    continue;
                }

                if (!type.IsSealed)
                {
                    violations.Add(item: type.FullName);
                }
            }
        }

        violations.ShouldBeEmpty(customMessage: $"Command/query handlers must be sealed: {string.Join(separator: ", ", values: violations)}");
    }

    // Command handlers live in Server.Application, named *CommandHandler.
    [Fact]
    public void CommandHandlers_Should_Live_In_Application_And_Be_Named_CommandHandler()
    {
        List<string> violations = [];

        foreach (Assembly assembly in ServerAssemblies)
        {
            foreach (TypeDefinition type in CecilAssemblyLoader.LoadTypes(assembly: assembly))
            {
                bool isCommandHandler = type.ImplementsInterface(interfaceFullName: CommandHandlerVoid) || type.ImplementsInterface(interfaceFullName: CommandHandlerResult);

                if (!isCommandHandler)
                {
                    continue;
                }

                if (assembly != ApplicationAssembly)
                {
                    violations.Add(item: $"{type.FullName} does not live in Server.Application");
                }

                if (!type.Name.EndsWith(value: "CommandHandler", comparisonType: StringComparison.Ordinal))
                {
                    violations.Add(item: $"{type.FullName} is not named *CommandHandler");
                }
            }
        }

        violations.ShouldBeEmpty(customMessage: $"Command handlers must live in Server.Application and be named *CommandHandler: {string.Join(separator: ", ", values: violations)}");
    }

    // Query handlers live in Server.Infrastructure.Persistence, named *QueryHandler.
    [Fact]
    public void QueryHandlers_Should_Live_In_Persistence_And_Be_Named_QueryHandler()
    {
        List<string> violations = [];

        foreach (Assembly assembly in ServerAssemblies)
        {
            foreach (TypeDefinition type in CecilAssemblyLoader.LoadTypes(assembly: assembly))
            {
                if (!type.ImplementsInterface(interfaceFullName: QueryHandler))
                {
                    continue;
                }

                if (assembly != PersistenceAssembly)
                {
                    violations.Add(item: $"{type.FullName} does not live in Server.Infrastructure.Persistence");
                }

                if (!type.Name.EndsWith(value: "QueryHandler", comparisonType: StringComparison.Ordinal))
                {
                    violations.Add(item: $"{type.FullName} is not named *QueryHandler");
                }
            }
        }

        violations.ShouldBeEmpty(customMessage: $"Query handlers must live in Server.Infrastructure.Persistence and be named *QueryHandler: {string.Join(separator: ", ", values: violations)}");
    }

    // Command handlers reference no DbContext-derived type — commands go through
    // repository contracts.
    [Fact]
    public void CommandHandlers_Should_Not_Reference_DbContext()
    {
        List<string> violations = [];

        foreach (TypeDefinition type in CecilAssemblyLoader.LoadTypes(assembly: ApplicationAssembly))
        {
            bool isCommandHandler = type.ImplementsInterface(interfaceFullName: CommandHandlerVoid) || type.ImplementsInterface(interfaceFullName: CommandHandlerResult);

            if (!isCommandHandler)
            {
                continue;
            }

            if (type.ReferencesTypeMatching(predicate: IsDbContextType))
            {
                violations.Add(item: type.FullName);
            }
        }

        violations.ShouldBeEmpty(customMessage: $"Command handlers must go through repository contracts, not DbContext directly: {string.Join(separator: ", ", values: violations)}");
    }

    // Query handlers reference no I*Repository — reads never go through the write model.
    [Fact]
    public void QueryHandlers_Should_Not_Reference_Repositories()
    {
        List<string> violations = [];

        foreach (TypeDefinition type in CecilAssemblyLoader.LoadTypes(assembly: PersistenceAssembly))
        {
            if (!type.ImplementsInterface(interfaceFullName: QueryHandler))
            {
                continue;
            }

            if (type.ReferencesTypeMatching(predicate: IsRepositoryInterface))
            {
                violations.Add(item: type.FullName);
            }
        }

        violations.ShouldBeEmpty(customMessage: $"Query handlers must not reference I*Repository — reads never go through the write model: {string.Join(separator: ", ", values: violations)}");
    }

    // IDomainEventHandler<> implementations are sealed, named *DomainEventHandler.
    [Fact]
    public void DomainEventHandlers_Should_Be_Sealed_And_Named_DomainEventHandler()
    {
        List<string> violations = [];

        foreach (Assembly assembly in ServerAssemblies)
        {
            foreach (TypeDefinition type in CecilAssemblyLoader.LoadTypes(assembly: assembly))
            {
                if (!type.ImplementsInterface(interfaceFullName: DomainEventHandler))
                {
                    continue;
                }

                if (!type.IsSealed)
                {
                    violations.Add(item: $"{type.FullName} is not sealed");
                }

                if (!type.Name.EndsWith(value: "DomainEventHandler", comparisonType: StringComparison.Ordinal))
                {
                    violations.Add(item: $"{type.FullName} is not named *DomainEventHandler");
                }
            }
        }

        violations.ShouldBeEmpty(customMessage: $"Domain-event handlers must be sealed and named *DomainEventHandler: {string.Join(separator: ", ", values: violations)}");
    }

    // IApiEndpoint implementations are sealed, internal, and live in Server.Api.
    [Fact]
    public void Endpoints_Should_Be_Sealed_Internal_And_Live_In_Api()
    {
        List<string> violations = [];

        foreach (Assembly assembly in ServerAssemblies)
        {
            foreach (TypeDefinition type in CecilAssemblyLoader.LoadTypes(assembly: assembly))
            {
                if (!type.ImplementsInterface(interfaceFullName: ApiEndpoint))
                {
                    continue;
                }

                if (!type.IsSealed)
                {
                    violations.Add(item: $"{type.FullName} is not sealed");
                }

                if (type.IsPublic)
                {
                    violations.Add(item: $"{type.FullName} is not internal");
                }

                if (assembly != ApiAssembly)
                {
                    violations.Add(item: $"{type.FullName} does not live in Server.Api");
                }
            }
        }

        violations.ShouldBeEmpty(customMessage: $"Endpoints must be sealed, internal, and live in Server.Api: {string.Join(separator: ", ", values: violations)}");
    }

    // No type in Server.Application implements IApiEndpoint.
    [Fact]
    public void Application_Should_Not_Implement_IApiEndpoint()
    {
        List<string> violations = [.. CecilAssemblyLoader.LoadTypes(assembly: ApplicationAssembly)
            .Where(predicate: type => type.ImplementsInterface(interfaceFullName: ApiEndpoint))
            .Select(selector: type => type.FullName)];

        violations.ShouldBeEmpty(customMessage: $"Server.Application must not implement IApiEndpoint: {string.Join(separator: ", ", values: violations)}");
    }

    private static bool IsCommandOrQueryHandler(TypeDefinition type) =>
        type.ImplementsInterface(interfaceFullName: CommandHandlerVoid) ||
        type.ImplementsInterface(interfaceFullName: CommandHandlerResult) ||
        type.ImplementsInterface(interfaceFullName: QueryHandler);

    private static bool IsDbContextType(TypeReference typeReference) =>
        string.Equals(a: typeReference.FullName, b: "Microsoft.EntityFrameworkCore.DbContext", comparisonType: StringComparison.Ordinal) ||
        typeReference.Name.EndsWith(value: "DbContext", comparisonType: StringComparison.Ordinal);

    private static bool IsRepositoryInterface(TypeReference typeReference) =>
        typeReference.Name.Length > 1 &&
        typeReference.Name[0] == 'I' &&
        char.IsUpper(c: typeReference.Name[1]) &&
        typeReference.Name.EndsWith(value: "Repository", comparisonType: StringComparison.Ordinal);
}
