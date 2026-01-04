
namespace JWT101.Models
{
    public class Message
    {
        // 加上 ? 或給予預設值，否則前端沒傳會報 400
        public string? Subject { get; set; } = "";
        public string? Content { get; set; } = "";
        public string? UserID { get; set; } = "";
        public DateTime? P_Date { get; set; }
    }
}

