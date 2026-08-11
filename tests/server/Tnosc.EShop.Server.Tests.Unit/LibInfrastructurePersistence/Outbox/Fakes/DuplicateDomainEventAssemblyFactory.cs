// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
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
        var assemblyName = new AssemblyName(assemblyName: $"Tnosc.Tests.DuplicateDomainEvents.{Guid.NewGuid():N}");
        var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(name: assemblyName, access: AssemblyBuilderAccess.Run);
        ModuleBuilder moduleBuilder = assemblyBuilder.DefineDynamicModule(name: assemblyName.Name!);

        DefineDomainEventType(moduleBuilder: moduleBuilder, typeName: "FirstDuplicateDomainEvent", domainEventName: sharedName);
        DefineDomainEventType(moduleBuilder: moduleBuilder, typeName: "SecondDuplicateDomainEvent", domainEventName: sharedName);

        return assemblyBuilder;
    }

    private static void DefineDomainEventType(ModuleBuilder moduleBuilder, string typeName, string domainEventName)
    {
        TypeBuilder typeBuilder = moduleBuilder.DefineType(
            name: typeName,
            attr: TypeAttributes.Public | TypeAttributes.Class,
            parent: typeof(object),
            interfaces: [typeof(IDomainEvent)]);

        ImplementThrowingProperty(typeBuilder: typeBuilder, name: nameof(IDomainEvent.Id), propertyType: typeof(Guid));
        ImplementThrowingProperty(typeBuilder: typeBuilder, name: nameof(IDomainEvent.OccurredOnUtc), propertyType: typeof(DateTime));

        ConstructorInfo attributeConstructor = typeof(DomainEventNameAttribute).GetConstructor(types: [typeof(string)])
            ?? throw new MissingMethodException(className: nameof(DomainEventNameAttribute), methodName: ".ctor(string)");
        typeBuilder.SetCustomAttribute(customBuilder: new CustomAttributeBuilder(con: attributeConstructor, constructorArgs: [domainEventName]));

        typeBuilder.CreateType();
    }

    private static void ImplementThrowingProperty(TypeBuilder typeBuilder, string name, Type propertyType)
    {
        PropertyInfo interfaceProperty = typeof(IDomainEvent).GetProperty(name: name)
            ?? throw new MissingMemberException(className: nameof(IDomainEvent), memberName: name);

        MethodBuilder getter = typeBuilder.DefineMethod(
            name: $"get_{name}",
            attributes: MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.SpecialName,
            returnType: propertyType,
            parameterTypes: Type.EmptyTypes);

        // These fixtures are never instantiated: the registry only reflects over Type metadata,
        // so the getter body is unreachable and simply throws.
        getter.GetILGenerator().ThrowException(excType: typeof(NotSupportedException));

        PropertyBuilder property = typeBuilder.DefineProperty(name: name, attributes: PropertyAttributes.None, returnType: propertyType, parameterTypes: null);
        property.SetGetMethod(mdBuilder: getter);

        MethodInfo interfaceGetter = interfaceProperty.GetGetMethod()
            ?? throw new MissingMemberException(className: nameof(IDomainEvent), memberName: $"get_{name}");
        typeBuilder.DefineMethodOverride(methodInfoBody: getter, methodInfoDeclaration: interfaceGetter);
    }
}
