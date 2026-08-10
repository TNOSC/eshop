// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC). All rights reserved.
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Mono.Cecil;

namespace Tnosc.EShop.Server.Tests.Architecture.Infrastructure;

/// <summary>
/// Loads an assembly's Mono.Cecil <see cref="TypeDefinition"/>s for rules that need to inspect
/// accessor visibility, `init` modifiers, or generic base types — none of which the NetArchTest
/// fluent API exposes directly.
/// </summary>
/// <remarks>
/// Modules are read with <see cref="ReadingMode.Immediate"/> and cached for the lifetime of the test
/// process rather than disposed per call. Cecil reads method bodies lazily off the still-open PE
/// stream, so a disposed <see cref="AssemblyDefinition"/> turns every later
/// <see cref="CecilExtensions.ReferencesTypeMatching"/> into an <see cref="ObjectDisposedException"/>.
/// Reading eagerly also keeps the cached definitions safe to share across xUnit's parallel collections.
/// </remarks>
internal static class CecilAssemblyLoader
{
    private static readonly ConcurrentDictionary<string, IReadOnlyList<TypeDefinition>> TypeCache =
        new(comparer: StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Returns every non-compiler-generated class and record type declared in the assembly,
    /// skipping nested compiler artifacts such as closures and `&lt;&gt;c` display classes.
    /// </summary>
    public static IReadOnlyList<TypeDefinition> LoadTypes(Assembly assembly) =>
        TypeCache.GetOrAdd(key: assembly.Location, valueFactory: static location => ReadTypes(location: location));

    private static IReadOnlyList<TypeDefinition> ReadTypes(string location)
    {
        var resolver = new DefaultAssemblyResolver();
        resolver.AddSearchDirectory(directory: Path.GetDirectoryName(path: location)!);

        var definition = AssemblyDefinition.ReadAssembly(
            fileName: location,
            parameters: new ReaderParameters
            {
                ReadingMode = ReadingMode.Immediate,
                AssemblyResolver = resolver,
            });

        return [.. definition.MainModule.GetTypes()
            .Where(predicate: type => type.IsClass && !type.IsCompilerGenerated() && !type.Name.StartsWith(value: '<'))];
    }

    private static bool IsCompilerGenerated(this TypeDefinition type) =>
        type.CustomAttributes.Any(predicate: a =>
            string.Equals(a: a.AttributeType.FullName, b: "System.Runtime.CompilerServices.CompilerGeneratedAttribute", comparisonType: StringComparison.Ordinal));
}
