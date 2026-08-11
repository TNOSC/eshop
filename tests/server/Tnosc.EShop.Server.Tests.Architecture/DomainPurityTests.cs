// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
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
/// aggregates, entities, typed ids and value objects
/// keep the shape the domain layer's invariants depend on.
/// </summary>
public sealed class DomainPurityTests
{
    private const string AggregateRootOpenGeneric = "Tnosc.Lib.Domain.AggregateRoot`1";
    private const string EntityOpenGeneric = "Tnosc.Lib.Domain.Entity`1";
    private const string EntityIdInterface = "Tnosc.Lib.Domain.IEntityId";
    private const string EntityIdSelfValueInterface = "Tnosc.Lib.Domain.IEntityId`2";
    private const string ValueObjectBase = "Tnosc.Lib.Domain.ValueObjects.ValueObject";
    private const string ResultOpenGeneric = "Tnosc.Lib.Domain.Results.Result`1";

    private static readonly string[] ForbiddenCollectionTypes =
    [
        "System.Collections.Generic.List`1",
        "System.Collections.Generic.Dictionary`2",
        "System.Collections.Generic.HashSet`1",
        "System.Collections.Generic.ICollection`1",
    ];

    private static readonly Assembly[] ServerAssemblies =
    [
        Tnosc.EShop.Server.Domain.AssemblyReference.Assembly,
        Tnosc.EShop.Server.Application.AssemblyReference.Assembly,
        Tnosc.EShop.Server.Api.AssemblyReference.Assembly,
        Tnosc.EShop.Server.Infrastructure.Persistence.AssemblyReference.Assembly,
        Tnosc.EShop.Server.Infrastructure.External.AssemblyReference.Assembly,
        Tnosc.EShop.Server.Infrastructure.Job.AssemblyReference.Assembly,
    ];

    // Every type inheriting AggregateRoot<> or Entity<> lives under
    // Tnosc.EShop.Server.Domain.<Context>.
    [Fact]
    public void AggregatesAndEntities_Should_Live_Under_Domain_Context_Namespace()
    {
        List<string> violations = [];

        foreach (Assembly assembly in ServerAssemblies)
        {
            foreach (TypeDefinition type in CecilAssemblyLoader.LoadTypes(assembly: assembly))
            {
                if (!IsAggregateOrEntity(type: type))
                {
                    continue;
                }

                if (!IsUnderDomainContextNamespace(@namespace: type.Namespace))
                {
                    violations.Add(item: type.FullName);
                }
            }
        }

        violations.ShouldBeEmpty(customMessage: $"Aggregates and entities must live under Tnosc.EShop.Server.Domain.<Context>: {string.Join(separator: ", ", values: violations)}");
    }

    // Aggregates and entities expose no public property setters (init is allowed).
    [Fact]
    public void AggregatesAndEntities_Should_Not_Expose_Public_Setters()
    {
        List<string> violations = [];

        foreach (TypeDefinition type in CecilAssemblyLoader.LoadTypes(assembly: Tnosc.EShop.Server.Domain.AssemblyReference.Assembly))
        {
            if (!IsAggregateOrEntity(type: type))
            {
                continue;
            }

            foreach (PropertyDefinition property in type.Properties)
            {
                if (property.SetMethod is { IsPublic: true } setMethod && !setMethod.IsInitOnly())
                {
                    violations.Add(item: $"{type.FullName}.{property.Name}");
                }
            }
        }

        violations.ShouldBeEmpty(customMessage: $"Aggregates and entities must expose no public setters (init is allowed): {string.Join(separator: ", ", values: violations)}");
    }

