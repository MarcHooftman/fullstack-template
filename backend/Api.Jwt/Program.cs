using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using Api.Jwt.Models;
using Api.Jwt.Services;
using Microsoft.EntityFrameworkCore;

namespace Api.Jwt;

public partial class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.
        // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
        builder.Services.AddOpenApi();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddControllers();
        builder.Services.AddSwaggerGen();
        builder.Services.AddSingleton<IConfiguration>(builder.Configuration);

        var jwtConn = builder.Configuration.GetConnectionString("JwtConnection")
              ?? Environment.GetEnvironmentVariable("JWT_CONNECTION");
        builder.Services.AddDbContext<JwtDbContext>(o => o.UseNpgsql(jwtConn));
        // Register user service for user/refresh-token management
        builder.Services.AddScoped<IUserService, UserService>();

        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseHttpsRedirection();

        // Frontend-compatible login endpoint - POST /api/jwt/login
        // Accepts { email, password } and returns { token, refreshToken } on success.
        app.MapPost("/api/jwt/login", async (UserLoginRequest req, IConfiguration config, IUserService userService) =>
        {
            if (string.IsNullOrWhiteSpace(req?.Email) || string.IsNullOrWhiteSpace(req?.Password))
                return Results.BadRequest(new { message = "Email and password are required." });

            // Try to validate against stored users first
            var user = await userService.ValidateCredentialsAsync(req.Email, req.Password);

            // Fallback to seeded super-admin for dev convenience
            if (user == null && string.Equals(req.Email?.Trim(), "super@admin.local", StringComparison.OrdinalIgnoreCase) && req.Password == "supersecret")
            {
                user = new User { Id = Guid.NewGuid(), Email = req.Email?.Trim() ?? "super@admin.local" };
            }

            if (user == null) return Results.Unauthorized();

            var issuer = config["Jwt:Issuer"] ?? "Issuer";
            var audience = config["Jwt:Audience"] ?? "Audience";
            var keyString = config["Jwt:Key"] ?? throw new InvalidOperationException("Jwt:Key not configured for issuer");
            var keyBytes = System.Text.Encoding.UTF8.GetBytes(keyString);
            var signingKey = new SymmetricSecurityKey(keyBytes);
            var creds = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Email ?? string.Empty),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim("role", "user")
            };

            var token = new JwtSecurityToken(issuer, audience, claims,
                expires: DateTime.UtcNow.AddHours(8), signingCredentials: creds);

            var jwt = new JwtSecurityTokenHandler().WriteToken(token);
            var refresh = await userService.GenerateRefreshTokenAsync(user.Id);

            return Results.Ok(new { token = jwt, refreshToken = refresh });
        });

        // Simple register endpoint for development: POST /api/jwt/register
        // Accepts { email, password } and returns { token } for a newly "created" user.
        app.MapPost("/api/jwt/register", async (UserLoginRequest req, IConfiguration config, IUserService userService) =>
        {
            if (string.IsNullOrWhiteSpace(req?.Email) || string.IsNullOrWhiteSpace(req?.Password))
                return Results.BadRequest(new { message = "Email and password are required." });

            var created = await userService.CreateUserAsync(req.Email.Trim(), req.Password);
            if (created == null)
                return Results.Conflict(new { message = "User already exists or invalid data." });

            var issuer = config["Jwt:Issuer"] ?? "Issuer";
            var audience = config["Jwt:Audience"] ?? "Audience";
            var keyString = config["Jwt:Key"] ?? throw new InvalidOperationException("Jwt:Key not configured for issuer");
            var keyBytes = System.Text.Encoding.UTF8.GetBytes(keyString);
            var signingKey = new SymmetricSecurityKey(keyBytes);
            var creds = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, created.Email),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim("role", "user")
            };

            var token = new JwtSecurityToken(issuer, audience, claims,
                expires: DateTime.UtcNow.AddHours(8), signingCredentials: creds);

            var jwt = new JwtSecurityTokenHandler().WriteToken(token);
            var refresh = await userService.GenerateRefreshTokenAsync(created.Id);

            return Results.Created(string.Empty, new { token = jwt, refreshToken = refresh });
        });

        // Refresh endpoint: POST /api/jwt/refresh
        // Accepts a payload containing an existing token (key names: "token", "accessToken", or "refreshToken")
        // and returns a renewed access token. This is intentionally permissive for local/dev use.
        app.MapPost("/api/jwt/refresh", async (System.Text.Json.JsonElement payload, IConfiguration config, IUserService userService) =>
        {
            string? incoming = null;
            if (payload.ValueKind == System.Text.Json.JsonValueKind.String)
            {
                incoming = payload.GetString();
            }
            else if (payload.ValueKind == System.Text.Json.JsonValueKind.Object)
            {
                if (payload.TryGetProperty("token", out var t) && t.ValueKind == System.Text.Json.JsonValueKind.String)
                    incoming = t.GetString();
                else if (payload.TryGetProperty("accessToken", out var a) && a.ValueKind == System.Text.Json.JsonValueKind.String)
                    incoming = a.GetString();
                else if (payload.TryGetProperty("refreshToken", out var r) && r.ValueKind == System.Text.Json.JsonValueKind.String)
                    incoming = r.GetString();
            }

            if (string.IsNullOrWhiteSpace(incoming))
                return Results.BadRequest(new { message = "No token provided for refresh." });

            try
            {
                // Treat incoming as a refresh token and rotate it
                var rotated = await userService.RotateRefreshTokenAsync(incoming);
                if (rotated == null) return Results.Unauthorized();

                var (user, newRefresh) = rotated.Value;

                var issuer = config["Jwt:Issuer"] ?? "Issuer";
                var audience = config["Jwt:Audience"] ?? "Audience";
                var keyString = config["Jwt:Key"] ?? throw new InvalidOperationException("Jwt:Key not configured for issuer");
                var keyBytes = System.Text.Encoding.UTF8.GetBytes(keyString);
                var signingKey = new SymmetricSecurityKey(keyBytes);
                var creds = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

                var claims = new[]
                {
                    new Claim(JwtRegisteredClaimNames.Sub, user.Email ?? string.Empty),
                    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                    new Claim("role", "user")
                };

                var token = new JwtSecurityToken(issuer, audience, claims,
                    expires: DateTime.UtcNow.AddHours(8), signingCredentials: creds);

                var jwt = new JwtSecurityTokenHandler().WriteToken(token);
                return Results.Ok(new { token = jwt, refreshToken = newRefresh });
            }
            catch (Exception)
            {
                return Results.Unauthorized();
            }
        });

        app.Run();
    }
}