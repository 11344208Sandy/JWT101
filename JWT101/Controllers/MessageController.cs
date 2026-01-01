using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using JWT101.Models;
using JWT101.Services;
using System.Security.Claims;

namespace JWT101.Controllers
{
    [Authorize] // 只有帶有有效 JWT Token 的請求才能存取這個控制器
    [Route("api/[controller]")]
    [ApiController]
    public class MessageController : ControllerBase
    {
        // 1. 取得所有留言：GET api/Message
        [HttpGet]
        [Route("")]
        public IActionResult GetMessages()
        {
            try
            {
                DbService db = new DbService();
                var list = db.GetAllMessages(); // 呼叫 DbService 撈取資料
                return Ok(list);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "讀取留言失敗：" + ex.Message });
            }
        }

        // 2. 新增一筆留言：POST api/Message
        [HttpPost]
        public IActionResult AddMessage([FromBody] Message msg)
        {
            if (msg == null || string.IsNullOrEmpty(msg.Content))
            {
                return BadRequest(new { message = "留言內容不能為空" });
            }

            try
            {
                // 從 Token 中自動抓取目前登入者的名字當作 UserID
                // ]前端就不需要手動傳 UserID，較安全
                var currentUserName = User.Identity?.Name ?? "Unknown";

                // 補齊後端該有的資訊
                msg.UserID = User.Identity?.Name ?? "Guest";
                msg.P_Date = DateTime.Now;

                DbService db = new DbService();
                bool isSuccess = db.InsertMessage(msg); // 呼叫 DbService 存入資料庫

                if (isSuccess)
                {
                    return Ok(new { message = "留言發布成功！" });
                }
                else
                {
                    return BadRequest(new { message = "留言寫入失敗" });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "伺服器錯誤：" + ex.Message });
            }
        }
    }
}