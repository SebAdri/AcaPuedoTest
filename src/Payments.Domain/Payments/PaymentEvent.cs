namespace Payments.Domain.Payments;

public class PaymentEvent
{
    public long Id { get; set; }
    public string ExternalId { get; set; } = default!;
    public string? ProviderEventId { get; set; }
    public string EventType { get; set; } = default!;
    public string Payload { get; set; } = default!;
    public DateTime ReceivedAt { get; set; } = DateTime.UtcNow;
}