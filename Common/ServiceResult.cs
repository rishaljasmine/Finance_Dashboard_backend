namespace FinanceDashboardApi.Common;

/// <summary>
/// Lets services report "this failed with HTTP status X and this message"
/// without either throwing exceptions for expected failures (409 duplicate
/// email, 401 bad password) or leaking HTTP concerns (Results.Problem) into
/// the service layer.
/// </summary>
public class ServiceResult<T>
{
    public bool Success { get; private init; }
    public T? Data { get; private init; }
    public int ErrorStatusCode { get; private init; }
    public string? ErrorMessage { get; private init; }

    public static ServiceResult<T> Ok(T data) => new() { Success = true, Data = data };

    public static ServiceResult<T> Fail(int statusCode, string message) =>
        new() { Success = false, ErrorStatusCode = statusCode, ErrorMessage = message };
}
