using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Payments.Infrastructure.Persistence;

public class PaymentsDbContextFactory : IDesignTimeDbContextFactory<PaymentsDbContext>
{
    public PaymentsDbContext CreateDbContext(string[] args)
    {
        var conn = Environment.GetEnvironmentVariable("DATABASE_URL")
                   ?? throw new InvalidOperationException("Set DATABASE_URL before running dotnet-ef.");
        var opts = new DbContextOptionsBuilder<PaymentsDbContext>()
            .UseNpgsql(conn)
            .Options;
        return new PaymentsDbContext(opts);
    }
}