    // Every IEntityId implementation is sealed, is a record, and implements
    // IEntityId<TSelf, TValue>.
    [Fact]
    public void EntityIds_Should_Be_Sealed_Records_Implementing_TypedEntityId()
    {
        List<string> violations = [];

        foreach (TypeDefinition type in CecilAssemblyLoader.LoadTypes(assembly: Tnosc.EShop.Server.Domain.AssemblyReference.Assembly))
        {
            if (type.IsInterface || !type.ImplementsInterface(interfaceFullName: EntityIdInterface))
            {
                continue;
            }

            if (!type.IsSealed)
            {
                violations.Add(item: $"{type.FullName} is not sealed");
            }

            if (!type.IsRecord())
            {
                violations.Add(item: $"{type.FullName} is not a record");
            }

            if (!type.ImplementsInterface(interfaceFullName: EntityIdSelfValueInterface))
            {
                violations.Add(item: $"{type.FullName} does not implement IEntityId<TSelf, TValue>");
            }
        }

        violations.ShouldBeEmpty(customMessage: $"Every IEntityId implementation must be a sealed record implementing IEntityId<TSelf, TValue>: {string.Join(separator: ", ", values: violations)}");
    }

    // Every ValueObject subtype is sealed and has no array/List/Dictionary/HashSet/
    // ICollection-typed property.
    [Fact]
    public void ValueObjects_Should_Be_Sealed_With_No_Mutable_Collection_Properties()
    {
        List<string> violations = [];

        foreach (TypeDefinition type in CecilAssemblyLoader.LoadTypes(assembly: Tnosc.EShop.Server.Domain.AssemblyReference.Assembly))
        {
            if (!type.InheritsOpenGeneric(openGenericFullName: ValueObjectBase))
            {
                continue;
            }

            if (!type.IsSealed)
            {
                violations.Add(item: $"{type.FullName} is not sealed");
            }

            foreach (PropertyDefinition property in type.Properties)
            {
                if (IsForbiddenCollectionType(propertyType: property.PropertyType))
                {
                    violations.Add(item: $"{type.FullName}.{property.Name} is a mutable reference collection ({property.PropertyType.FullName})");
                }
            }
        }

        violations.ShouldBeEmpty(customMessage: $"Value objects must be sealed and hold no mutable reference-collection properties: {string.Join(separator: ", ", values: violations)}");
    }

    // Every ValueObject subtype with a non-public constructor exposes a public static
    // method returning Result<TSelf>.
    [Fact]
    public void ValueObjects_With_NonPublic_Constructor_Should_Expose_Static_Result_Factory()
    {
        List<string> violations = [];

        foreach (TypeDefinition type in CecilAssemblyLoader.LoadTypes(assembly: Tnosc.EShop.Server.Domain.AssemblyReference.Assembly))
        {
            if (!type.InheritsOpenGeneric(openGenericFullName: ValueObjectBase))
            {
                continue;
            }

            bool hasNonPublicConstructor = type.Methods
                .Where(predicate: method => method.IsConstructor)
                .Any(predicate: ctor => !ctor.IsPublic && !ctor.IsStatic);

            if (!hasNonPublicConstructor)
            {
                continue;
            }

            bool hasResultFactory = type.Methods.Any(predicate: method =>
                method is { IsPublic: true, IsStatic: true } &&
                method.ReturnType is GenericInstanceType generic &&
                string.Equals(a: generic.ElementType.FullName, b: ResultOpenGeneric, comparisonType: StringComparison.Ordinal));

            if (!hasResultFactory)
            {
                violations.Add(item: type.FullName);
            }
        }

        violations.ShouldBeEmpty(customMessage: $"Value objects with a non-public constructor must expose a public static Result<TSelf> factory: {string.Join(separator: ", ", values: violations)}");
    }

    private static bool IsAggregateOrEntity(TypeDefinition type) =>
        type.InheritsOpenGeneric(openGenericFullName: AggregateRootOpenGeneric) || type.InheritsOpenGeneric(openGenericFullName: EntityOpenGeneric);

    private static bool IsUnderDomainContextNamespace(string? @namespace)
    {
        const string prefix = "Tnosc.EShop.Server.Domain.";

        return @namespace is not null &&
               @namespace.StartsWith(value: prefix, comparisonType: StringComparison.Ordinal) &&
               @namespace.Length > prefix.Length;
    }

    private static bool IsForbiddenCollectionType(TypeReference propertyType)
    {
        if (propertyType is ArrayType)
        {
            return true;
        }

        return propertyType is GenericInstanceType generic &&
               ForbiddenCollectionTypes.Contains(value: generic.ElementType.FullName);
    }
}
