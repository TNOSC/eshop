// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tnosc.EShop.Server.Application.Ordering.Ports;
using Tnosc.EShop.Server.Domain.Ordering.Orders;
using Tnosc.Lib.Shared.Results;

namespace Tnosc.EShop.Server.Application.Ordering.Commands.PlaceOrder.Steps;

/// <summary>
/// Copies the customer's default address into a <see cref="ShippingAddress"/> the order owns outright.
/// </summary>
/// <remarks>
/// A copy, not a reference. From this point the order's delivery address is Ordering's data: editing
/// or deleting the profile address later leaves already-placed orders saying exactly where they went.
/// The address is re-validated on the way in rather than trusted, because Identity's rules and
/// Ordering's are free to diverge and only this side's are binding here.
/// </remarks>
/// <param name="profileReader">The customer-profile read port.</param>
internal sealed class CustomerResolver(ICustomerProfileReader profileReader) : ICustomerResolver
{
    /// <inheritdoc />
    public async ValueTask<Result<ShippingAddress>> ResolveAsync(
        Guid customerId,
        CancellationToken cancellationToken = default)
    {
        CustomerProfileSnapshot? profile = await profileReader.GetDefaultAddressAsync(
            customerId: customerId,
            cancellationToken: cancellationToken);

        if (profile is null)
        {
            return OrderErrors.NoShippingAddress(customerId: customerId);
        }

        Result<ShippingAddress> address = ShippingAddress.Create(
            street: profile.Street,
            city: profile.City,
            postalCode: profile.PostalCode,
            country: profile.Country);

        if (address.IsError)
        {
            return address.Errors.ToArray();
        }

        return address.Value;
    }
}
