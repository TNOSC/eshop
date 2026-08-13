// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

namespace Tnosc.EShop.Server.Api.Payment;

/// <summary>
/// The route templates and OpenAPI tag shared by the Payment endpoints, so a path is spelled once.
/// </summary>
internal static class PaymentRoutes
{
    /// <summary>
    /// The OpenAPI tag every Payment endpoint is grouped under.
    /// </summary>
    public const string Tag = "Payment";

    /// <summary>
    /// Payments in general — initiating one is a <c>POST</c> here.
    /// </summary>
    public const string Payments = "/api/payments";

    /// <summary>
    /// A single payment by identifier.
    /// </summary>
    public const string PaymentById = $"{Payments}/{{id:guid}}";

    /// <summary>
    /// Capturing a single payment.
    /// </summary>
    public const string PaymentCapture = $"{PaymentById}/capture";

    /// <summary>
    /// Refunding a single payment.
    /// </summary>
    public const string PaymentRefund = $"{PaymentById}/refund";

    /// <summary>
    /// The payment initiated for a single order.
    /// </summary>
    public const string PaymentByOrder = "/api/orders/{orderId:guid}/payment";
}
