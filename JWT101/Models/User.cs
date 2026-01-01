using System.Data.OleDb;
using System.Data;


namespace JWT101.Models
{
    public class User
    {
        private readonly string _connStr = "Provider=Microsoft.ACE.OLEDB.12.0;Data Source=C:\\JWT101\\myAccessDB.mdb;";
        public string Name { get; set; }
        public string Password { get; set; }
        public string Email { get; set; }

        public void CheckAndAddEmailColumn()
        {
            using (OleDbConnection conn = new OleDbConnection(_connStr))
            {
                try
                {
                    conn.Open();
                    // 強制在 Users 資料表增加 Email 欄位
                    // 如果欄位已存在，會噴出異常並被 catch 攔截，不會影響程式
                    string sql = "ALTER TABLE [Users] ADD COLUMN [Email] TEXT(255)";
                    using (OleDbCommand cmd = new OleDbCommand(sql, conn))
                    {
                        cmd.ExecuteNonQuery();
                    }

                    // 同時幫 Messages 資料表也補上，因為發文需要 UserID (Email)
                    string sql2 = "ALTER TABLE [Messages] ADD COLUMN [UserID] TEXT(255)";
                    using (OleDbCommand cmd = new OleDbCommand(sql2, conn))
                    {
                        cmd.ExecuteNonQuery();
                    }
                }
                catch (Exception ex)
                {
                    // 這裡通常是因為欄位已經存在，所以可以不處理，或者印出偵錯資訊
                    System.Diagnostics.Debug.WriteLine("資料庫結構檢查完畢或欄位已存在: " + ex.Message);
                }
            }
        }
        public Boolean Authorize(string name, string password)
        {
            Boolean ret = false;
            using (OleDbConnection conn = new OleDbConnection(_connStr))
            {
                conn.Open();
                //string SqlStr = "select * from [Users] where [Name]=@uid and [Password]=@password";
                string SqlStr = "select * from [Users] where [Name]=? and [Password]=?";
                OleDbCommand cmd = new OleDbCommand(SqlStr, conn);
                cmd.Parameters.Add(new OleDbParameter("Name", name));
                cmd.Parameters.Add(new OleDbParameter("Password", password));
                using (OleDbDataReader reader = cmd.ExecuteReader())
                {
                    if (reader != null && reader.HasRows)
                    { ret = true; }
                    else
                    { ret = false; }
                }
            }
            return ret;
        }

    }
}
