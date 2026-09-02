namespace FinanceDashboardApi.Services;

/// <summary>
/// Matches Python's base64.urlsafe_b64encode/urlsafe_b64decode exactly:
/// standard base64 with '+'/'/' swapped for '-'/'_', padding KEPT
/// (Python's variant does not strip '=' padding, unlike most other
/// "url-safe base64" conventions).
/// </summary>
public static class Base64Url
{
    public static string Encode(byte[] bytes)
    {
        return Convert.ToBase64String(bytes)
            .Replace('+', '-')
            .Replace('/', '_');
    }

    public static byte[] Decode(string text)
    {
        var standard = text.Replace('-', '+').Replace('_', '/');
        return Convert.FromBase64String(standard);
    }
}
