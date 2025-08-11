namespace Payments.Application.Abstractions.Security;
public interface ISignatureVerifier{bool Verify(string payload, string? signatureHeader, string secret);}