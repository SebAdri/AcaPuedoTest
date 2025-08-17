using System.Text;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.EntityFrameworkCore;
using Payments.Application.Abstractions.Payments;
using Payments.Application.Abstractions.Persistence;
using Payments.Application.Abstractions.Security;
using Payments.Contracts.Dtos;
using Payments.Domain.Orders;
using Payments.Domain.Orders.Enums;
using Payments.Domain.Payments;
using Payments.Infrastructure.DI;
using Payments.Infrastructure.Persistence;
using Serilog;

var builder = WebApplication.CreateBuilder(args);
builder.Host.UseSerilog((ctx, lc) => lc.WriteTo.Console().ReadFrom.Configuration(ctx.Configuration));
builder.Services.Configure<JsonOptions>(o => o.SerializerOptions.PropertyNamingPolicy = null);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddInfrastructure(builder.Configuration);
var app = builder.Build();
app.UseSerilogRequestLogging();
app.UseSwagger();
app.UseSwaggerUI();
// app.MapGet("/", () => Results.Ok(new { ok = true, name = Payments.Api, now = DateTime.UtcNow }));
app.MapPost("/api/orders",
    async (OrderCreateRequest req, IOrdersRepository repo, IAdamsPayClient apClient, CancellationToken ct) =>
    {
        var existing = await repo.GetByExternalIdAsync(req.ExternalId, ct);
        if (existing is not null) return Results.Conflict(new { message = "ExternalId already exists" });
        var order = new Order
        {
            ExternalId = req.ExternalId, Amount = req.Amount, Currency = req.Currency ?? "PYG",
            Status = OrderStatus.Pending
        };
        await repo.AddAsync(order, ct);
        await repo.SaveChangesAsync(ct);
        // var paymentUrl = await apClient.CreateChargeAsync(order.ExternalId, order.Amount, order.Currency,
        //     req.Description ?? $""Order {
        //     order.ExternalId
        // }
        // "",ct);
        var paymentUrl = await apClient.CreateChargeAsync(order.ExternalId, order.Amount, order.Currency,
            req.Description, ct);
        order.PaymentUrl = paymentUrl;
        await repo.SaveChangesAsync(ct);
        var dto = new OrderDto(order.ExternalId, order.Amount, order.Currency, order.Status.ToString(),
            order.PaymentUrl);
        return Results.Ok(dto);
    }).WithOpenApi();
app.MapGet("/api/orders/{externalId}", async (string externalId, IOrdersRepository repo, CancellationToken ct) =>
{
    var order = await repo.GetByExternalIdAsync(externalId, ct);
    return order is null
        ? Results.NotFound()
        : Results.Ok(new OrderDto(order.ExternalId, order.Amount, order.Currency, order.Status.ToString(),
            order.PaymentUrl));
}).WithOpenApi();
app.MapGet("/api/debts/{externalId}", async (string externalId, IAdamsPayClient apClient, CancellationToken ct) =>
{
    var debt = await apClient.GetOrderAsync(externalId);
    return debt is null
        ? Results.NotFound()
        : Results.Ok(debt);
}).WithOpenApi();
app.MapPost("/webhooks/adamspay", async (HttpRequest request, IOrdersRepository repo,
    Payments.Infrastructure.Persistence.PaymentsDbContext db, ISignatureVerifier sig, IAdamsNotifyVerifier verifier, IConfiguration cfg,
    CancellationToken ct) =>
{
    // // 1) Habilitar re-lectura del body y leer RAW
    // request.EnableBuffering();
    // using var readere = new StreamReader(request.Body, Encoding.UTF8, leaveOpen: true);
    // var rawBody = await readere.ReadToEndAsync();
    // request.Body.Position = 0;
    //
    // // 2) Obtener cabeceras de AdamsPay
    // var appHeader = request.Headers["x-adams-notify-app"].ToString();
    // var hashHeader = request.Headers["x-adams-notify-hash"].ToString();
    //
    // // 3) Cargar secreto (y opcional APP_ID) desde config/env
    // var secrett = cfg["ADAMSPAY_WEBHOOK_SECRET"];
    // var expectedAppId = cfg["ADAMSPAY_APP_ID"]; // opcional
    //
    // if (string.IsNullOrWhiteSpace(secrett))
    //     return Results.StatusCode(StatusCodes.Status500InternalServerError); // falta config
    //
    // // 4) Validar hash (MD5)
    // var ok = verifier.Verify(rawBody, hashHeader, secrett);
    // if (!ok)
    //     return Results.Unauthorized(); // 401
    //
    // // 5) (Opcional) Validar appId
    // if (!string.IsNullOrWhiteSpace(expectedAppId) &&
    //     !string.Equals(expectedAppId, appHeader, StringComparison.Ordinal))
    // {
    //     return Results.Unauthorized();
    // }
    
    using var reader = new StreamReader(request.Body);
    var payload = await reader.ReadToEndAsync();
    var secret = cfg["ADAMSPAY_WEBHOOK_SECRET"];
    var signature = request.Headers["X-AP-Signature"].FirstOrDefault();
    if (!string.IsNullOrWhiteSpace(secret) && !sig.Verify(payload, signature, secret)) return Results.Unauthorized();
    var evt = System.Text.Json.JsonSerializer.Deserialize<AdamspayWebhook>(payload);
    if (evt is null) return Results.BadRequest(new { message = "Invalid payload" });
    var order = await repo.GetByExternalIdAsync(evt.Notify.Id, ct);
    if (order is null) return Results.NotFound();
    order.Status = evt.Debt.PayStatus.Status?.ToLowerInvariant() switch
    {
        "paid" => OrderStatus.Paid, "expired" => OrderStatus
            .Expired,
        "cancelled" => OrderStatus.Cancelled, _ => order.Status
    };
    db.PaymentEvents.Add(new PaymentEvent
    {
        ExternalId = evt.Debt.DocId, ProviderEventId = evt.Notify.Id, EventType = evt.Notify.Type ?? "unknown", Payload = payload
    });
    await repo.SaveChangesAsync(ct);
    return Results.Ok(new { received = true });
}).WithOpenApi();
var logger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("Startup");
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<Payments.Infrastructure.Persistence.PaymentsDbContext>();
    if (db.Database.IsRelational())
    {
        logger.LogInformation("EF Provider: RELATIONAL (running MigrateAsync)");
        await db.Database.MigrateAsync();
    }
    else
    {
        logger.LogInformation("EF Provider: INMEMORY (running EnsureCreatedAsync)");
        await db.Database.EnsureCreatedAsync();
    }
}
app.MapPost("/webhooks/adamspayy", async (HttpContext context, ILogger<Program> logger) =>
{
    // Leer body completo como string
    context.Request.EnableBuffering();
    using var reader = new StreamReader(context.Request.Body, leaveOpen: true);
    var body = await reader.ReadToEndAsync();
    context.Request.Body.Position = 0; // reset para que no se pierda

    // Log body
    logger.LogInformation("Adamspay Webhook Body: {Body}", body);

    // Log headers
    foreach (var header in context.Request.Headers)
    {
        logger.LogInformation("Header: {Key} = {Value}", header.Key, header.Value);
    }

    // Respuesta al webhook
    return Results.Ok(new { status = "received" });
});


app.Run();

record OrderCreateRequest(string ExternalId, decimal Amount, string? Currency, string? Description);

public record AdamspayWebhook(
    Notify Notify, 
    Debt Debt
);

public record Notify(
    string Id,
    string Type,
    int Version,
    DateTime Time,
    string Merchant,
    string App,
    string Env
);
