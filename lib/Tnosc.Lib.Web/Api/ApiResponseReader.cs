// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Tnosc.Lib.Web.Contracts;
using Tnosc.Lib.Web.Results;

namespace Tnosc.Lib.Web.Api;

/// <summary>
/// Reads an <see cref="HttpResponseMessage"/> into an <see cref="ClientResult"/> or
/// <see cref="ClientResult{TValue}"/> — the one place that knows about the server's four response
/// shapes, so no typed client duplicates this logic.
/// </summary>
public static class ApiResponseReader
{
    /// <summary>
    /// Reads a response expected to carry a value on success — a 200/201 JSON body, or a 204 with
    /// no body at all.
    /// </summary>
    /// <param name="response">The response to read.</param>
    /// <param name="cancellationToken">The token observed while reading the response body.</param>
    public static async Task<ClientResult<TValue>> ReadAsync<TValue>(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
        {
            return response.StatusCode is HttpStatusCode.NoContent
                ? ClientResult<TValue>.Success(value: default!)
                : ClientResult<TValue>.Success(
                    value: (await response.Content.ReadFromJsonAsync<TValue>(
                        cancellationToken: cancellationToken))!);
        }

        ClientProblem problem =
            await ReadProblemAsync(response: response, cancellationToken: cancellationToken)
            ?? ClientProblem.FromStatus(status: (int)response.StatusCode);

        return ClientResult<TValue>.Failure(problem: problem);
    }

    /// <summary>Reads a response expected to carry no value on success — a bare 204.</summary>
    /// <param name="response">The response to read.</param>
    /// <param name="cancellationToken">The token observed while reading the response body.</param>
    public static async Task<ClientResult> ReadAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
        {
            return ClientResult.Success();
        }

        ClientProblem problem =
            await ReadProblemAsync(response: response, cancellationToken: cancellationToken)
            ?? ClientProblem.FromStatus(status: (int)response.StatusCode);

        return ClientResult.Failure(problem: problem);
    }

    private static async Task<ClientProblem?> ReadProblemAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        try
        {
            return await response.Content.ReadFromJsonAsync<ClientProblem>(cancellationToken: cancellationToken);
        }
        catch (JsonException)
        {
            // A non-JSON body — e.g. an HTML error page from an infrastructure hop — is not a bug here.
            return null;
        }
    }
}
