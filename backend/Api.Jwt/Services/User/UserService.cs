using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Api.Jwt;

namespace Api.Jwt.Services;

public class UserService : IUserService
{
    private readonly JwtDbContext _db;
    private readonly PasswordHasher<User> _hasher;

    public UserService(JwtDbContext db)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _hasher = new PasswordHasher<User>();
    }

    public async Task<User?> CreateUserAsync(string email, string password)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            return null;

        var normalized = email.Trim().ToLowerInvariant();
        var exists = await _db.Users.AsNoTracking().AnyAsync(u => u.Email.ToLower() == normalized);
        if (exists) return null;

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = email.Trim()
        };

        user.PasswordHash = _hasher.HashPassword(user, password);

        _db.Users.Add(user);
        await _db.SaveChangesAsync();
        return user;
    }

    public async Task<User?> ValidateCredentialsAsync(string email, string password)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            return null;

        var normalized = email.Trim();
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == normalized);
        if (user == null) return null;

        var result = _hasher.VerifyHashedPassword(user, user.PasswordHash, password);
        return result == PasswordVerificationResult.Success || result == PasswordVerificationResult.SuccessRehashNeeded
            ? user
            : null;
    }

    public async Task<string> GenerateRefreshTokenAsync(Guid userId, TimeSpan? lifetime = null)
    {
        var user = await _db.Users.FindAsync(userId);
        if (user == null) throw new InvalidOperationException("User not found");

        var raw = GenerateRandomToken(64);
        var hash = HashToken(raw);

        var refresh = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TokenHash = hash,
            ExpiresAt = DateTime.UtcNow.Add(lifetime ?? TimeSpan.FromDays(30))
        };

        _db.RefreshTokens.Add(refresh);
        await _db.SaveChangesAsync();

        return raw;
    }

    public async Task<(User user, string newRefreshToken)?> RotateRefreshTokenAsync(string oldRefreshToken)
    {
        if (string.IsNullOrWhiteSpace(oldRefreshToken)) return null;

        var hash = HashToken(oldRefreshToken);
        var existing = await _db.RefreshTokens.FirstOrDefaultAsync(rt => rt.TokenHash == hash && (rt.RevokedAt == null) && rt.ExpiresAt > DateTime.UtcNow);
        if (existing == null) return null;

        // revoke old
        existing.RevokedAt = DateTime.UtcNow;

        // create new
        var newToken = GenerateRandomToken(64);
        var newHash = HashToken(newToken);
        var refresh = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = existing.UserId,
            TokenHash = newHash,
            ExpiresAt = DateTime.UtcNow.AddDays(30)
        };

        _db.RefreshTokens.Add(refresh);
        await _db.SaveChangesAsync();

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == existing.UserId);
        return user == null ? null : (user, newToken);
    }

    public async Task<bool> RevokeRefreshTokenAsync(string refreshToken)
    {
        if (string.IsNullOrWhiteSpace(refreshToken)) return false;
        var hash = HashToken(refreshToken);
        var existing = await _db.RefreshTokens.FirstOrDefaultAsync(rt => rt.TokenHash == hash && rt.RevokedAt == null);
        if (existing == null) return false;

        existing.RevokedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<User?> GetByEmailAsync(string email)
    {
        if (string.IsNullOrWhiteSpace(email)) return null;
        return await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Email == email.Trim());
    }

    // Helpers
    private static string GenerateRandomToken(int size)
    {
        var bytes = new byte[size];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToBase64String(bytes);
    }

    private static string HashToken(string token)
    {
        using var sha = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(token);
        var hash = sha.ComputeHash(bytes);
        return Convert.ToHexString(hash);
    }
}
