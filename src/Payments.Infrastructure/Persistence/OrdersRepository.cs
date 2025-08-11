using Microsoft.EntityFrameworkCore;
using Payments.Application.Abstractions.Persistence;
using Payments.Domain.Orders;

namespace Payments.Infrastructure.Persistence;

public class OrdersRepository : IOrdersRepository
{
    private readonly PaymentsDbContext _db;
    public OrdersRepository(PaymentsDbContext db) => _db = db;
    public async Task AddAsync(Order order, CancellationToken ct = default) => await _db.Orders.AddAsync(order, ct);

    public Task<Order?> GetByExternalIdAsync(string externalId, CancellationToken ct = default) =>
        _db.Orders.FirstOrDefaultAsync(o => o.ExternalId == externalId, ct)!;

    public Task SaveChangesAsync(CancellationToken ct = default) => _db.SaveChangesAsync(ct);
}