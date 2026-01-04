using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// 設定 CORS (跨來源資源共用)，讓前端 HTML 可以呼叫後端 API
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()   // 允許任何來源
              .AllowAnyMethod()   // 允許任何方法 (GET, POST...)
              .AllowAnyHeader();  // 允許任何標頭
    });
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// 設定 JWT 驗證邏輯
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,// 必須驗證簽名
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"])),
            ValidateIssuer = true, // 驗證發行者
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidateAudience = true, // 驗證接收者
            ValidAudience = builder.Configuration["Jwt:Audience"],
            ValidateLifetime = true, // 驗證 Token 是否過期
            ClockSkew = TimeSpan.Zero // 過期時間零容忍 (時間一到立刻失效)
        };
    });

var app = builder.Build();

app.UseDefaultFiles(); // 支援 index.html 預設頁面
app.UseStaticFiles();  // 支援存取 wwwroot 內的 HTML/CSS/JS

// 檢查目前是否為「開發模式」 (Development)
if (app.Environment.IsDevelopment())
{
    // 啟用 Swagger 生成器，它會根據你的 Controller 自動產生 API 文件說明檔 (JSON/YAML)
    app.UseSwagger();

    // 啟用 Swagger 的網頁介面 (UI)，讓你可以直接在瀏覽器輸入 /swagger 測試 API
    app.UseSwaggerUI(); 
}

app.UseHttpsRedirection(); // 自動將 HTTP 請求重新導向至更安全的 HTTPS
app.UseRouting(); // 啟用路由匹配功能

// --- 【新增】 3. 啟用 CORS (必須放在 Authentication 之前) ---
app.UseCors("AllowAll");

app.UseAuthentication(); // 啟用身份驗證 (誰是誰)
app.UseAuthorization(); // 啟用授權 (誰能做什麼)
app.MapControllers(); // 對應 Controller 路徑

app.Run();app.Run(); // 啟動伺服器