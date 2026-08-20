// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System.ComponentModel;
using ModelContextProtocol.Server;

namespace Tnosc.EShop.Mcp.Tool;

/// <summary>
/// A placeholder tool used to verify the MCP server's tool-registration pipeline end to end.
/// </summary>
[McpServerToolType]
public static class DummyTool
{
    /// <summary>Echoes back the supplied message, prefixed to prove a round trip through the tool pipeline.</summary>
    [McpServerTool]
    [Description("Echoes back the supplied message. Used to verify the MCP server is wired up correctly.")]
    public static string Echo(string message) => $"Echo: {message}";
}
