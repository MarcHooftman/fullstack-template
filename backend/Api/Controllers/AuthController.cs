using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using Api.DTOs;

namespace Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public AuthController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        // Try to parse a JSON string into a JsonElement safely. Returns false on parse errors.
        private static bool TryParseJson(string? json, out JsonElement element)
        {
            element = default;
            if (string.IsNullOrEmpty(json)) return false;
            try
            {
                using var doc = JsonDocument.Parse(json);
                element = doc.RootElement.Clone();
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Proxy login endpoint. Forwards to the JWT issuer service configured by DI.
        /// </summary>
        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest req)
        {
            if (string.IsNullOrEmpty(req?.Email) || string.IsNullOrEmpty(req?.Password))
                return BadRequest(new { code = "invalid_request", message = "Email and password are required." });

            var client = _httpClientFactory.CreateClient("JwtIssuer");
            var content = new StringContent(JsonSerializer.Serialize(req), Encoding.UTF8, "application/json");
            try
            {
                var resp = await client.PostAsync("/api/jwt/login", content);
                var body = await resp.Content.ReadAsStringAsync();
                var ct = resp.Content.Headers.ContentType?.MediaType ?? string.Empty;
                if (!resp.IsSuccessStatusCode)
                {
                    if (TryParseJson(body, out var errEl))
                        return StatusCode((int)resp.StatusCode, errEl);

                    return StatusCode((int)resp.StatusCode, string.IsNullOrEmpty(body) ? new { message = resp.ReasonPhrase } : new { message = body });
                }

                if (ct.Contains("application/json"))
                {
                    // Forward JSON response from issuer unchanged so casing (e.g. `token`) is preserved
                    return new ContentResult { Content = body, ContentType = "application/json", StatusCode = 200 };
                }

                // If the issuer returned non-JSON body, return it as the token (legacy behavior) or as message
                if (TryParseJson(body, out var bodyEl))
                    return new ContentResult { Content = JsonSerializer.Serialize(bodyEl), ContentType = "application/json", StatusCode = 200 };

                return Ok(new { token = body });
            }
            catch (HttpRequestException ex)
            {
                return StatusCode(502, new { code = "bad_gateway", message = "Unable to reach JWT issuer service.", detail = ex.Message });
            }

        }

        /// <summary>
        /// Register via invitation. Forward to JWT issuer register endpoint and return its response.
        /// </summary>
        [HttpPost("register")]
        [AllowAnonymous]
        public async Task<IActionResult> Register([FromBody] RegisterRequest req)
        {
            var client = _httpClientFactory.CreateClient("JwtIssuer");
            var content = new StringContent(JsonSerializer.Serialize(req), Encoding.UTF8, "application/json");
            try
            {
                var resp = await client.PostAsync("/api/jwt/register", content);
                var body = await resp.Content.ReadAsStringAsync();
                if (!resp.IsSuccessStatusCode)
                {
                    if (TryParseJson(body, out var errEl))
                        return StatusCode((int)resp.StatusCode, errEl);

                    return StatusCode((int)resp.StatusCode, string.IsNullOrEmpty(body) ? new { message = resp.ReasonPhrase } : new { message = body });
                }

                // Forward issuer JSON directly so field names (like `token`) remain as the issuer returns them.
                if (resp.Content.Headers.ContentType?.MediaType?.Contains("application/json") == true)
                    return new ContentResult { Content = body, ContentType = "application/json", StatusCode = 200 };

                if (TryParseJson(body, out var bodyEl))
                    return new ContentResult { Content = JsonSerializer.Serialize(bodyEl), ContentType = "application/json", StatusCode = 200 };

                return Ok(string.IsNullOrEmpty(body) ? new { } : new { message = body });
            }
            catch (HttpRequestException ex)
            {
                return StatusCode(502, new { code = "bad_gateway", message = "Unable to reach JWT issuer service.", detail = ex.Message });
            }
        }

        /// <summary>
        /// Refresh token endpoint. Forwards refresh requests to JWT issuer.
        /// </summary>
        [HttpPost("refresh")]
        [AllowAnonymous]
        public async Task<IActionResult> Refresh([FromBody] JsonElement payload)
        {
            var client = _httpClientFactory.CreateClient("JwtIssuer");
            var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
            try
            {
                var resp = await client.PostAsync("/api/jwt/refresh", content);
                var body = await resp.Content.ReadAsStringAsync();
                if (!resp.IsSuccessStatusCode)
                {
                    if (TryParseJson(body, out var errEl))
                        return StatusCode((int)resp.StatusCode, errEl);

                    return StatusCode((int)resp.StatusCode, string.IsNullOrEmpty(body) ? new { message = resp.ReasonPhrase } : new { message = body });
                }

                if (string.IsNullOrEmpty(body)) return Ok(new { });

                if (resp.Content.Headers.ContentType?.MediaType?.Contains("application/json") == true)
                    return new ContentResult { Content = body, ContentType = "application/json", StatusCode = 200 };

                if (TryParseJson(body, out var be))
                    return new ContentResult { Content = JsonSerializer.Serialize(be), ContentType = "application/json", StatusCode = 200 };

                return Ok(new { message = body });
            }
            catch (HttpRequestException ex)
            {
                return StatusCode(502, new { code = "bad_gateway", message = "Unable to reach JWT issuer service.", detail = ex.Message });
            }
        }
    }
}