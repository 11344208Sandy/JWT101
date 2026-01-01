using JWT101.Models;
using JWT101.Services;
using System.Data.OleDb;
using System.Runtime.Versioning;
using System.IO;

namespace JWT101.Services
{
    [SupportedOSPlatform("windows")]


    public class DbService
    {
        private readonly string _connStr = "Provider=Microsoft.ACE.OLEDB.12.0;Data Source=MyAccessDB.mdb;";
        bool result = false;
        public bool Authorize(string uid, string password, string email)
        {
            
            using (OleDbConnection conn = new OleDbConnection(_connStr))
            {
                try
                {
                    conn.Open();
                    string sql = "SELECT * FROM [Users] WHERE [Name]=? AND [Password]=?"; 
                    bool result = false;
                    using (OleDbCommand cmd = new OleDbCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("?", uid);
                        cmd.Parameters.AddWithValue("?", password);
                        using (OleDbDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.HasRows) result = true;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("資料庫連線失敗：" + ex.Message);
                }
            }
            return result;
        }
        public bool Register(string username, string password, string email)
        {
            try
            {
                using (OleDbConnection conn = new OleDbConnection(_connStr))
                {
                    // 確保 SQL 順序與 Parameters 順序一致
                    string sql = "INSERT INTO [Users] ([Name], [Password], [Email]) VALUES (?, ?, ?)";
                    using (OleDbCommand cmd = new OleDbCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("?", username);
                        cmd.Parameters.AddWithValue("?", password);
                        cmd.Parameters.AddWithValue("?", email ?? ""); // 避免 email 為 null

                        conn.Open();
                        return cmd.ExecuteNonQuery() > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("註冊寫入異常：" + ex.Message);
                return false;
            }
        }
        // 取得所有留言
        public List<Message> GetAllMessages()
        {
            List<Message> list = new List<Message>();
            using (OleDbConnection conn = new OleDbConnection(_connStr))
            {
                conn.Open();
                string sql = "SELECT * FROM [Messages] ORDER BY [P_Date] DESC"; // 假設資料表叫 Messages
                OleDbCommand cmd = new OleDbCommand(sql, conn);
                using (OleDbDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        list.Add(new Message
                        {
                            Subject = reader["Subject"].ToString(),
                            Content = reader["Content"].ToString(),
                            UserID = reader["UserID"].ToString(),
                            P_Date = reader["P_Date"] != DBNull.Value ? Convert.ToDateTime(reader["P_Date"]) : DateTime.Now
                        });
                    }
                }
            }

            return list;
        }
        public bool InsertMessage(Message msg)
        {
            try
            {
                using (OleDbConnection conn = new OleDbConnection(_connStr))
                {
                    // 1. 所有的欄位名稱都加上 [ ] 以避開 Access 保留字
                    // 2. 確保 UserID 跟資料庫名稱完全一致
                    string sql = "INSERT INTO [Messages] ([Subject], [Content], [UserID], [P_Date]) VALUES (?, ?, ?, ?)";

                    using (OleDbCommand cmd = new OleDbCommand(sql, conn))
                    {
                        // 1. Subject (字串)
                        cmd.Parameters.AddWithValue("?", (object)msg.Subject ?? "");
                        // 2. Content (字串)
                        cmd.Parameters.AddWithValue("?", (object)msg.Content ?? "");
                        // 3. UserID (字串)
                        cmd.Parameters.AddWithValue("?", (object)msg.UserID ?? "Guest");
                        // 4. P_Date (日期)
                        // 我們強制給它一個乾淨的日期時間物件
                        DateTime postDate = DateTime.Now;
                        cmd.Parameters.Add("?", OleDbType.Date).Value = DateTime.Now;

                        conn.Open();
                        int rows = cmd.ExecuteNonQuery();
                        return rows > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("=== 資料庫寫入異常匯報 ===");
                System.Diagnostics.Debug.WriteLine("原因: " + ex.Message);
                return false;
            }
        }
        public bool Register(string username, string password)
        {
            try
            {
                using (System.Data.OleDb.OleDbConnection conn = new System.Data.OleDb.OleDbConnection(_connStr))
                {
                    // 這裡假設你的 Users 表欄位是 Name 和 Password
                    string sql = "INSERT INTO [Users] ([Name], [Password]) VALUES (?, ?)";
                    using (System.Data.OleDb.OleDbCommand cmd = new System.Data.OleDb.OleDbCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("?", username);
                        cmd.Parameters.AddWithValue("?", password);
                        conn.Open();
                        return cmd.ExecuteNonQuery() > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("註冊寫入異常：" + ex.Message);
                return false;
            }
        }
    }
}