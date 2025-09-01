using System;
using System.Data;
using Microsoft.Data.SqlClient;

namespace LibraryManagement
{
    public interface IAuthService
    {
        bool ValidateCredentials(string username, string password);
    }

    /// <summary>
    /// Central place to obtain SqlConnection for the whole app.
    /// </summary>
    public static class Db
    {
        // Keep a single source of truth for the connection string.
        // Adjust as needed for your environment.
        public const string ConnStr =
            "server=localhost\\SQLEXPRESS;database=LibraryManagement;user id=sa;password=16482548@a;encrypt=true;trustservercertificate=true;";

        public static SqlConnection Create() => new SqlConnection(ConnStr);
    }

    public sealed class SqlAuthService : IAuthService
    {
        public bool ValidateCredentials(string username, string password)
        {
            if (string.IsNullOrWhiteSpace(username) || password is null) return false;

            // IMPORTANT: treat disabled accounts as non-matches (same as wrong credentials)
            const string sql = @"
select MatKhau
from dbo.NhanVien
where TenNV = @u
  and ISNULL(Status, 1) = 1;"; // if Status is null, treat as active; disabled (0) won't match

            using var conn = Db.Create();
            using var cmd  = new SqlCommand(sql, conn);
            cmd.Parameters.Add(new SqlParameter("@u", SqlDbType.NVarChar, 128) { Value = username.Trim() });

            conn.Open();
            var dbValue = cmd.ExecuteScalar();

            if (dbValue is not string storedHash || string.IsNullOrWhiteSpace(storedHash))
                return false;

            bool needsRehash;
            bool ok = PasswordHasher.Verify(password, storedHash, out needsRehash);

            if (ok && needsRehash)
            {
                TryUpgradeHash(conn, username.Trim(), password);
            }

            return ok;
        }

        private static void TryUpgradeHash(SqlConnection openConn, string username, string password)
        {
            try
            {
                string newHash = PasswordHasher.HashPassword(password);
                using var up = new SqlCommand(
                    @"update dbo.NhanVien set MatKhau = @h where TenNV = @u", openConn);
                up.Parameters.Add(new SqlParameter("@h", SqlDbType.NVarChar, -1) { Value = newHash });
                up.Parameters.Add(new SqlParameter("@u", SqlDbType.NVarChar, 128) { Value = username });
                up.ExecuteNonQuery();
            }
            catch
            {
                // best-effort; ignore
            }
        }
    }
}
