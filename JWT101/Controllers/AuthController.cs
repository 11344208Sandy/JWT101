using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using JWT101.Services;
using JWT101.Models;

namespace JWT101.Controllers
{
    [Route("api/[controller]")] // 定義路徑為 api/Auth
    [ApiController] // 標記為 Web API 控制器
    public class AuthController : ControllerBase
    {
        private readonly IConfiguration _configuration; // 用於讀取 appsettings.json 的設定

        public AuthController(IConfiguration configuration)
        {
            _configuration = configuration; // 透過依賴注入取得設定檔
        }

        // 登入請求
        public class LoginRequest
        {
            public string name { get; set; }
            public string password { get; set; }
        }

        // 註冊請求
        public class RegisterRequest
        {
            public string name { get; set; }
            public string password { get; set; }
            public string email { get; set; }
        }

        // 登入 API：POST api/Auth/login
        [HttpPost("login")]
        [Consumes("application/json") // 指定接收 JSON 格式
        public IActionResult Login([FromBody] LoginRequest request)
        {
            // 1. 驗證帳號密碼是否正確
            DbService db = new DbService(); // 初始化資料庫服務

            if (!db.Authorize(request.name, request.password))
            {
                return Unauthorized(new { message = "帳號或密碼錯誤" });
            }

            // 2. 驗證成功，取得該使用者完整資料
            var user = db.GetUserByName(request.name);
            if (user == null)
            {
                return NotFound(new { message = "找不到使用者資料" });
            }

            // 3. 產生 JWT Token 回傳給前端儲存
            var token = GenerateJwtToken(user);
            return Ok(new { token = token });
        }

        // 註冊 API：POST api/Auth/register
        [HttpPost("register")]
        [Consumes("application/json")]
        public IActionResult Register([FromBody] RegisterRequest request)
        {
            // 基本防呆檢查
            if (string.IsNullOrEmpty(request.name) || string.IsNullOrEmpty(request.password))
                return BadRequest(new { message = "帳號密碼不可為空" });

            DbService db = new DbService();

            try
            {
                // 執行註冊邏輯（內部含重複檢查）
                if (db.Register(request.name, request.password, request.email))
                {
                    return Ok(new { message = "註冊成功！" });
                }
                return BadRequest(new { message = "註冊失敗，請稍後再試" });
            }
            catch (Exception ex)
            {
                // 捕捉來自 DbService 的自定義錯誤訊息（如：帳號已存在）
                return BadRequest(new { message = ex.Message });
            }

        }

        // 產生 JWT Token 的核心邏輯 產生 JWT 
        private string GenerateJwtToken(User user)
        {
            // 設定 Payload (有效負載)，將使用者資訊塞入 Token
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.Name),
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()), // 儲存數字 ID
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, "User") // 假設角色
            };

            // 取得金鑰與加密憑證
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddMinutes(30), // 30分鐘後過期
                Issuer = _configuration["Jwt:Issuer"], // 發行者
                Audience = _configuration["Jwt:Audience"], // 接收者
                SigningCredentials = credentials
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);

            return tokenHandler.WriteToken(token); // 轉換為字串回傳
        }
    }
}