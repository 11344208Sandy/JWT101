using JWT101.Models;
using JWT101.Services;
using System.Data.OleDb;
using System.Runtime.Versioning;
using System.IO;

namespace JWT101.Services
{
    [SupportedOSPlatform("windows")] // 標記僅在 Windows 執行（因為 Access 驅動程式限制）


    public class DbService
    {
        // 定義 Access 資料庫連線字串
        private readonly string _connStr = "Provider=Microsoft.ACE.OLEDB.12.0;Data Source=MyAccessDB.mdb;";
        
        // 登入驗證：檢查帳號密碼是否存在
        public bool Authorize(string uid, string password)
        {
            bool result = false;
            using (OleDbConnection conn = new OleDbConnection(_connStr))
            {
                try
                {
                    conn.Open(); // 開啟連線
                    string sql = "SELECT * FROM [Users] WHERE [Name]=? AND [Password]=?"; 
                    using (OleDbCommand cmd = new OleDbCommand(sql, conn))
                    {
                        // 使用參數化查詢防止 SQL 注入攻擊
                        cmd.Parameters.AddWithValue("?", uid);
                        cmd.Parameters.AddWithValue("?", password);
                        using (OleDbDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.HasRows) result = true; // 若有資料代表驗證成功
                        }
                    }
                    conn.Close(); // 確保連線被關閉
                }
                catch (Exception ex)
                {
                    Console.WriteLine("資料庫連線失敗：" + ex.Message);
                }
            }
            return result;
        }

        public User GetUserByName(string username)
        {
            User user = null;
            using (OleDbConnection conn = new OleDbConnection(_connStr))
            {
                try
                {
                    conn.Open();
                    string sql = "SELECT * FROM [Users] WHERE [Name]=?";
                    using (OleDbCommand cmd = new OleDbCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("?", username);
                        using (OleDbDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                user = new User
                                {
                                    Id = Convert.ToInt32(reader["UserID"]),
                                    Name = reader["Name"].ToString(),
                                    Password = reader["Password"].ToString(),
                                    Email = reader["Email"] != DBNull.Value ? reader["Email"].ToString() : string.Empty
                                };
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("查詢使用者失敗：" + ex.Message);
                }
            }
            return user;
        }

        // 註冊功能：包含重複檢查
        public bool Register(string name, string password, string email)
        {
            using (OleDbConnection conn = new OleDbConnection(_connStr))
            {
                try
                {
                    conn.Open();

                    // 1. 檢查重複性
                    // 同時檢查 Name 或 Email 是否已經有人用過
                    string checkSql = "SELECT COUNT(*) FROM [Users] WHERE [Name]=? OR [Email]=?";
                    using (OleDbCommand checkCmd = new OleDbCommand(checkSql, conn))
                    {
                        checkCmd.Parameters.AddWithValue("?", name);
                        checkCmd.Parameters.AddWithValue("?", email);

                        int count = Convert.ToInt32(checkCmd.ExecuteScalar());
                        if (count > 0)
                        {
                            // 拋出具體錯誤，這樣 Controller 才能抓到並回傳給前端
                            throw new Exception("帳號名稱或 Email 已經被註冊過了！");
                        }
                    }

                    // 2. 執行註冊 
                    string sql = "INSERT INTO [Users] ([Name], [Password], [Email]) VALUES (?, ?, ?)";
                    using (OleDbCommand cmd = new OleDbCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("?", name);
                        cmd.Parameters.AddWithValue("?", password);
                        cmd.Parameters.AddWithValue("?", email);
                        return cmd.ExecuteNonQuery() > 0;
                    }
                }
                catch (Exception ex)
                {
                    // 將錯誤訊息往上層傳遞
                    throw new Exception(ex.Message);
                }
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

        // 插入新留言
        public bool InsertMessage(Message msg)
        {
            try
            {
                using (OleDbConnection conn = new OleDbConnection(_connStr))
                {
                    string sql = "INSERT INTO [Messages] ([Subject], [Content], [UserID], [P_Date]) VALUES (?, ?, ?, ?)";

                    using (OleDbCommand cmd = new OleDbCommand(sql, conn))
                    {
                        // 處理空值避免資料庫報錯
                        cmd.Parameters.AddWithValue("?", (object)msg.Subject ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("?", (object)msg.Content ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("?", (object)msg.UserID ?? DBNull.Value);
                        // 指定日期型態，解決 Access Date 格式報錯問題
                        cmd.Parameters.Add("?", OleDbType.Date).Value = msg.P_Date;

                        conn.Open();
                        return cmd.ExecuteNonQuery() > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                // 將詳細的資料庫錯誤訊息往上拋，而不是只回傳 false
                System.Diagnostics.Debug.WriteLine("=== 資料庫寫入異常匯報 ===");
                System.Diagnostics.Debug.WriteLine("原因: " + ex.Message);
                throw new Exception("資料庫寫入異常：" + ex.Message, ex);
            }
        }
    }
}