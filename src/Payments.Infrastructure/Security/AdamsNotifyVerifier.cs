namespace Payments.Application.Abstractions.Security;

using System.Security.Cryptography;
using System.Text;

public sealed class AdamsNotifyVerifier : IAdamsNotifyVerifier
{
    public bool Verify(string rawBody, string? receivedHash, string secret)
    {
        if (string.IsNullOrWhiteSpace(receivedHash)) return false;
        var input = "adams" + rawBody + secret;

        using var md5 = MD5.Create();
        var bytes = md5.ComputeHash(Encoding.UTF8.GetBytes(input));
        var expected = Convert.ToHexString(bytes).ToLowerInvariant();

        return ConstantTimeEquals(expected, receivedHash.Trim().ToLowerInvariant());
    }

    private static bool ConstantTimeEquals(string a, string b)
    {
        if (a.Length != b.Length) return false;
        var diff = 0;
        for (int i = 0; i < a.Length; i++)
            diff |= a[i] ^ b[i];
        return diff == 0;
    }
}
