using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Api;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // Use camelCase for JSON property names to match the frontend contract
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        // Be tolerant when reading JSON from external services (case-insensitive)
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
        // Accept numeric epoch milliseconds or ISO date strings for DateTime fields from the frontend
        options.JsonSerializerOptions.Converters.Add(new Api.Common.Json.DateTimeEpochOrIsoConverter());
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "Rooster API", Version = "v1" });

    // Add JWT authentication to Swagger
    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token",
        Name = "Authorization",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// Configure Database
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
// Allow tests to opt-out of the default DB provider registration by setting environment variable
var skipDbRegistration = Environment.GetEnvironmentVariable("ROOSTER_TEST_INMEMORY") == "true";
if (!skipDbRegistration)
{
    builder.Services.AddDbContext<DbContext>(options =>
        options.UseNpgsql(connectionString));
}

// Configure JWT Authentication
var jwtKey = builder.Configuration["Jwt:Key"] ?? throw new InvalidOperationException("JWT Key not configured");
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "RoosterApp";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "RoosterApp";

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });

builder.Services.AddAuthorization();

// Configure CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(
            "http://localhost:5173",
            "http://localhost:5174",
            "http://localhost:3000",
            "http://localhost:8080",
            "http://frontend",
            "http://frontend:80"
        )
        .AllowAnyMethod()
        .AllowAnyHeader()
        .AllowCredentials();
    });
});

// Configure AutoMapper
builder.Services.AddAutoMapper(typeof(Program));


// HttpClient for JWT issuer proxy (configure base URL via JwtIssuer:BaseUrl or JwtIssuerBaseUrl env var)
// Default to the Docker Compose service host so the API container can reach the issuer.
builder.Services.AddHttpClient("JwtIssuer", client =>
{
    var jwtIssuerBase = builder.Configuration["JwtIssuer:BaseUrl"] ?? builder.Configuration["JwtIssuerBaseUrl"] ?? "http://jwt:80";
    client.BaseAddress = new Uri(jwtIssuerBase);
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Log incoming requests
app.Use(async (context, next) =>
{
    var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
    var method = context.Request.Method;
    var path = context.Request.Path;
    var queryString = context.Request.QueryString;
    var userClaim = context.User?.Identity?.Name ?? "anonymous";

    logger.LogInformation("Incoming request: {Method} {Path}{QueryString} from {User}",
        method, path, queryString, userClaim);

    await next();

    logger.LogInformation("Response: {Method} {Path} -> {StatusCode}",
        method, path, context.Response.StatusCode);
});

// Enable CORS before other middleware
app.UseCors("AllowFrontend");

// Only redirect to HTTPS in production or when properly configured
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

// Expose Program class for WebApplicationFactory in integration tests
public partial class Program { }