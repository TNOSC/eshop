// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

namespace Tnosc.EShop.Server.Infrastructure.External.Payment;

/// <summary>
/// The deterministic test card numbers <see cref="FakePaymentGateway"/> recognises. Every other
/// funding-source reference — including <see langword="null"/>, any other card number, and any
/// wallet id — is treated as an approval, matching how most real sandbox gateways behave.
/// </summary>
public static class FakeCardNumbers
{
    /// <summary>
    /// Always approved by the gateway.
    /// </summary>
    public const string Approved = "4242424242424242";

    /// <summary>
    /// Always declined by the gateway — a business outcome, not a thrown exception.
    /// </summary>
    public const string Declined = "4000000000000002";

    /// <summary>
    /// Always times out — the gateway throws <see cref="Tnosc.Lib.Application.Exceptions.TransientFailureException"/>
    /// instead of returning a result, exercising the retry pipeline.
    /// </summary>
    public const string Timeout = "4000000000000119";

    /// <summary>
    /// Always rejected by the gateway as malformed — throws
    /// <see cref="Tnosc.Lib.Application.Exceptions.InvalidRequestException"/>.
    /// </summary>
    public const string Malformed = "0000000000000000";
}
