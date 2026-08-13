// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Tnosc.EShop.Server.Infrastructure.Persistence.Contexts;
using Tnosc.Lib.Infrastructure.Persistence;
using PaymentAggregate = Tnosc.EShop.Server.Domain.Payment.Payments.Payment;
using PaymentAggregateId = Tnosc.EShop.Server.Domain.Payment.Payments.PaymentId;
using PaymentRepositoryContract = Tnosc.EShop.Server.Domain.Payment.Payments.IPaymentRepository;

namespace Tnosc.EShop.Server.Infrastructure.Persistence.Payment.Repositories;

/// <summary>
/// The write-side <see cref="PaymentAggregate"/> repository.
/// </summary>
/// <param name="context">The write context this repository reads and writes through.</param>
internal sealed class PaymentRepository(EShopWriteDbContext context)
    : RepositoryBase<PaymentAggregate, PaymentAggregateId>(context), PaymentRepositoryContract
{
    /// <inheritdoc />
    public async ValueTask<PaymentAggregate?> GetByOrderIdAsync(
        Guid orderId,
        CancellationToken cancellationToken = default) =>
        await context.Set<PaymentAggregate>()
            .FirstOrDefaultAsync(predicate: payment => payment.OrderId == orderId, cancellationToken: cancellationToken);
}
