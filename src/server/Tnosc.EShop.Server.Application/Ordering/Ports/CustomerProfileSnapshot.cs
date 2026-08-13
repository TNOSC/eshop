// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

namespace Tnosc.EShop.Server.Application.Ordering.Ports;

/// <summary>
/// The parts of a customer's profile an order needs — their default delivery address, and nothing
/// else.
/// </summary>
/// <remarks>
/// Narrow on purpose. Ordering has no business knowing a customer's name, phone number or every
/// address they hold; it needs somewhere to send this parcel. A port that returned the whole profile
/// would make Ordering's dependency on Identity look far larger than it is.
/// </remarks>
/// <param name="Street">The street line of the customer's default address.</param>
/// <param name="City">The city.</param>
/// <param name="PostalCode">The postal code.</param>
/// <param name="Country">The ISO 3166-1 alpha-2 country code.</param>
public sealed record CustomerProfileSnapshot(
    string Street,
    string City,
    string PostalCode,
    string Country);
