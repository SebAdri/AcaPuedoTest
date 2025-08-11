using Microsoft.Extensions.Configuration;
using Payments.Application.Abstractions.Payments;

namespace Payments.Infrastructure.Payments;

public class AdamsPayClient : IAdamsPayClient
{
    private readonly HttpClient _http;
    private readonly string _baseUrl;
    private readonly string? _apiKey;

    public AdamsPayClient(HttpClient http, IConfiguration cfg)
    {
        _http = http;
        _baseUrl = cfg["ADAMSPAY_BASE_URL"] ?? "https://api.adamspay.com";
        _apiKey = cfg["ADAMSPAY_API_KEY"];
    }

    public async Task<string> CreateChargeAsync(string externalId, decimal amount, string currency, string description,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(_apiKey))
            return $"https://pay.example.test/checkout/{Uri.EscapeDataString(externalId)}";
        var payload = new { external_id = externalId, amount, currency, description };
        using var req = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl.TrimEnd('/')}/charges");
        req.Headers.Add("Authorization", $"Bearer {_apiKey}");
        req.Content = new StringContent(System.Text.Json.JsonSerializer.Serialize(payload), System.Text.Encoding.UTF8,
            "application/json");
        var res = await _http.SendAsync(req, ct);
        res.EnsureSuccessStatusCode();
        using var json = await res.Content.ReadAsStreamAsync(ct);
        var doc = await System.Text.Json.JsonDocument.ParseAsync(json, cancellationToken: ct);
        return doc.RootElement.TryGetProperty("payment_url", out var url)
            ? url.GetString()!
            : throw new InvalidOperationException("payment_url not found");
    }
}