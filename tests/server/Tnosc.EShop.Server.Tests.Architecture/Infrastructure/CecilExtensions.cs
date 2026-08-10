// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC). All rights reserved.
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Tnosc.EShop.Server.Tests.Architecture.Infrastructure;

/// <summary>
/// Shared Mono.Cecil helpers for walking generic base-type chains and interface lists — the
/// primitives the domain-purity and handler rules build on.
/// </summary>
internal static class CecilExtensions
{
    /// <summary>
    /// Walks <paramref name="type"/>'s base-type chain (resolving generic instantiations) looking
    /// for an open-generic base whose full name (e.g. <c>Tnosc.Lib.Domain.AggregateRoot`1</c>)
    /// matches <paramref name="openGenericFullName"/>.
    /// </summary>
    public static bool InheritsOpenGeneric(this TypeDefinition type, string openGenericFullName)
    {
        TypeReference? current = type.BaseType;

        while (current is not null)
        {
            if (current is GenericInstanceType generic && string.Equals(a: generic.ElementType.FullName, b: openGenericFullName, comparisonType: StringComparison.Ordinal))
            {
                return true;
            }

            if (string.Equals(a: current.FullName, b: openGenericFullName, comparisonType: StringComparison.Ordinal))
            {
                return true;
            }

            current = current.Resolve()?.BaseType;
        }

        return false;
    }

    /// <summary>
    /// True if <paramref name="type"/> implements, directly or via an inherited interface, an
    /// interface whose full name matches <paramref name="interfaceFullName"/>.
    /// </summary>
    public static bool ImplementsInterface(this TypeDefinition type, string interfaceFullName)
    {
        foreach (InterfaceImplementation implementation in type.Interfaces)
        {
            TypeReference interfaceType = implementation.InterfaceType;

            if (string.Equals(a: interfaceType.FullName, b: interfaceFullName, comparisonType: StringComparison.Ordinal))
            {
                return true;
            }

            if (interfaceType is GenericInstanceType generic && string.Equals(a: generic.ElementType.FullName, b: interfaceFullName, comparisonType: StringComparison.Ordinal))
            {
                return true;
            }

            if (interfaceType.Resolve() is { } resolved && resolved.ImplementsInterface(interfaceFullName: interfaceFullName))
            {
                return true;
            }
        }

        TypeReference? baseType = type.BaseType;
        return baseType?.Resolve() is { } resolvedBase && resolvedBase.ImplementsInterface(interfaceFullName: interfaceFullName);
    }

    /// <summary>
    /// True if the property's setter is `init`-only rather than a settable `set` — Cecil wraps an
    /// `init` accessor's return type in a `System.Runtime.CompilerServices.IsExternalInit`
    /// required-modifier type.
    /// </summary>
    public static bool IsInitOnly(this MethodDefinition setMethod) =>
        setMethod.ReturnType is RequiredModifierType modifier &&
        string.Equals(a: modifier.ModifierType.FullName, b: "System.Runtime.CompilerServices.IsExternalInit", comparisonType: StringComparison.Ordinal);

    /// <summary>
    /// True if the type is a compiler-generated `record` — Cecil has no first-class notion of
    /// records, but the compiler always emits a synthesized `&lt;Clone&gt;$` method for one.
    /// </summary>
    public static bool IsRecord(this TypeDefinition type) =>
        type.Methods.Any(predicate: m => string.Equals(a: m.Name, b: "<Clone>$", comparisonType: StringComparison.Ordinal));

    /// <summary>
    /// True if any field, constructor/method parameter, local variable, or instruction operand in
    /// <paramref name="type"/> refers to a type matching <paramref name="predicate"/> — the
    /// mechanism behind "handlers reference no DbContext/repository" rules.
    /// </summary>
    public static bool ReferencesTypeMatching(this TypeDefinition type, Func<TypeReference, bool> predicate)
    {
        foreach (FieldDefinition field in type.Fields)
        {
            if (predicate(field.FieldType))
            {
                return true;
            }
        }

        foreach (MethodDefinition method in type.Methods)
        {
            foreach (ParameterDefinition parameter in method.Parameters)
            {
                if (predicate(parameter.ParameterType))
                {
                    return true;
                }
            }

            if (!method.HasBody)
            {
                continue;
            }

            foreach (VariableDefinition variable in method.Body.Variables)
            {
                if (predicate(variable.VariableType))
                {
                    return true;
                }
            }

            foreach (Instruction instruction in method.Body.Instructions)
            {
                TypeReference? referenced = instruction.Operand switch
                {
                    TypeReference typeReference => typeReference,
                    FieldReference fieldReference => fieldReference.DeclaringType,
                    MethodReference methodReference => methodReference.DeclaringType,
                    _ => null,
                };

                if (referenced is not null && predicate(referenced))
                {
                    return true;
                }
            }
        }

        return false;
    }
}
