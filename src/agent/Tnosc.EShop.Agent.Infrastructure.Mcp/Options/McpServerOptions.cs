// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System.ComponentModel.DataAnnotations;

namespace Tnosc.EShop.Agent.Infrastructure.Mcp.Options;

/// <summary>
/// Where the tool server lives, bound from the <c>"McpServer"</c> configuration section.
/// </summary>
public sealed class McpServerOptions
{
    /// <summary>The configuration section this class binds to.</summary>
    public const string SectionName = "McpServer";

    /// <summary>
    /// Gets or sets the service-discovery name of the tool server. Defaults to <c>"mcp"</c>.
    /// </summary>
    /// <remarks>
    /// A logical name rather than a URL: the orchestrator resolves it to whatever host and port the
    /// tool server actually got, so nothing here has to be changed between running locally and
    /// running deployed.
    /// </remarks>
    [Required]
    public string ServiceName { get; set; } = "mcp";

    /// <summary>
    /// Gets or sets the path the tool server is mounted at. Defaults to <c>"/mcp"</c>.
    /// </summary>
    [Required]
    public string Path { get; set; } = "/mcp";
}
