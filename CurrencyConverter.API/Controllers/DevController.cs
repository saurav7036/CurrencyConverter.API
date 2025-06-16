using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.Json;

namespace CurrencyConverter.API.Controllers
{
    [ApiController]
    [Route("api/v1/dev")]
    public class DevController : ControllerBase
    {
        private readonly IConfiguration _config;

        public DevController(IConfiguration config)
        {
            _config = config;
        }

        public class TokenRequest
        {
            public string? Username { get; set; } = "test-user";
            public Dictionary<string, bool>? Permissions { get; set; }
            public int ExpirationInSeconds { get; set; } = 15 * 60;
        }

        [HttpPost("token")]
        public IActionResult GenerateTestToken([FromBody] TokenRequest request)
        {
            var jwtKey = _config["Jwt:Key"];
            var key = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(jwtKey!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, request.Username ?? "test-user"),
                new Claim("permissions", JsonSerializer.Serialize(request.Permissions))
            };

            var token = new JwtSecurityToken(
                claims: claims,
                expires: DateTime.UtcNow.AddSeconds(request.ExpirationInSeconds),
                signingCredentials: creds
            );

            var tokenStr = new JwtSecurityTokenHandler().WriteToken(token);
            return Ok(tokenStr);
        }
    }
}
