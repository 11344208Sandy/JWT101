using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using JWT101.Models;
using JWT101.Services;
using System.Security.Claims;

namespace JWT101.Controllers
{
    [Authorize] // [關鍵] 全控制器保護：只有帶有效 Token 才能進入
    [Route("api/[controller]")]
    [ApiController]
    public class MessageController : ControllerBase
    {
        // 取得留言：GET api/Message
        [HttpGet]
        [Route("")]
        public IActionResult GetMessages()
        {
            try
            {
                DbService db = new DbService();
                var list = db.GetAllMessages(); // 從資料庫讀取所有留言
                return Ok(list); // 回傳 200 與資料
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "讀取留言失敗：" + ex.Message });
            }
        }

        // 新增留言：POST api/Message
        [HttpPost]
        public IActionResult AddMessage([FromBody] Message msg)
        {
            // 防呆：不能發空內容
            if (msg == null || string.IsNullOrEmpty(msg.Content))
            {
                return BadRequest(new { message = "留言內容不能為空" });
            }

            try
            {
                // 從使用者的 Token 中「解析」出 Email Claim
                var emailClaim = User.FindFirst(ClaimTypes.Email);
                if (emailClaim == null)
                {
                    return Unauthorized(new { message = "Token 中缺少使用者 Email 資訊" });
                }

                // 將後端解析出的 Email 自動填入 UserID 欄位
                msg.UserID = emailClaim.Value;
                msg.P_Date = DateTime.Now; // 自動填入目前伺服器時間

                DbService db = new DbService();
                bool isSuccess = db.InsertMessage(msg); // 執行寫入

                if (isSuccess)
                {
                    return Ok(new { message = "留言發布成功！" });
                }
                else
                {
                    // 這裡的錯誤現在意義不大，因為 DbService 會拋出例外
                    return BadRequest(new { message = "留言寫入失敗" });
                }
            }
            catch (Exception ex)
            {
                // 現在可以抓到來自 DbService 的更詳細錯誤
                return StatusCode(500, new { message = "伺服器錯誤：" + (ex.InnerException?.Message ?? ex.Message) });
            }
        }
    }
}