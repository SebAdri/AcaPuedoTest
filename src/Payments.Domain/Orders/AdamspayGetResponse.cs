namespace Payments.Domain.Orders;

using System.Text.Json.Serialization;

public record AdamspayGetResponse(
    [property: JsonPropertyName("meta")] Meta Meta,
    [property: JsonPropertyName("debt")] Debt Debt
);

public record Meta(
    [property: JsonPropertyName("status")] string Status,
    [property: JsonPropertyName("site")] string Site,
    [property: JsonPropertyName("now")] DateTime Now
);

public record Debt(
    [property: JsonPropertyName("docId")] string DocId,
    [property: JsonPropertyName("label")] string Label,
    [property: JsonPropertyName("slug")] string Slug,
    [property: JsonPropertyName("payUrl")] string PayUrl,
    [property: JsonPropertyName("amount")] Amount Amount,
    [property: JsonPropertyName("exchangeRate")] string? ExchangeRate,
    [property: JsonPropertyName("target")] Target Target,
    [property: JsonPropertyName("description")] string? Description,
    [property: JsonPropertyName("validPeriod")] ValidPeriod ValidPeriod,
    [property: JsonPropertyName("payStatus")] PayStatus PayStatus,
    [property: JsonPropertyName("statusHash")] string StatusHash,
    [property: JsonPropertyName("attr")] string? Attr,
    [property: JsonPropertyName("uiTheme")] string? UiTheme,
    [property: JsonPropertyName("objId")] string ObjId,
    [property: JsonPropertyName("objStatus")] ObjStatus ObjStatus,
    [property: JsonPropertyName("created")] DateTime Created,
    [property: JsonPropertyName("updated")] DateTime Updated,
    [property: JsonPropertyName("refs")] Refs Refs,
    [property: JsonPropertyName("meta")] DebtMeta DebtMeta
);

public record Amount(
    [property: JsonPropertyName("currency")] string Currency,
    [property: JsonPropertyName("value")] string Value,
    [property: JsonPropertyName("paid")] string Paid
);

public record Target(
    [property: JsonPropertyName("type")] string? Type,
    [property: JsonPropertyName("number")] string? Number,
    [property: JsonPropertyName("label")] string? Label
);

public record ValidPeriod(
    [property: JsonPropertyName("start")] DateTime Start,
    [property: JsonPropertyName("end")] DateTime End
);

public record PayStatus(
    [property: JsonPropertyName("status")] string Status,
    [property: JsonPropertyName("code")] string? Code,
    [property: JsonPropertyName("time")] DateTime Time,
    [property: JsonPropertyName("text")] string? Text
);

public record ObjStatus(
    [property: JsonPropertyName("status")] string Status,
    [property: JsonPropertyName("code")] string? Code,
    [property: JsonPropertyName("time")] DateTime Time,
    [property: JsonPropertyName("text")] string? Text
);

public record Refs(
    [property: JsonPropertyName("txList")] List<string> TxList,
    [property: JsonPropertyName("wires")] List<string> Wires
);

public record DebtMeta(
    [property: JsonPropertyName("merchantObjId")] string MerchantObjId,
    [property: JsonPropertyName("appObjId")] string AppObjId,
    [property: JsonPropertyName("firstTxObjId")] string FirstTxObjId,
    [property: JsonPropertyName("lastTxObjId")] string LastTxObjId
);
