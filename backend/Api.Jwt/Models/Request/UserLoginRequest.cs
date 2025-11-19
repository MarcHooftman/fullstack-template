namespace Api.Jwt.Models;

public record UserLoginRequest(string? Email, string? Password);