// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using Tnosc.Lib.Application.Queries;

namespace Tnosc.EShop.Server.Application.Identity.Queries.GetCurrentCustomer;

/// <summary>
/// Reads the caller's own customer profile.
/// </summary>
/// <remarks>
/// The endpoint reads <c>IUserContext.UserId</c> and puts it here, so the handler takes the caller's
/// identity as plain data and stays testable without an HTTP context. Because the profile is resolved
/// from the token rather than from a route value, one customer structurally cannot read another's.
/// </remarks>
/// <param name="ExternalUserId">The identity provider's subject identifier for the caller.</param>
public sealed record GetCurrentCustomerQuery(string? ExternalUserId) : IQuery<CustomerDto>;
