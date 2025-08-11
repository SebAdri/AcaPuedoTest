using Payments.Domain.Orders;
namespace Payments.Application.Abstractions.Persistence;
public interface IOrdersRepository{Task AddAsync(Order order, CancellationToken ct=default); Task<Order?> GetByExternalIdAsync(string externalId, CancellationToken ct=default); Task SaveChangesAsync(CancellationToken ct=default);}