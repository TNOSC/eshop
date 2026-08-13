// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using Tnosc.Lib.Application.Commands;

namespace Tnosc.EShop.Server.Application.Identity.Commands.DeactivateCustomer;

/// <summary>
/// Deactivates a customer's profile.
/// </summary>
/// <remarks>
/// Local only: this does not disable the Keycloak account, which an operator does separately in the
/// admin console.
/// </remarks>
/// <param name="CustomerId">The identifier of the customer to deactivate, from the route.</param>
public sealed record DeactivateCustomerCommand(Guid CustomerId) : ICommand;
