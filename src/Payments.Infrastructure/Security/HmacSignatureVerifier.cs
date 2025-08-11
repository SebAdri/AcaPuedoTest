using System.Security.Cryptography; using System.Text; using Payments.Application.Abstractions.Security;
namespace Payments.Infrastructure.Security; public class HmacSignatureVerifier:ISignatureVerifier{
public bool Verify(string payload, string? signatureHeader, string secret){ if(string.IsNullOrEmpty(signatureHeader)) return false;
using var h=new HMACSHA256(Encoding.UTF8.GetBytes(secret)); var hash=h.ComputeHash(Encoding.UTF8.GetBytes(payload)); var hex=Convert.ToHexString(hash).ToLowerInvariant(); return string.Equals(hex, signatureHeader, StringComparison.OrdinalIgnoreCase);} }