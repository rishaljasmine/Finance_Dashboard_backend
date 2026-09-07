namespace FinanceDashboardApi.Services.Interfaces;

public interface ITokenService
{
    string CreateToken(long userId);

    /// <summary>Returns the user id encoded in a valid, correctly-signed token; null otherwise.</summary>
    long? GetUserIdFromToken(string token);
}
