using System;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Microsoft.Data.SqlClient;

namespace LibraryManagement.Forms.SachRelated.LichSuCapNhatSach
{
    public partial class LichSuCapNhatSachForm : Form
    {
        private readonly DataTable _dt = new();
        private readonly BindingSource _bs = new();

        public LichSuCapNhatSachForm()
        {
            // Use a unique initializer name to avoid any InitializeComponent() ambiguity
            InitializeComponent_LichSuCapNhatSach();

            dgvLog.AutoGenerateColumns = false;
            dgvLog.AllowUserToAddRows = false;
            dgvLog.AllowUserToDeleteRows = false;
            dgvLog.MultiSelect = false;
            dgvLog.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvLog.RowHeadersVisible = false;
            dgvLog.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

            _bs.DataSource = _dt;
            dgvLog.DataSource = _bs;

            Load += (_, __) => Reload();
            btnTimKiem.Click += (_, __) => Reload(txtSearch.Text?.Trim());
            txtSearch.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) Reload(txtSearch.Text?.Trim()); };

            dgvLog.DataBindingComplete += (_, __) => UpdateStt();
            dgvLog.Sorted += (_, __) => UpdateStt();
        }

        private void Reload(string? q = null)
        {
            _dt.Clear();

            using var conn = Db.Create();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
SELECT  ls.LichSuCapNhatSach_ID,
        ISNULL(nv.TenNV, N'')      AS NhanVien,
        ISNULL(s.TenSach, N'')     AS TenSach,
        ls.NgayCapNhat,
        ls.HinhThucCapNhat,
        ls.ChiTietCapNhat
FROM dbo.LichSuCapNhatSach ls
LEFT JOIN dbo.NhanVien nv ON nv.NV_ID = ls.NV_ID
LEFT JOIN dbo.Sach s      ON s.Sach_ID = ls.Sach_ID
/**where**/
ORDER BY ls.NgayCapNhat DESC;";
            if (!string.IsNullOrWhiteSpace(q))
            {
                cmd.CommandText = cmd.CommandText.Replace("/**where**/",
                    "WHERE nv.TenNV LIKE N'%' + @q + N'%' OR s.TenSach LIKE N'%' + @q + N'%' OR ls.ChiTietCapNhat LIKE N'%' + @q + N'%'");
                cmd.Parameters.Add(new SqlParameter("@q", SqlDbType.NVarChar, 255) { Value = q! });
            }
            else
            {
                cmd.CommandText = cmd.CommandText.Replace("/**where**/", "");
            }

            using var da = new SqlDataAdapter(cmd);
            da.Fill(_dt);

            BuildColumns(); // stable widths/order every time
            lblEmpty.Visible = _dt.Rows.Count == 0;
        }

        private void BuildColumns()
        {
            if (dgvLog.Columns.Count > 0)
            {
                // If columns already built, just make sure date format stays
                if (dgvLog.Columns.Contains("NgayCapNhat"))
                    dgvLog.Columns["NgayCapNhat"].DefaultCellStyle.Format = "dd/MM/yyyy HH:mm:ss";
                UpdateStt();
                return;
            }

            dgvLog.Columns.Clear();

            // [0] STT
            dgvLog.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "STT",
                HeaderText = "STT",
                ReadOnly = true,
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
                FillWeight = 8
            });

            // (Hidden) Technical ID – keep for reference but not shown
            var idCol = new DataGridViewTextBoxColumn
            {
                Name = "LichSuCapNhatSach_ID",
                DataPropertyName = "LichSuCapNhatSach_ID",
                Visible = false,
                ReadOnly = true
            };
            dgvLog.Columns.Add(idCol);

            // [1] Nhân viên
            dgvLog.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "NhanVien",
                DataPropertyName = "NhanVien",
                HeaderText = "Nhân viên",
                ReadOnly = true,
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
                FillWeight = 18
            });

            // [2] Tên sách
            dgvLog.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "TenSach",
                DataPropertyName = "TenSach",
                HeaderText = "Tên sách",
                ReadOnly = true,
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
                FillWeight = 24
            });

            // [3] Ngày cập nhật (format dd/MM/yyyy HH:mm:ss)
            var dtCol = new DataGridViewTextBoxColumn
            {
                Name = "NgayCapNhat",
                DataPropertyName = "NgayCapNhat",
                HeaderText = "Ngày cập nhật",
                ReadOnly = true,
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
                FillWeight = 18,
                DefaultCellStyle = { Format = "dd/MM/yyyy HH:mm:ss" }
            };
            dgvLog.Columns.Add(dtCol);

            // [4] Hình thức
            dgvLog.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "HinhThucCapNhat",
                DataPropertyName = "HinhThucCapNhat",
                HeaderText = "Hình thức",
                ReadOnly = true,
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
                FillWeight = 12
            });

            // [5] Chi tiết
            dgvLog.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "ChiTietCapNhat",
                DataPropertyName = "ChiTietCapNhat",
                HeaderText = "Chi tiết",
                ReadOnly = true,
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
                FillWeight = 20
            });
        }

        private void UpdateStt()
        {
            var sttIdx = dgvLog.Columns["STT"]?.Index ?? -1;
            if (sttIdx < 0) return;
            for (int i = 0; i < dgvLog.Rows.Count; i++)
                if (!dgvLog.Rows[i].IsNewRow)
                    dgvLog.Rows[i].Cells[sttIdx].Value = (i + 1).ToString();
        }
    }
}
