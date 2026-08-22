// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System.ComponentModel.DataAnnotations;

namespace Tnosc.EShop.Agent.Infrastructure.Ai.Options;

/// <summary>
/// Which Microsoft Foundry project and model serve this host's agents, bound from the
/// <c>"Foundry"</c> configuration section.
/// </summary>
/// <remarks>
/// There is deliberately no API key here. Credentials come from the ambient Azure identity, so a key
/// never lands in configuration, in a container image, or in a commit.
/// </remarks>
public sealed class FoundryOptions
{
    /// <summary>The configuration section this class binds to.</summary>
    public const string SectionName = "Foundry";

    /// <summary>
    /// Gets or sets the Foundry project endpoint.
    /// </summary>
    [Required]
    [Url]
    public string Endpoint { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the model deployment agents run against.
    /// </summary>
    [Required]
    public string DeploymentName { get; set; } = string.Empty;
}
