// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

namespace Tnosc.EShop.Server.Application.Payment.Ports;

/// <summary>
/// The outcome of a <see cref="IPaymentGateway.CaptureAsync"/> call.
/// </summary>
/// <param name="IsSuccessful">Whether the gateway captured the funds.</param>
/// <param name="CaptureId">The gateway's reference, when successful.</param>
/// <param name="FailureReason">Why the capture failed, when not successful.</param>
public sealed record GatewayCaptureResult(bool IsSuccessful, string? CaptureId, string? FailureReason);
