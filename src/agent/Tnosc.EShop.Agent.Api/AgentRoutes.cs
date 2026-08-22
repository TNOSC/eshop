// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using Tnosc.EShop.Agent.Domain.Agents;

namespace Tnosc.EShop.Agent.Api;

/// <summary>
/// The route templates and OpenAPI tag shared by the agent endpoints, so a path is spelled once.
/// </summary>
internal static class AgentRoutes
{
    /// <summary>
    /// The OpenAPI tag every agent endpoint is grouped under.
    /// </summary>
    public const string Tag = "Agents";

    /// <summary>
    /// The prefix every agent is mounted under.
    /// </summary>
    public const string Prefix = "/agents";

    /// <summary>
    /// The shopping assistant's conversation endpoint.
    /// </summary>
    public const string ShoppingAssistant = $"{Prefix}/{AgentNames.ShoppingAssistant}";
}
