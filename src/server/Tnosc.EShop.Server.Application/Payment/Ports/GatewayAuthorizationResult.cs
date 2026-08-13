// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

namespace Tnosc.EShop.Server.Application.Payment.Ports;

/// <summary>
/// The outcome of a <see cref="IPaymentGateway.AuthorizeAsync"/> call.
/// </summary>
/// <remarks>
/// A decline is data, not an exception — <see cref="IsApproved"/> is <see langword="false"/> and
/// <see cref="DeclineReason"/> is populated. Only a technical failure (timeout, 5xx, malformed
/// request) makes the adapter throw instead of returning one of these.
/// </remarks>
/// <param name="IsApproved">Whether the gateway approved the authorization.</param>
/// <param name="AuthorizationId">The gateway's reference, when approved.</param>
/// <param name="DeclineReason">Why the gateway declined, when not approved.</param>
public sealed record GatewayAuthorizationResult(bool IsApproved, string? AuthorizationId, string? DeclineReason);
