using System.Security.Cryptography;
using System.Text;
using Org.BouncyCastle.Crypto.Generators;

namespace FinanceDashboardApi.Services;

/// <summary>
/// Mirrors backend/main.py's hash_password/verify_password exactly:
/// scrypt(N=16384, r=8, p=1, dklen=64) over a 16-byte random salt,
/// stored as "scrypt$" + urlsafe-base64(salt + digest). Must stay
/// byte-for-byte compatible with the Python implementation so
/// accounts created by either backend can log into both.
/// </summary>
public static class PasswordHasher
{
    private const int SaltLength = 16;
    private const int N = 16384;
    private const int R = 8;
    private const int P = 1;
    private const int DkLen = 64;

    public static string Hash(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltLength);
        var digest = SCrypt.Generate(Encoding.UTF8.GetBytes(password), salt, N, R, P, DkLen);

        var combined = new byte[salt.Length + digest.Length];
        Buffer.BlockCopy(salt, 0, combined, 0, salt.Length);
        Buffer.BlockCopy(digest, 0, combined, salt.Length, digest.Length);

        return "scrypt$" + Base64Url.Encode(combined);
    }

    public static bool Verify(string password, string? storedHash)
    {
        if (string.IsNullOrEmpty(storedHash))
        {
            return false;
        }

        try
        {
            var parts = storedHash.Split('$', 2);

            if (parts.Length != 2 || parts[0] != "scrypt")
            {
                return false;
            }

            var decoded = Base64Url.Decode(parts[1]);

            if (decoded.Length != SaltLength + DkLen)
            {
                return false;
            }

            var salt = decoded[..SaltLength];
            var expectedDigest = decoded[SaltLength..];

            var actualDigest = SCrypt.Generate(Encoding.UTF8.GetBytes(password), salt, N, R, P, DkLen);

            return CryptographicOperations.FixedTimeEquals(actualDigest, expectedDigest);
        }
        catch
        {
            return false;
        }
    }
}
