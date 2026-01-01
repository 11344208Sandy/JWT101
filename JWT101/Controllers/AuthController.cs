using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using JWT101.Services;
using JWT101.Models;

namespace JWT101.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public AuthController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public class LoginRequest
        {
            public string name { get; set; }
            public string password { get; set; }
            public string? email { get; set; } // 新增此欄位接收前端資料

        }

        // POST api/Auth/login
        [HttpPost("login")]
        public IActionResult Login([FromBody] LoginRequest request)
        {
            // ** 1. 驗證用戶憑證 **
            DbService db = new DbService();

            // 【修正 2】傳入參數改為 request.name 與 request.password
            if (!db.Authorize(request.name, request.password, request.email))
            {
                // 回傳格式也要包含 message，前端才好顯示
                return Unauthorized(new { message = "帳號或密碼錯誤（資料庫驗證失敗）" });
            }

            // ** 2. 生成 JWT **
            var token = GenerateJwtToken(request.name, "User");
            return Ok(new { token = token });
        }

        [HttpPost("register")]
        public IActionResult Register([FromBody] LoginRequest request)
        {
            if (string.IsNullOrEmpty(request.name) || string.IsNullOrEmpty(request.password))
                return BadRequest(new { message = "帳號密碼不可為空" });

            DbService db = new DbService();

            // 【修正點】呼叫 Register 時傳入 request.email
            if (db.Register(request.name, request.password, request.email))
            {
                return Ok(new { message = "註冊成功！" });
            }
            return BadRequest(new { message = "註冊失敗，帳號可能重複" });
        }


        private string GenerateJwtToken(string username, string role)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, username),
                new Claim(ClaimTypes.Role, role)
            };

            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddMinutes(30),
                Issuer = _configuration["Jwt:Issuer"],
                Audience = _configuration["Jwt:Audience"],
                SigningCredentials = credentials
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);

            return tokenHandler.WriteToken(token);
        }
    }
}