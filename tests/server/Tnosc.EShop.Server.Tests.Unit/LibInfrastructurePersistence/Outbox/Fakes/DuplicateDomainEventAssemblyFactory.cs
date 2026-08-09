// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC). All rights reserved.
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using System.Reflection;
using System.Reflection.Emit;
using Tnosc.Lib.Domain;

namespace Tnosc.EShop.Server.Tests.Unit.LibInfrastructurePersistence.Outbox.Fakes;

/// <summary>
/// Builds a throwaway, in-memory assembly containing two distinct <see cref="IDomainEvent"/>
/// implementors that both declare the same <see cref="DomainEventNameAttribute"/> name.
/// </summary>
/// <remarks>
/// The duplicate-name pair must live in a dedicated assembly so the duplicate-key test does not
/// poison every other test that scans <c>typeof(SomeFixture).Assembly</c> for domain events.
/// </remarks>
internal static class DuplicateDomainEventAssemblyFactory
{
    public static Assembly Build(string sharedName)
    {
        var assemblyName = new AssemblyName($"Tnosc.Tests.DuplicateDomainEvents.{Guid.NewGuid():N}");
        var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
        ModuleBuilder moduleBuilder = assemblyBuilder.DefineDynamicModule(assemblyName.Name!);

        DefineDomainEventType(moduleBuilder, "FirstDuplicateDomainEvent", sharedName);
        DefineDomainEventType(moduleBuilder, "SecondDuplicateDomainEvent", sharedName);

        return assemblyBuilder;
    }

    private static void DefineDomainEventType(ModuleBuilder moduleBuilder, string typeName, string domainEventName)
    {
        TypeBuilder typeBuilder = moduleBuilder.DefineType(
            typeName,
            TypeAttributes.Public | TypeAttributes.Class,
            typeof(object),
            [typeof(IDomainEvent)]);

        ImplementThrowingProperty(typeBuilder, nameof(IDomainEvent.Id), typeof(Guid));
        ImplementThrowingProperty(typeBuilder, nameof(IDomainEvent.OccurredOnUtc), typeof(DateTime));

        ConstructorInfo attributeConstructor = typeof(DomainEventNameAttribute).GetConstructor([typeof(string)])
            ?? throw new MissingMethodException(nameof(DomainEventNameAttribute), ".ctor(string)");
        typeBuilder.SetCustomAttribute(new CustomAttributeBuilder(attributeConstructor, [domainEventName]));

        typeBuilder.CreateType();
    }

    private static void ImplementThrowingProperty(TypeBuilder typeBuilder, string name, Type propertyType)
    {
        PropertyInfo interfaceProperty = typeof(IDomainEvent).GetProperty(name)
            ?? throw new MissingMemberException(nameof(IDomainEvent), name);

        MethodBuilder getter = typeBuilder.DefineMethod(
            $"get_{name}",
            MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.SpecialName,
            propertyType,
            Type.EmptyTypes);

        // These fixtures are never instantiated: the registry only reflects over Type metadata,
        // so the getter body is unreachable and simply throws.
        getter.GetILGenerator().ThrowException(typeof(NotSupportedException));

        PropertyBuilder property = typeBuilder.DefineProperty(name, PropertyAttributes.None, propertyType, null);
        property.SetGetMethod(getter);

        MethodInfo interfaceGetter = interfaceProperty.GetGetMethod()
            ?? throw new MissingMemberException(nameof(IDomainEvent), $"get_{name}");
        typeBuilder.DefineMethodOverride(getter, interfaceGetter);
    }
}
