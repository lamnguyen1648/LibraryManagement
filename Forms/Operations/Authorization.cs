using System.Globalization;
using System.Text;

namespace LibraryManagement.Forms.Operations
{
    public static partial class Authorization
    {
        /// <summary>
        /// Feature keys you can reuse around the app.
        /// </summary>
        public const string FeatureQuanLyNhanVien = "QuanLyNhanVien";

        /// <summary>
        /// Central access check by feature key.
        /// Default policy: Only Admin/Quản trị can access Quản lý nhân viên.
        /// </summary>
        public static bool CanAccess(string featureKey)
        {
            var role = UserSession.TenChucVu ?? "";
            var roleNorm = Normalize(role);

            switch (featureKey)
            {
                case FeatureQuanLyNhanVien:
                    // Allow a few common admin variants. Tweak as you like.
                    string[] allowed = { "admin", "quan tri", "administrator", "sysadmin" };
                    return allowed.Contains(roleNorm);

                default:
                    // Not specified => allow
                    return true;
            }
        }

        private static string Normalize(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return "";
            s = s.Trim().ToLowerInvariant();

            // Remove Vietnamese diacritics for robust matching
            var formD = s.Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder();
            foreach (var ch in formD)
            {
                var uc = CharUnicodeInfo.GetUnicodeCategory(ch);
                if (uc != UnicodeCategory.NonSpacingMark)
                    sb.Append(ch);
            }
            s = sb.ToString().Normalize(NormalizationForm.FormC);

            // collapse multiple spaces
            while (s.Contains("  ")) s = s.Replace("  ", " ");
            return s;
        }
    }
}