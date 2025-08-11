using Payments.Domain.Orders.Enums;

namespace Payments.Domain.Orders;

public class Order
{
    public long Id { get; set; }
    public string ExternalId { get; set; } = default!;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "PYG";
    public OrderStatus Status { get; set; } = OrderStatus.Pending;
    public string? PaymentUrl { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}