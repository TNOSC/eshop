// ----------------------------------------------------------------------------------
// Copyright (c) Tunisian .NET Open Source Community (TNOSC).
// This code is provided by TNOSC and is freely available under the MIT License.
// Author: Ahmed HEDFI (ahmed.hedfi@gmail.com)
// ----------------------------------------------------------------------------------

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Tnosc.EShop.Server.Application.Payment.Queries.GetPaymentByOrder;
using Tnosc.EShop.Server.Domain.Payment.Payments;
using Tnosc.EShop.Server.Infrastructure.Persistence.Contexts;
using Tnosc.EShop.Server.Infrastructure.Persistence.Payment.ReadModels;
using Tnosc.Lib.Application.Queries;
using Tnosc.Lib.Domain.Results;

namespace Tnosc.EShop.Server.Infrastructure.Persistence.Payment.Queries;

/// <summary>
/// Projects the payment initiated for an order, straight from the read context.
/// </summary>
/// <param name="context">The read context.</param>
internal sealed class GetPaymentByOrderQueryHandler(EShopReadDbContext context)
    : IQueryHandler<GetPaymentByOrderQuery, PaymentDto>
{
    /// <inheritdoc />
    public async ValueTask<Result<PaymentDto>> HandleAsync(
        GetPaymentByOrderQuery query,
        CancellationToken cancellationToken = default)
    {
        PaymentDto? payment = await context.Set<PaymentReadModel>()
            .Where(predicate: readModel => readModel.OrderId == query.OrderId)
            .Select(selector: readModel => new PaymentDto(
                Id: readModel.Id,
                OrderId: readModel.OrderId,
                AmountAmount: readModel.Amount,
                AmountCurrency: readModel.Currency,
                Method: readModel.Method,
                Status: readModel.Status,
                GatewayReference: readModel.GatewayReference,
                FailureReason: readModel.FailureReason))
            .FirstOrDefaultAsync(cancellationToken: cancellationToken);

        if (payment is null)
        {
            return PaymentErrors.NotFoundForOrder(orderId: query.OrderId);
        }

        return payment;
    }
}
