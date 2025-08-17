using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Payments.Application.Abstractions.Payments;
using Payments.Domain.Orders;
using Payments.Infrastructure.Time;

namespace Payments.Infrastructure.Payments;

public class AdamsPayClient : IAdamsPayClient
{
    private readonly HttpClient _http;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly string _baseUrl;
    private readonly string? _apiKey;

    public AdamsPayClient(HttpClient http, IConfiguration cfg, IDateTimeProvider  dateTimeProvider)
    {
        _http = http;
        _dateTimeProvider = dateTimeProvider;
        _baseUrl = cfg["ADAMSPAY_BASE_URL"] ?? "https://api.adamspay.com";
        _apiKey = cfg["ADAMSPAY_API_KEY"];
    }

    public async Task<string> CreateChargeAsync(string externalId, decimal amount, string currency, string description,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(_apiKey))
            return $"https://pay.example.test/checkout/{Uri.EscapeDataString(externalId)}";
        // var payload = new { external_id = externalId, amount, currency, description };
        
        var payload = new
        {
            debt = new
            {
                docId = externalId,
                amount = new
                {
                    currency = currency,
                    value = amount.ToString(),  
                },
                label = description,
                validPeriod = new
                {
                    start = _dateTimeProvider.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss"),
                    end = _dateTimeProvider.UtcNow.AddDays(2).ToString("yyyy-MM-ddTHH:mm:ss")
                }
            }
        };

        
        using var req = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl.TrimEnd('/')}/api/v1/debts");
        // req.Headers.Add("Authorization", $"Bearer {_apiKey}");
        req.Headers.Add("apikey", $"{_apiKey}");
        req.Content = new StringContent(System.Text.Json.JsonSerializer.Serialize(payload), System.Text.Encoding.UTF8,
            "application/json");
        var res = await _http.SendAsync(req, ct);
        var resMessage = res.Content.ReadAsStringAsync().Result;
        var resStatusCode = res.StatusCode;
        res.EnsureSuccessStatusCode();
        
        using var json = await res.Content.ReadAsStreamAsync(ct);
        var doc = await System.Text.Json.JsonDocument.ParseAsync(json, cancellationToken: ct);
        // var asd = doc.RootElement.TryGetProperty("payUrl", out var urla);
        // return doc.RootElement.TryGetProperty("payUrl", out var url)
        //     ? url.GetString()!
        //     : throw new InvalidOperationException("payment_url not found");
        return doc.RootElement
            .GetProperty("debt")
            .GetProperty("payUrl")
            .GetString(); 
    }

    public async Task<AdamspayGetResponse> GetOrderAsync(string externalId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(_apiKey))
            throw new InvalidOperationException("ADAMSPAY_API_KEY no configurado.");

        var url = $"{_baseUrl.TrimEnd('/')}/api/v1/debts/{Uri.EscapeDataString(externalId)}";

        using var req = new HttpRequestMessage(HttpMethod.Get, url);
        // req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        req.Headers.Add("apikey", _apiKey);

        using var res = await _http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct);

        if (!res.IsSuccessStatusCode)
        {
            // Leer cuerpo (si lo hay) para diagnosticar
            var errorBody = await res.Content.ReadAsStringAsync(ct);
            throw new HttpRequestException(
                $"Adamspay GET {url} devolvió {(int)res.StatusCode} {res.ReasonPhrase}. Body: {errorBody}");
        }

        // Deserializar tipado
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true // por si cambian mayúsculas/minúsculas
        };

        var data = await res.Content.ReadFromJsonAsync<AdamspayGetResponse>(options, ct);
        if (data is null)
            throw new InvalidOperationException("No se pudo deserializar la respuesta de Adamspay.");

        return data;
    }
}