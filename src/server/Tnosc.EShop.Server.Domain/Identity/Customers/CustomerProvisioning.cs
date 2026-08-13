// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

namespace Tnosc.EShop.Server.Domain.Identity.Customers;

/// <summary>
/// The outcome of <see cref="CustomerFactory.ProvisionAsync"/>: the customer, and whether this call
/// is the one that created them.
/// </summary>
/// <remarks>
/// <see cref="WasCreated"/> exists so the endpoint can answer <c>201</c> on a first login and
/// <c>200</c> on every later one without deciding anything: the domain already made that call, and
/// the flag is the domain reporting it. A handler comparing counts or re-querying to work the same
/// thing out would be business branching, which <c>NoBusinessBranchingTests</c> rejects.
/// </remarks>
/// <param name="Customer">The provisioned customer.</param>
/// <param name="WasCreated">
/// <see langword="true"/> when this call registered the customer; <see langword="false"/> when it
/// found an existing one and reconciled it.
/// </param>
public sealed record CustomerProvisioning(Customer Customer, bool WasCreated);
