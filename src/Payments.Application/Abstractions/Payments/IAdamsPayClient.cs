using Payments.Domain.Orders;

namespace Payments.Application.Abstractions.Payments;

public interface IAdamsPayClient
{
    Task<string> CreateChargeAsync(string externalId, decimal amount, string currency, string description,
        CancellationToken ct = default);

    public Task<AdamspayGetResponse> GetOrderAsync(string externalId, CancellationToken ct = default);
}