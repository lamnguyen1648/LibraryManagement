using System;
using System.Data;
using Microsoft.Data.SqlClient;

namespace LibraryManagement
{
    public interface IAuthService
    {
        bool ValidateCredentials(string username, string password);
    }

    public static class Db
    {
        public const string ConnStr =
            "server=localhost\\SQLEXPRESS;database=LibraryManagement;user id=sa;password=16482548@a;encrypt=true;trustservercertificate=true;";

        public static SqlConnection Create() => new SqlConnection(ConnStr);
    }

    public static class UserSession
    {
        public static int NV_ID { get; private set; }
        public static string TenNV { get; private set; } = "";
        public static int? CV_ID { get; private set; }
        public static string? TenChucVu { get; private set; }
        public static bool IsActive { get; private set; }

        public static void Set(int nvId, string tenNv, int? cvId, string? tenChucVu, bool isActive)
        {
            NV_ID = nvId;
            TenNV = tenNv ?? "";
            CV_ID = cvId;
            TenChucVu = tenChucVu;
            IsActive = isActive;
        }

        public static void Clear()
        {
            NV_ID = 0;
            TenNV = "";
            CV_ID = null;
            TenChucVu = null;
            IsActive = false;
        }

        public static bool IsAdmin =>
            (TenChucVu != null && TenChucVu.Equals("Admin", StringComparison.OrdinalIgnoreCase)) || (CV_ID == 1);
    }

    public sealed class SqlAuthService : IAuthService
    {
        public bool ValidateCredentials(string username, string password)
        {
            if (string.IsNullOrWhiteSpace(username) || password is null)
            {
                UserSession.Clear();
                return false;
            }

            const string sql = @"
SELECT  nv.NV_ID,
        nv.TenNV,
        nv.MatKhau,
        nv.CV_ID,
        ISNULL(nv.Status, 1) AS Status,
        COALESCE(cv.TenChucVu, cv.TenChucVu) AS TenChucVu
FROM dbo.NhanVien nv
LEFT JOIN dbo.ChucVu cv ON cv.CV_ID = nv.CV_ID
WHERE (nv.TenNV = @u OR nv.Mail = @u);";

            using var conn = Db.Create();
            using var cmd  = new SqlCommand(sql, conn);
            cmd.Parameters.Add(new SqlParameter("@u", SqlDbType.NVarChar, 128) { Value = username.Trim() });

            conn.Open();
            using var rd = cmd.ExecuteReader();
            if (!rd.Read())
            {
                UserSession.Clear();
                return false;
            }

            var storedHash = rd["MatKhau"] as string ?? "";
            var active     = rd["Status"] == DBNull.Value || Convert.ToBoolean(rd["Status"]);
            bool needsRehash = false;
            bool ok = !string.IsNullOrWhiteSpace(storedHash) &&
                      PasswordHasher.Verify(password, storedHash, out needsRehash);

            if (!ok || !active)
            {
                UserSession.Clear();
                return false;
            }

            int    nvId      = Convert.ToInt32(rd["NV_ID"]);
            string tenNv     = rd["TenNV"] as string ?? "";
            int?   cvId      = rd["CV_ID"] == DBNull.Value ? null : (int?)Convert.ToInt32(rd["CV_ID"]);
            string? tenChucVu= rd["TenChucVu"] == DBNull.Value ? null : rd["TenChucVu"].ToString();

            UserSession.Set(nvId, tenNv, cvId, tenChucVu, isActive: true);

            rd.Close();

            if (needsRehash)
            {
                TryUpgradeHash(conn, nvId, PasswordHasher.HashPassword(password));
            }

            return true;
        }

        private static void TryUpgradeHash(SqlConnection openConn, int nvId, string newHash)
        {
            try
            {
                using var up = new SqlCommand(
                    "UPDATE dbo.NhanVien SET MatKhau = @h WHERE NV_ID = @id", openConn);
                up.Parameters.Add(new SqlParameter("@h", SqlDbType.NVarChar, -1) { Value = newHash });
                up.Parameters.Add(new SqlParameter("@id", SqlDbType.Int) { Value = nvId });
                up.ExecuteNonQuery();
            }
            catch
            {
            }
        }
    }
}
