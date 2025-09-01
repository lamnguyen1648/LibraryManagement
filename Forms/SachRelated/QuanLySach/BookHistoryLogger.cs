using System.Data;
using LibraryManagement.Forms.Operations;
using Microsoft.Data.SqlClient;

namespace LibraryManagement.Forms.SachRelated.QuanLySach
{
    public static partial class BookHistoryLogger
    {
        // Try to read NV_ID from your session object
        private static int CurrentUserId
        {
            get
            {
                try
                {
                    // Adjust if your session holder differs (UserSession.Current.NV_ID, etc.)
                    return (int)UserSession.NV_ID!;
                }
                catch { return 0; }
            }
        }

        public static void LogInsert(SqlConnection? existingConn, int sachId, string? extra = null)
            => Write(existingConn, sachId, "Insert", extra ?? $"Thêm sách ID={sachId}");

        public static void LogDelete(SqlConnection? existingConn, int sachId, string? extra = null)
            => Write(existingConn, sachId, "Delete", extra ?? $"Xóa sách ID={sachId}");

        public static void LogUpdate(SqlConnection? existingConn, int sachId, DataRow before, DataRow after)
        {
            string details = BuildDiff(before, after);
            if (string.IsNullOrWhiteSpace(details)) details = "Cập nhật nhưng không thay đổi giá trị.";
            Write(existingConn, sachId, "Update", details);
        }

        private static void Write(SqlConnection? existingConn, int sachId, string action, string detail)
        {
            using var conn = existingConn ?? Db.Create();
            if (conn.State != ConnectionState.Open) conn.Open();

            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
INSERT INTO dbo.LichSuCapNhatSach (NV_ID, SACH_ID, HinhThucCapNhat, ChiTietCapNhat)
VALUES (@nv, @s, @act, @detail);";
            cmd.Parameters.Add(new SqlParameter("@nv", SqlDbType.Int) { Value = CurrentUserId });
            cmd.Parameters.Add(new SqlParameter("@s",  SqlDbType.Int) { Value = sachId });
            cmd.Parameters.Add(new SqlParameter("@act", SqlDbType.NVarChar, 20) { Value = action });
            cmd.Parameters.Add(new SqlParameter("@detail", SqlDbType.NVarChar, -1) { Value = (object)detail ?? DBNull.Value });
            cmd.ExecuteNonQuery();
        }

        private static string BuildDiff(DataRow before, DataRow after)
        {
            static string VN(string col) => col.ToLowerInvariant() switch
            {
                "tensach"      => "Tên sách",
                "namxuatban"   => "Năm xuất bản",
                "nxb_id"       => "Nhà Xuất Bản",
                "tg_id"        => "Tác Giả",
                "tl_id"        => "Thể Loại",
                _              => col
            };

            var parts = new System.Collections.Generic.List<string>();
            foreach (DataColumn c in before.Table.Columns)
            {
                if (string.Equals(c.ColumnName, "Sach_ID", StringComparison.OrdinalIgnoreCase))
                    continue;

                object b = before[c.ColumnName];
                object a = after[c.ColumnName];
                string bs = b == DBNull.Value ? "NULL" : b.ToString();
                string as_ = a == DBNull.Value ? "NULL" : a.ToString();

                if (!string.Equals(bs, as_, StringComparison.Ordinal))
                    parts.Add($"{VN(c.ColumnName)}: {bs} -> {as_}");
            }
            return parts.Count == 0 ? "" : "Cập nhật: " + string.Join("; ", parts);
        }
    }
}
