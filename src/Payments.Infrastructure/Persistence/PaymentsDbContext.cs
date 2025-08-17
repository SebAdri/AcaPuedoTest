using Microsoft.EntityFrameworkCore;
using Payments.Domain.Orders;
using Payments.Domain.Payments;

namespace Payments.Infrastructure.Persistence;

public class PaymentsDbContext : DbContext
{
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<PaymentEvent> PaymentEvents => Set<PaymentEvent>();

    public PaymentsDbContext(DbContextOptions<PaymentsDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<Order>().HasIndex(o => o.ExternalId).IsUnique();
        b.Entity<Order>()
            .Property(x => x.Amount)
            .HasColumnType("numeric(18,2)");
    }
}