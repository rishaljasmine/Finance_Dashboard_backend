using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using FinanceDashboardApi.Services.Interfaces;

namespace FinanceDashboardApi.Services;

/// <summary>
/// base64url(JSON payload) + "." + hex(HMAC-SHA256) opaque token, signed
/// with AUTH_SECRET. Not a JWT — kept intentionally simple/self-contained.
/// </summary>
public class TokenService(string authSecret) : ITokenService
{
    private readonly byte[] _secretBytes = Encoding.UTF8.GetBytes(authSecret);

    private static readonly JsonSerializerOptions PayloadJsonOptions = new()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public string CreateToken(long userId)
    {
        var payload = new
        {
            user_id = userId,
            created_at = DateTimeOffset.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.ffffff+00:00")
        };

        var payloadJson = JsonSerializer.Serialize(payload, PayloadJsonOptions);
        var payloadEncoded = Base64Url.Encode(Encoding.UTF8.GetBytes(payloadJson));
        var signature = Sign(payloadEncoded);

        return $"{payloadEncoded}.{signature}";
    }

    public long? GetUserIdFromToken(string token)
    {
        var dotIndex = token.IndexOf('.');
        if (dotIndex < 0) return null;

        var payloadEncoded = token[..dotIndex];
        var signature = token[(dotIndex + 1)..];
        var expectedSignature = Sign(payloadEncoded);

        if (!CryptographicOperations.FixedTimeEquals(
                Encoding.ASCII.GetBytes(signature.ToLowerInvariant()),
                Encoding.ASCII.GetBytes(expectedSignature)))
        {
            return null;
        }

        try
        {
            var payloadJson = Encoding.UTF8.GetString(Base64Url.Decode(payloadEncoded));
            using var doc = JsonDocument.Parse(payloadJson);
            return doc.RootElement.GetProperty("user_id").GetInt64();
        }
        catch
        {
            return null;
        }
    }

    private string Sign(string payloadEncoded)
    {
        var hash = HMACSHA256.HashData(_secretBytes, Encoding.UTF8.GetBytes(payloadEncoded));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
