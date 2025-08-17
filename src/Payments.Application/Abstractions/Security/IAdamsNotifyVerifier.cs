namespace Payments.Application.Abstractions.Security;

public interface IAdamsNotifyVerifier
{
    bool Verify(string rawBody, string? receivedHash, string secret);
}