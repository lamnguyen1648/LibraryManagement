using System.Data;
using Microsoft.Data.SqlClient;

namespace LibraryManagement.Forms.PhieuMuonRelated.QuanLyPhieuMuon
{
    public partial class ChiTietPhieuMuonForm : Form
    {
        private readonly int _pmId;

        public ChiTietPhieuMuonForm(int pmId)
        {
            _pmId = pmId;
            InitializeComponent();
            StartPosition = FormStartPosition.CenterParent;
            Load += (_, __) => LoadData();
        }

        private void LoadData()
        {
            try
            {
                using var conn = Db.Create(); conn.Open();

                // main record
                var pm = new DataTable();
                using (var da = new SqlDataAdapter(
                           "SELECT PM_ID, DG_ID, NgayMuon, NgayTra, TinhTrang FROM PhieuMuon WHERE PM_ID=@id", conn))
                {
                    da.SelectCommand!.Parameters.Add(new SqlParameter("@id", SqlDbType.Int) { Value = _pmId });
                    da.Fill(pm);
                }
                if (pm.Rows.Count == 0) { MessageBox.Show("Không tìm thấy phiếu mượn.", "Lỗi"); Close(); return; }
                var r = pm.Rows[0];

                // display columns
                string dgDisplay = ResolveDisplayCol(conn, "DocGia",
                    new[] { "TenDocGia", "HoTen", "Ten", "HoVaTen", "TenDG", "Ten_DocGia" }, "DG_ID");
                string sachDisplay = ResolveDisplayCol(conn, "Sach",
                    new[] { "TenSach", "Ten", "TieuDe", "NhanDe" }, "Sach_ID");

                // Độc giả name
                string docGiaName = "";
                if (r["DG_ID"] != DBNull.Value)
                {
                    using var cmd = new SqlCommand($"SELECT [{dgDisplay}] FROM DocGia WHERE DG_ID=@dg", conn);
                    cmd.Parameters.Add(new SqlParameter("@dg", SqlDbType.Int) { Value = Convert.ToInt32(r["DG_ID"]) });
                    var o = cmd.ExecuteScalar();
                    docGiaName = o == null || o == DBNull.Value ? r["DG_ID"].ToString()! : o.ToString()!;
                }

                // books
                var books = new DataTable();
                using (var da = new SqlDataAdapter($@"
SELECT s.Sach_ID, s.[{sachDisplay}] AS TenHienThi
FROM ChiTietPhieuMuon c
JOIN Sach s ON s.Sach_ID = c.Sach_ID
WHERE c.PM_ID = @id
ORDER BY TenHienThi;", conn))
                {
                    da.SelectCommand!.Parameters.Add(new SqlParameter("@id", SqlDbType.Int) { Value = _pmId });
                    da.Fill(books);
                }

                // fill UI
                lblTitle.Text = $"Chi tiết phiếu mượn #{_pmId}";
                lblDocGiaValue.Text     = docGiaName;
                lblNgayMuonValue.Text   = FormatDate(r["NgayMuon"]);
                lblNgayTraValue.Text    = r["NgayTra"] == DBNull.Value ? "—" : FormatDate(r["NgayTra"]);
                lblTinhTrangValue.Text  = r["TinhTrang"] == DBNull.Value ? "—" : r["TinhTrang"].ToString();

                lbSach.Items.Clear();
                foreach (DataRow br in books.Rows)
                    lbSach.Items.Add(br["TenHienThi"]?.ToString() ?? $"Sách #{br["Sach_ID"]}");
                lblTongSach.Text = $"Tổng số sách: {lbSach.Items.Count}";
            }
            catch (Exception ex)
            {
                MessageBox.Show("Không tải được chi tiết:\r\n" + ex.Message, "Lỗi",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                Close();
            }
        }

        private static string ResolveDisplayCol(SqlConnection conn, string table, string[] preferred, string fallbackId)
        {
            var peek = new DataTable();
            using (var da = new SqlDataAdapter($"SELECT TOP(0) * FROM {table}", conn)) da.Fill(peek);
            return preferred.FirstOrDefault(c => peek.Columns.Contains(c))
                   ?? peek.Columns.Cast<DataColumn>().FirstOrDefault(c => c.DataType == typeof(string))?.ColumnName
                   ?? fallbackId;
        }

        private static string FormatDate(object o)
        {
            if (o == null || o == DBNull.Value) return "—";
            var d = Convert.ToDateTime(o);
            return d.ToString("dd/MM/yyyy");
        }
    }
}
