namespace FinanceDashboardApi.Services;

/// <summary>
/// Standard base64 with '+'/'/' swapped for '-'/'_', padding KEPT.
/// </summary>
public static class Base64Url
{
    public static string Encode(byte[] bytes) =>
        Convert.ToBase64String(bytes).Replace('+', '-').Replace('/', '_');

    public static byte[] Decode(string text) =>
        Convert.FromBase64String(text.Replace('-', '+').Replace('_', '/'));
}
