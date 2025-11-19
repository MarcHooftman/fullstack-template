using Api.Jwt;

namespace Api.Jwt.Services;

public interface IUserService
{
    Task<User?> CreateUserAsync(string email, string password);
    Task<User?> ValidateCredentialsAsync(string email, string password);
    Task<string> GenerateRefreshTokenAsync(Guid userId, TimeSpan? lifetime = null);
    // Rotate returns the user and the newly created raw refresh token
    Task<(User user, string newRefreshToken)?> RotateRefreshTokenAsync(string oldRefreshToken);
    Task<bool> RevokeRefreshTokenAsync(string refreshToken);
    Task<User?> GetByEmailAsync(string email);
}