// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

namespace Tnosc.EShop.Agent.Domain.Agents;

/// <summary>
/// The name of every agent this host serves.
/// </summary>
/// <remarks>
/// <para>
/// These are <c>const</c> because the same literal has to be written by two projects that cannot see
/// each other: the API layer names an agent when it maps that agent's endpoint, and the host
/// registers the agent under the identical key. A shared constant makes a mismatch a compile error;
/// two independent literals make it a 404 at run time, which reads like a routing bug rather than a
/// spelling mistake.
/// </para>
/// <para>
/// The same reasoning already governs cache tags and permission names in this solution.
/// </para>
/// </remarks>
public static class AgentNames
{
    /// <summary>
    /// The storefront assistant: answers questions about the catalogue on a shopper's behalf.
    /// </summary>
    public const string ShoppingAssistant = "shopping-assistant";
}
