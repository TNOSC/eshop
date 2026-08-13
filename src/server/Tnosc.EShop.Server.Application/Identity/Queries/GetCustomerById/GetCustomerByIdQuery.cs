// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using Tnosc.EShop.Server.Application.Identity.Queries.GetCurrentCustomer;
using Tnosc.Lib.Application.Queries;

namespace Tnosc.EShop.Server.Application.Identity.Queries.GetCustomerById;

/// <summary>
/// Reads any customer's profile by identifier.
/// </summary>
/// <remarks>
/// Unlike <see cref="GetCurrentCustomerQuery"/>, this names a customer other than the caller, so the
/// endpoint behind it requires the <c>identity:read</c> permission.
/// </remarks>
/// <param name="CustomerId">The identifier of the customer to read.</param>
public sealed record GetCustomerByIdQuery(Guid CustomerId) : IQuery<CustomerDto>;
