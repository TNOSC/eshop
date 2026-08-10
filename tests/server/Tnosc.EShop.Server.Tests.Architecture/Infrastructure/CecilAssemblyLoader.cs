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

namespace Tnosc.EShop.Server.Tests.Architecture.Infrastructure;

/// <summary>
/// Loads an assembly's Mono.Cecil <see cref="TypeDefinition"/>s for rules that need to inspect
/// accessor visibility, `init` modifiers, or generic base types — none of which the NetArchTest
/// fluent API exposes directly.
/// </summary>
internal static class CecilAssemblyLoader
{
    /// <summary>
    /// Returns every non-compiler-generated class and record type declared in the assembly,
    /// skipping nested compiler artifacts such as closures and `&lt;&gt;c` display classes.
    /// </summary>
    public static IReadOnlyList<TypeDefinition> LoadTypes(Assembly assembly)
    {
        using var definition = AssemblyDefinition.ReadAssembly(fileName: assembly.Location);

        return [.. definition.MainModule.GetTypes()
            .Where(predicate: type => type.IsClass && !type.IsCompilerGenerated() && !type.Name.StartsWith(value: '<'))];
    }

    private static bool IsCompilerGenerated(this TypeDefinition type) =>
        type.CustomAttributes.Any(predicate: a =>
            string.Equals(a: a.AttributeType.FullName, b: "System.Runtime.CompilerServices.CompilerGeneratedAttribute", comparisonType: StringComparison.Ordinal));
}
