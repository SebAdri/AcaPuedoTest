using Microsoft.AspNetCore.Http.Json;
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
app.MapPost("/webhooks/adamspay", async (HttpRequest request, IOrdersRepository repo,
    Payments.Infrastructure.Persistence.PaymentsDbContext db, ISignatureVerifier sig, IConfiguration cfg,
    CancellationToken ct) =>
{
    using var reader = new StreamReader(request.Body);
    var payload = await reader.ReadToEndAsync();
    var secret = cfg["ADAMSPAY_WEBHOOK_SECRET"];
    var signature = request.Headers["X-AP-Signature"].FirstOrDefault();
    if (!string.IsNullOrWhiteSpace(secret) && !sig.Verify(payload, signature, secret)) return Results.Unauthorized();
    var evt = System.Text.Json.JsonSerializer.Deserialize<WebhookEvent>(payload);
    if (evt is null) return Results.BadRequest(new { message = "Invalid payload" });
    var order = await repo.GetByExternalIdAsync(evt.ExternalId, ct);
    if (order is null) return Results.NotFound();
    order.Status = evt.Status?.ToLowerInvariant() switch
    {
        "paid" => OrderStatus.Paid, "expired" => OrderStatus
            .Expired,
        "cancelled" => OrderStatus.Cancelled, _ => order.Status
    };
    db.PaymentEvents.Add(new PaymentEvent
    {
        ExternalId = evt.ExternalId, ProviderEventId = evt.Id, EventType = evt.Type ?? "unknown", Payload = payload
    });
    await repo.SaveChangesAsync(ct);
    return Results.Ok(new { received = true });
}).WithOpenApi();
app.Run();

record OrderCreateRequest(string ExternalId, decimal Amount, string? Currency, string? Description);

record WebhookEvent(string? Type, string? Id, string ExternalId, string Status, decimal Amount);