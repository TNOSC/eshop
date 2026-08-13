// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;

namespace Tnosc.EShop.Server.Application.Identity.Commands.ProvisionCustomer;

/// <summary>
/// What provisioning produced: the customer's identifier, and whether this call is the one that
/// created them.
/// </summary>
/// <remarks>
/// A flat projection of the domain's <c>CustomerProvisioning</c>, so the aggregate itself never
/// leaves the handler. <see cref="WasCreated"/> is what lets the endpoint answer <c>201</c> on a first
/// login and <c>200</c> on every later one while deciding nothing of its own — the domain already
/// decided, and this carries the verdict out.
/// </remarks>
/// <param name="CustomerId">The provisioned customer's identifier.</param>
/// <param name="WasCreated">
/// <see langword="true"/> when this call registered the customer; <see langword="false"/> when it
/// reconciled an existing one.
/// </param>
public sealed record ProvisionCustomerResult(Guid CustomerId, bool WasCreated);
