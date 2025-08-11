using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Payments.Application.Abstractions.Payments;
using Payments.Application.Abstractions.Persistence;
using Payments.Application.Abstractions.Security;
using Payments.Infrastructure.Payments;
using Payments.Infrastructure.Persistence;
using Payments.Infrastructure.Security;
using Payments.Infrastructure.Time;


namespace Payments.Infrastructure.DI;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration cfg)
    {
        var conn = Environment.GetEnvironmentVariable("DATABASE_URL") ?? cfg.GetConnectionString("Default");
        if (string.IsNullOrWhiteSpace(conn))
            services.AddDbContext<PaymentsDbContext>(o => o.UseInMemoryDatabase("payments-dev"));
        else services.AddDbContext<PaymentsDbContext>(o => o.UseNpgsql(conn));
        services.AddScoped<IOrdersRepository, OrdersRepository>();
        services.AddHttpClient<IAdamsPayClient, AdamsPayClient>();
        services.AddSingleton<ISignatureVerifier, HmacSignatureVerifier>();
        services.AddSingleton<IDateTimeProvider, SystemDateTimeProvider>();
        return services;
    }
}