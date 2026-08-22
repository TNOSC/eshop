// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.ClientModel;
using System.ClientModel.Primitives;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Azure.Core;

namespace Tnosc.EShop.Agent.Infrastructure.Ai;

/// <summary>
/// Lets an Azure identity satisfy the token contract the Foundry client expects.
/// </summary>
/// <remarks>
/// <para>
/// The two SDKs have not converged: Azure identities implement <see cref="TokenCredential"/> from
/// the Azure SDK, while the Foundry project client asks for the newer <c>System.ClientModel</c>
/// token abstraction, and no adapter between them ships in either package. This class is that
/// adapter, and it is deliberately the smallest thing that closes the gap — a few dozen lines under
/// our control rather than a dependency taken for one type.
/// </para>
/// <para>
/// Delete it the moment the SDKs bridge this themselves.
/// </para>
/// </remarks>
/// <param name="credential">The Azure identity to obtain tokens from.</param>
/// <param name="defaultScope">
/// The scope to request when the caller does not name one. The Foundry data plane is reached with
/// an Azure AI scope, and a request with no scope at all is rejected rather than defaulted.
/// </param>
internal sealed class AzureCredentialTokenProvider(TokenCredential credential, string defaultScope)
    : AuthenticationTokenProvider
{
    /// <summary>
    /// The scope the Foundry data plane accepts when nothing more specific is requested.
    /// </summary>
    public const string DefaultAiScope = "https://ai.azure.com/.default";

    /// <inheritdoc />
    public override GetTokenOptions? CreateTokenOptions(IReadOnlyDictionary<string, object> properties) =>
        new(properties: properties);

    /// <inheritdoc />
    public override AuthenticationToken GetToken(GetTokenOptions options, CancellationToken cancellationToken)
    {
        AccessToken token = credential.GetToken(
            requestContext: new TokenRequestContext(scopes: ResolveScopes(options: options)),
            cancellationToken: cancellationToken);

        return ToAuthenticationToken(token: token);
    }

    /// <inheritdoc />
    public override async ValueTask<AuthenticationToken> GetTokenAsync(
        GetTokenOptions options,
        CancellationToken cancellationToken)
    {
        AccessToken token = await credential.GetTokenAsync(
            requestContext: new TokenRequestContext(scopes: ResolveScopes(options: options)),
            cancellationToken: cancellationToken);

        return ToAuthenticationToken(token: token);
    }

    private static AuthenticationToken ToAuthenticationToken(AccessToken token) =>
        new(tokenValue: token.Token,
            tokenType: "Bearer",
            expiresOn: token.ExpiresOn,
            refreshOn: token.RefreshOn);

    private string[] ResolveScopes(GetTokenOptions options)
    {
        if (!options.Properties.TryGetValue(key: GetTokenOptions.ScopesPropertyName, value: out object? scopes))
        {
            return [defaultScope];
        }

        // The property is loosely typed by contract, and the shape differs between callers: some
        // pass a single scope, others a collection. Handling both here keeps the failure out of a
        // cast exception thrown deep inside a token refresh, where it is far harder to read.
        return scopes switch
        {
            string single => [single],
            IEnumerable<string> many => [.. many],
            _ => [defaultScope],
        };
    }
}
