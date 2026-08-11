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
/// Mechanises <c>.claude/rules/configuration-options.md</c>: <c>IConfiguration</c> and the
/// <c>IOptions&lt;T&gt;</c> family may only ever be touched inside an <c>AddXxx</c> composition-root
/// extension method — never taken as a constructor parameter by an ordinary class.
/// </summary>
public sealed class ConfigurationTests
{
    private static readonly string[] ConfigurationTypeNames =
    [
        "Microsoft.Extensions.Configuration.IConfiguration",
        "Microsoft.Extensions.Configuration.IConfigurationSection",
    ];

    private static readonly string[] OptionsWrapperOpenGenericNames =
    [
        "Microsoft.Extensions.Options.IOptions`1",
        "Microsoft.Extensions.Options.IOptionsSnapshot`1",
        "Microsoft.Extensions.Options.IOptionsMonitor`1",
    ];

    private static readonly Assembly[] AllAssemblies =
    [
        Tnosc.EShop.Server.Domain.AssemblyReference.Assembly,
        Tnosc.EShop.Server.Application.AssemblyReference.Assembly,
        Tnosc.EShop.Server.Api.AssemblyReference.Assembly,
        Tnosc.EShop.Server.Infrastructure.Persistence.AssemblyReference.Assembly,
        Tnosc.EShop.Server.Infrastructure.External.AssemblyReference.Assembly,
        Tnosc.EShop.Server.Infrastructure.Job.AssemblyReference.Assembly,
        Tnosc.EShop.Server.Shared.AssemblyReference.Assembly,
        Assembly.Load(assemblyString: "Tnosc.EShop.Server.Host"),
    ];

    // No class anywhere takes IConfiguration/IConfigurationSection as a constructor parameter.
    // AddXxx extension methods are static methods on static classes, not constructors, so the
    // one sanctioned composition-root binding site is untouched by this rule.
    [Fact]
    public void No_Constructor_Should_Inject_IConfiguration()
    {
        List<string> violations = CollectConstructorParameterViolations(isForbidden: IsConfigurationType);

        violations.ShouldBeEmpty(customMessage: $"No class may constructor-inject IConfiguration/IConfigurationSection — bind a narrow <Feature>Options class in the owning AddXxx extension method instead (see .claude/rules/configuration-options.md): {string.Join(separator: ", ", values: violations)}");
    }

    // No class anywhere takes IOptions<T>/IOptionsSnapshot<T>/IOptionsMonitor<T> as a constructor
    // parameter. Consumers take the plain, unwrapped settings class directly.
    [Fact]
    public void No_Constructor_Should_Inject_IOptions()
    {
        List<string> violations = CollectConstructorParameterViolations(isForbidden: IsOptionsWrapperType);

        violations.ShouldBeEmpty(customMessage: $"No class may constructor-inject IOptions<T>/IOptionsSnapshot<T>/IOptionsMonitor<T> — unwrap to the plain settings class in the owning AddXxx extension method and inject that instead (see .claude/rules/configuration-options.md): {string.Join(separator: ", ", values: violations)}");
    }

    private static List<string> CollectConstructorParameterViolations(Func<TypeReference, bool> isForbidden)
    {
        List<string> violations = [];

        foreach (Assembly assembly in AllAssemblies)
        {
            foreach (TypeDefinition type in CecilAssemblyLoader.LoadTypes(assembly: assembly))
            {
                foreach (MethodDefinition constructor in type.Methods.Where(predicate: m => m.IsConstructor))
                {
                    foreach (ParameterDefinition parameter in constructor.Parameters)
                    {
                        if (isForbidden(parameter.ParameterType))
                        {
                            violations.Add(item: $"{type.FullName}({parameter.ParameterType.Name} {parameter.Name})");
                        }
                    }
                }
            }
        }

        return violations;
    }

    private static bool IsConfigurationType(TypeReference typeReference) =>
        ConfigurationTypeNames.Contains(value: typeReference.FullName, comparer: StringComparer.Ordinal);

    private static bool IsOptionsWrapperType(TypeReference typeReference) =>
        typeReference is GenericInstanceType generic &&
        OptionsWrapperOpenGenericNames.Contains(value: generic.ElementType.FullName, comparer: StringComparer.Ordinal);
}
