// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC). All rights reserved.
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;

namespace Tnosc.EShop.Server.Tests.Architecture.Infrastructure;

/// <summary>
/// Locates the repository root from this file's own path (rather than the working directory,
/// which varies between `dotnet test`, an IDE test runner, and CI) and enumerates source files
/// under a given `src/server` project for Roslyn-based scans.
/// </summary>
internal static class SourceScanner
{
    private static readonly char[] PathSeparators = [Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar];
    private static readonly Lazy<string> RepoRootLazy = new(valueFactory: () => ResolveRepoRoot());

    /// <summary>
    /// The repository root directory, resolved once from the caller's compile-time file path.
    /// </summary>
    public static string RepoRoot => RepoRootLazy.Value;

    /// <summary>
    /// Enumerates every `.cs` file under the given `src/server/&lt;projectName&gt;` project,
    /// excluding `bin` and `obj` build output.
    /// </summary>
    public static IEnumerable<string> EnumerateProjectSourceFiles(string projectName)
    {
        string projectDirectory = Path.Combine(path1: RepoRoot, path2: "src", path3: "server", path4: projectName);

        if (!Directory.Exists(path: projectDirectory))
        {
            yield break;
        }

        foreach (string file in Directory.EnumerateFiles(path: projectDirectory, searchPattern: "*.cs", searchOption: SearchOption.AllDirectories))
        {
            if (IsUnderBuildOutput(projectDirectory: projectDirectory, filePath: file))
            {
                continue;
            }

            yield return file;
        }
    }

    private static bool IsUnderBuildOutput(string projectDirectory, string filePath)
    {
        string relative = Path.GetRelativePath(relativeTo: projectDirectory, path: filePath);
        string[] segments = relative.Split(separator: PathSeparators);

        return segments.Length > 0 &&
               (string.Equals(a: segments[0], b: "bin", comparisonType: StringComparison.OrdinalIgnoreCase) ||
                string.Equals(a: segments[0], b: "obj", comparisonType: StringComparison.OrdinalIgnoreCase));
    }

    private static string ResolveRepoRoot([CallerFilePath] string callerFilePath = "")
    {
        // This file lives at <repoRoot>/tests/server/Tnosc.EShop.Server.Tests.Architecture/Infrastructure/SourceScanner.cs
        DirectoryInfo? directory = new(path: Path.GetDirectoryName(path: callerFilePath)!);

        while (directory is not null && !File.Exists(path: Path.Combine(path1: directory.FullName, path2: "Tnosc.EShop.slnx")))
        {
            directory = directory.Parent;
        }

        if (directory is null)
        {
            throw new InvalidOperationException(message: $"Could not locate repository root above '{callerFilePath}'.");
        }

        return directory.FullName;
    }
}
