using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// --- 【新增】 1. 設定 CORS 策略，允許前端網頁存取 API ---
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

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"])),
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidateAudience = true,
            ValidAudience = builder.Configuration["Jwt:Audience"],
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    });

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    // 這裡呼叫你的 DbService 或是直接寫 SQL
    try
    {
        // 假設你的連線字串在 configuration 裡
        string connStr = builder.Configuration.GetConnectionString("DefaultConnection");
        using (var conn = new System.Data.OleDb.OleDbConnection("你的連線字串"))
        {
            conn.Open();
            // 強制幫 Users 表加 Email
            var cmd = new System.Data.OleDb.OleDbCommand("ALTER TABLE [Messages] ADD COLUMN [Email] TEXT(255)", conn);
            cmd.ExecuteNonQuery();
        }
    }
    catch { /* 欄位已存在，忽略錯誤 */ }

}

// --- 【新增】 2. 啟用靜態檔案支援 (讓 wwwroot 裡的 HTML 可以顯示) ---
app.UseDefaultFiles(); // 預設尋找 index.html
app.UseStaticFiles();  // 啟用靜態檔案存取

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseRouting();

// --- 【新增】 3. 啟用 CORS (必須放在 Authentication 之前) ---
app.UseCors("AllowAll");

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();