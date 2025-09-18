// LichSuCapNhatSachForm.cs

using System.Data;
using Microsoft.Data.SqlClient;

namespace LibraryManagement.Forms.LichSuCapNhatSach
{
    public partial class LichSuCapNhatSachForm : Form
    {
        private readonly DataTable _dt = new();
        private readonly BindingSource _bs = new();

        public LichSuCapNhatSachForm()
        {
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

            // Search & load
            Load += (_, __) => Reload();
            btnTimKiem.Click += (_, __) => Reload(txtSearch.Text?.Trim());
            txtSearch.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) Reload(txtSearch.Text?.Trim()); };

            // STT reliability
            dgvLog.DataBindingComplete += (_, __) => UpdateStt();
            dgvLog.Sorted += (_, __) => UpdateStt();
            dgvLog.RowsAdded += (_, __) => UpdateStt();
            dgvLog.DataSourceChanged += (_, __) => UpdateStt();

            // “Xem chi tiết” button
            dgvLog.CellPainting += DgvLog_CellPainting;
            dgvLog.CellMouseClick += DgvLog_CellMouseClick;
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

            BuildColumns();
            lblEmpty.Visible = _dt.Rows.Count == 0;
            UpdateStt();
        }

        private void BuildColumns()
        {
            if (dgvLog.Columns.Count > 0)
            {
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
                SortMode = DataGridViewColumnSortMode.NotSortable,
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
                FillWeight = 8
            });

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

            // [3] Ngày cập nhật
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

            dgvLog.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "ChucNang",
                HeaderText = "Chức năng",
                ReadOnly = true,
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
                FillWeight = 12
            });
        }

        private void UpdateStt()
        {
            var sttIdx = dgvLog.Columns["STT"]?.Index ?? -1;
            if (sttIdx < 0) return;

            for (int i = 0; i < dgvLog.Rows.Count; i++)
            {
                var row = dgvLog.Rows[i];
                if (!row.IsNewRow)
                    row.Cells[sttIdx].Value = (i + 1).ToString();
            }
        }

        private void DgvLog_CellPainting(object? sender, DataGridViewCellPaintingEventArgs e)
        {
            if (e.RowIndex < 0) return;
            if (dgvLog.Columns[e.ColumnIndex].Name != "ChucNang") return;

            e.PaintBackground(e.ClipBounds, true);

            var cell = e.CellBounds;
            int padding = Math.Max(2, cell.Height / 10);
            int btnWidth = cell.Width - padding * 2;
            int btnHeight = cell.Height - padding * 2;
            var btnRect = new Rectangle(cell.X + padding, cell.Y + padding, btnWidth, btnHeight);

            ButtonRenderer.DrawButton(
                e.Graphics, btnRect, "Xem chi tiết", e.CellStyle.Font, false,
                System.Windows.Forms.VisualStyles.PushButtonState.Default);

            e.Handled = true;
        }

        private void DgvLog_CellMouseClick(object? sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.RowIndex < 0 || e.Button != MouseButtons.Left) return;
            if (dgvLog.Columns[e.ColumnIndex].Name != "ChucNang") return;

            var cell = dgvLog.GetCellDisplayRectangle(e.ColumnIndex, e.RowIndex, false);
            int padding = Math.Max(2, cell.Height / 10);
            var btnRect = new Rectangle(cell.X + padding, cell.Y + padding, cell.Width - padding * 2, cell.Height - padding * 2);
            var click = dgvLog.PointToClient(Cursor.Position);
            if (!btnRect.Contains(click)) return;

            if (dgvLog.Rows[e.RowIndex].DataBoundItem is not DataRowView drv) return;

            using var dlg = new XemChiTietForm(drv.Row);
            dlg.ShowDialog(this);
        }

    }
}
