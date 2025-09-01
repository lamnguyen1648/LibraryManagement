using System.Data;
using Microsoft.Data.SqlClient;

namespace LibraryManagement.Forms.Operations
{
    /// <summary>
    /// Holds current signed-in user info for authorization decisions.
    /// Set Username right after successful login, then call EnsureLoaded().
    /// </summary>
    public static partial class UserSession
    {
        public static string? Username { get; set; }  // TenNV used at login
        public static int?    UserId   { get; private set; }
        public static int?    RoleId   { get; private set; }   // CV_ID
        public static string? RoleName { get; private set; }   // TenChucVu
        public static bool    IsActive { get; private set; } = true;

        private static bool   _loaded;

        public static void Initialize(string username)
        {
            Username = username?.Trim();
            _loaded = false;
            UserId = RoleId = null;
            RoleName = null;
            IsActive = true;
        }

        public static void EnsureLoaded()
        {
            if (_loaded || string.IsNullOrWhiteSpace(Username)) return;

            using var conn = Db.Create();
            using var cmd  = conn.CreateCommand();
            cmd.CommandText = @"
SELECT nv.NV_ID, nv.TenNV, nv.CV_ID, ISNULL(nv.Status,1) AS Status, cv.TenChucVu
FROM dbo.NhanVien nv
LEFT JOIN dbo.ChucVu cv ON nv.CV_ID = cv.CV_ID
WHERE nv.TenNV = @u;";
            cmd.Parameters.Add(new SqlParameter("@u", SqlDbType.NVarChar, 128) { Value = Username! });

            conn.Open();
            using var rd = cmd.ExecuteReader();
            if (rd.Read())
            {
                UserId   = rd["NV_ID"] is DBNull ? null : Convert.ToInt32(rd["NV_ID"]);
                RoleId   = rd["CV_ID"] is DBNull ? null : Convert.ToInt32(rd["CV_ID"]);
                RoleName = rd["TenChucVu"] as string;
                IsActive = rd["Status"] is DBNull ? true : Convert.ToBoolean(rd["Status"]);
            }
            _loaded = true;
        }
    }
}