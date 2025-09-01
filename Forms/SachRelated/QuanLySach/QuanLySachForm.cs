using System;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using LibraryManagement.Forms.NhanVienRelated.QuanLyNhanVien;
using Microsoft.Data.SqlClient;
using LibraryManagement.Forms.SachRelated.LichSuCapNhatSach;
using LibraryManagement.Forms.SachRelated.QuanLySach;
using LibraryManagement.Forms.SachRelated.QuanLyTacGia;
using LibraryManagement.Forms.SachRelated.QuanLyTheLoai; // this namespace (self)

namespace LibraryManagement.Forms.SachRelated.QuanLySach
{
    public partial class QuanLySachForm : Form
    {
        private readonly DataTable _dt = new();
        private readonly BindingSource _bs = new();

        public QuanLySachForm()
        {
            InitializeComponent();

            // Bind grid
            dgvSach.AutoGenerateColumns = false;
            _bs.DataSource = _dt;
            dgvSach.DataSource = _bs;

            // Events
            Load += (_, __) => Reload();
            btnTimKiem.Click += (_, __) => Reload(txtSearch.Text?.Trim());
            txtSearch.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) Reload(txtSearch.Text?.Trim()); };

            btnThemSach.Click += (_, __) => OpenThemSach();
            btnXoaNhieu.Click += (_, __) => BulkDelete();
            btnLichSuCapNhat.Click += (_, __) => OpenLichSu();

            btnQuanLyNXB.Click += (_, __) => OpenQuanLyNXB();
            btnQuanLyTheLoai.Click += (_, __) => OpenQuanLyTheLoai();
            btnQuanLyTacGia.Click += (_, __) => OpenQuanLyTacGia();

            dgvSach.CurrentCellDirtyStateChanged += (_, __) =>
            {
                if (dgvSach.IsCurrentCellDirty && dgvSach.CurrentCell is DataGridViewCheckBoxCell)
                    dgvSach.CommitEdit(DataGridViewDataErrorContexts.Commit);
            };
            dgvSach.CellValueChanged += (_, e) =>
            {
                if (e.RowIndex >= 0 && dgvSach.Columns[e.ColumnIndex].Name == "Select")
                    UpdateBulkDeleteButtonState();
            };
            dgvSach.DataBindingComplete += (_, __) => UpdateSttValues();
            dgvSach.Sorted += (_, __) => UpdateSttValues();
            dgvSach.CellPainting += DgvSach_CellPainting;
            dgvSach.CellMouseClick += DgvSach_CellMouseClick;
        }

        // ================== Data ==================
        private void Reload(string? search = null)
        {
            _dt.Clear();

            using var conn = Db.Create();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
SELECT  s.Sach_ID,
        s.TenSach,
        s.NamXuatBan,
        ISNULL(nxb.TenNXB, N'')      AS NhaXuatBan,
        ISNULL(tg.TenTG,  N'')       AS TacGia,
        ISNULL(tl.TenTheLoai, N'')   AS TheLoai
FROM dbo.Sach s
LEFT JOIN dbo.NhaXuatBan nxb ON nxb.NXB_ID = s.NXB_ID
LEFT JOIN dbo.TacGia     tg  ON tg.TG_ID   = s.TG_ID
LEFT JOIN dbo.TheLoai    tl  ON tl.TL_ID   = s.TL_ID
/**where**/
ORDER BY s.Sach_ID DESC;";
            if (!string.IsNullOrWhiteSpace(search))
            {
                cmd.CommandText = cmd.CommandText.Replace("/**where**/",
                    "WHERE s.TenSach LIKE N'%' + @q + N'%'");
                cmd.Parameters.Add(new SqlParameter("@q", SqlDbType.NVarChar, 255) { Value = search! });
            }
            else cmd.CommandText = cmd.CommandText.Replace("/**where**/", "");

            using var da = new SqlDataAdapter(cmd);
            da.Fill(_dt);

            BuildGridColumnsOnce();
            lblEmpty.Visible = _dt.Rows.Count == 0;
            UpdateBulkDeleteButtonState();
            UpdateSttValues();
        }

        // ================== Grid: fixed columns & widths ==================
        private void BuildGridColumnsOnce()
        {
            if (dgvSach.Columns.Count > 0) return;

            dgvSach.Columns.Clear();

            // [0] Checkbox
            dgvSach.Columns.Add(new DataGridViewCheckBoxColumn
            {
                Name = "Select", HeaderText = "Chọn",
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill, FillWeight = 7
            });

            // [1] STT
            dgvSach.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "STT", HeaderText = "STT", ReadOnly = true,
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill, FillWeight = 7
            });

            // (Hidden) ID
            dgvSach.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Sach_ID", DataPropertyName = "Sach_ID",
                Visible = false, ReadOnly = true
            });

            // [Tên sách]
            dgvSach.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "TenSach", DataPropertyName = "TenSach",
                HeaderText = "Tên sách", ReadOnly = true,
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill, FillWeight = 24
            });

            // [Năm xuất bản]
            dgvSach.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "NamXuatBan", DataPropertyName = "NamXuatBan",
                HeaderText = "Năm xuất bản", ReadOnly = true,
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill, FillWeight = 11
            });

            // [Nhà Xuất Bản]
            dgvSach.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "NhaXuatBan", DataPropertyName = "NhaXuatBan",
                HeaderText = "Nhà Xuất Bản", ReadOnly = true,
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill, FillWeight = 15
            });

            // [Tác Giả]
            dgvSach.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "TacGia", DataPropertyName = "TacGia",
                HeaderText = "Tác Giả", ReadOnly = true,
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill, FillWeight = 13
            });

            // [Thể Loại]
            dgvSach.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "TheLoai", DataPropertyName = "TheLoai",
                HeaderText = "Thể Loại", ReadOnly = true,
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill, FillWeight = 13
            });

            // [Chức năng] paint buttons Sửa/Xóa
            dgvSach.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Function", HeaderText = "Chức năng", ReadOnly = true,
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill, FillWeight = 10
            });
        }

        private void UpdateSttValues()
        {
            var col = dgvSach.Columns["STT"];
            if (col == null) return;
            for (int i = 0; i < dgvSach.Rows.Count; i++)
                if (!dgvSach.Rows[i].IsNewRow)
                    dgvSach.Rows[i].Cells[col.Index].Value = (i + 1).ToString();
        }

        private void UpdateBulkDeleteButtonState()
        {
            var selIdx = dgvSach.Columns["Select"]?.Index ?? -1;
            if (selIdx < 0) { btnXoaNhieu.Enabled = false; return; }

            bool any = dgvSach.Rows.Cast<DataGridViewRow>()
                .Any(r => Convert.ToBoolean(r.Cells[selIdx].Value ?? false));
            btnXoaNhieu.Enabled = any;
        }

        // ================== Function column (Sửa/Xóa) ==================
        private void DgvSach_CellPainting(object? sender, DataGridViewCellPaintingEventArgs e)
        {
            if (e.RowIndex < 0) return;
            if (dgvSach.Columns[e.ColumnIndex].Name != "Function") return;

            e.PaintBackground(e.ClipBounds, true);
            var cell = e.CellBounds;
            int pad = Math.Max(2, cell.Height / 10);
            int btnW = (cell.Width - (pad * 3)) / 2;
            int btnH = cell.Height - (pad * 2);

            var editR = new Rectangle(cell.X + pad, cell.Y + pad, btnW, btnH);
            var delR = new Rectangle(editR.Right + pad, cell.Y + pad, btnW, btnH);

            ButtonRenderer.DrawButton(e.Graphics, editR, "Sửa", e.CellStyle.Font, false,
                System.Windows.Forms.VisualStyles.PushButtonState.Default);
            ButtonRenderer.DrawButton(e.Graphics, delR, "Xóa", e.CellStyle.Font, false,
                System.Windows.Forms.VisualStyles.PushButtonState.Default);

            e.Handled = true;
        }

        private void DgvSach_CellMouseClick(object? sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.RowIndex < 0) return;
            if (e.Button != MouseButtons.Left) return;
            if (dgvSach.Columns[e.ColumnIndex].Name != "Function") return;

            var cell = dgvSach.GetCellDisplayRectangle(e.ColumnIndex, e.RowIndex, false);
            int pad = Math.Max(2, cell.Height / 10);
            int w = (cell.Width - (pad * 3)) / 2;

            var editR = new Rectangle(cell.X + pad, cell.Y + pad, w, cell.Height - (pad * 2));
            var delR = new Rectangle(editR.Right + pad, cell.Y + pad, w, cell.Height - (pad * 2));
            var click = dgvSach.PointToClient(Cursor.Position);

            if (editR.Contains(click)) EditRow(e.RowIndex);
            else if (delR.Contains(click)) DeleteSingle(e.RowIndex);
        }

        // ================== Actions ==================
        private int? GetRowId(DataGridViewRow row)
        {
            try
            {
                object? v = null;

                if (row.DataBoundItem is DataRowView drv)
                    v = drv.Row["Sach_ID"];
                else
                    v = row.Cells["Sach_ID"].Value;

                if (v == null || v == DBNull.Value) return null;
                if (v is int i) return i;
                if (v is long l) return checked((int)l);
                if (int.TryParse(v.ToString(), out var p)) return p;
            }
            catch { }
            return null;
        }

        private void OpenThemSach()
        {
            using var f = new ThemSachForm();
            if (f.ShowDialog(this) == DialogResult.OK)
                Reload(txtSearch.Text?.Trim());
        }

        private void EditRow(int rowIndex)
        {
            var id = GetRowId(dgvSach.Rows[rowIndex]);
            if (id == null) { MessageBox.Show("Không tìm thấy ID.", "Lỗi"); return; }

            using var f = new SuaSachForm(id.Value);
            if (f.ShowDialog(this) == DialogResult.OK)
                Reload(txtSearch.Text?.Trim());
        }

        private void DeleteSingle(int rowIndex)
        {
            var id = GetRowId(dgvSach.Rows[rowIndex]);
            if (id == null) { MessageBox.Show("Không tìm thấy ID.", "Lỗi"); return; }

            if (MessageBox.Show("Bạn có chắc muốn xóa sách này?", "Xóa sách",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;

            using var conn = Db.Create();
            using var cmdDel = conn.CreateCommand();
            cmdDel.CommandText = "DELETE FROM dbo.Sach WHERE Sach_ID=@id";
            cmdDel.Parameters.Add(new SqlParameter("@id", SqlDbType.Int){ Value = id.Value });
            conn.Open();
            cmdDel.ExecuteNonQuery();

            // No logging here (disabled by request)
            Reload(txtSearch.Text?.Trim());
        }

        private void BulkDelete()
        {
            var selIdx = dgvSach.Columns["Select"]?.Index ?? -1;
            if (selIdx < 0) return;

            var ids = dgvSach.Rows.Cast<DataGridViewRow>()
                .Where(r => Convert.ToBoolean(r.Cells[selIdx].Value ?? false))
                .Select(GetRowId).Where(i => i.HasValue).Select(i => i!.Value).ToArray();

            if (ids.Length == 0) return;

            if (MessageBox.Show($"Xóa {ids.Length} sách đã chọn?", "Xóa nhiều",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;

            ExecuteDelete(ids);
            Reload(txtSearch.Text?.Trim());
        }

        private void ExecuteDelete(int[] ids)
        {
            using var conn = Db.Create();
            conn.Open();

            foreach (var id in ids)
            {
                using var del = conn.CreateCommand();
                del.CommandText = "DELETE FROM dbo.Sach WHERE Sach_ID=@id";
                del.Parameters.Add(new SqlParameter("@id", SqlDbType.Int){ Value = id });
                del.ExecuteNonQuery();

                // No logging here (disabled by request)
            }
        }

        private void OpenLichSu()
        {
            using var f = new LichSuCapNhatSachForm();
            var me = this;
            me.Hide();
            try { _ = f.ShowDialog(me); }
            finally { me.Show(); me.Activate(); }
        }

        // === Navigations (adjust namespaces if needed) ===
        private void OpenQuanLyNXB()
        {
            try
            {
                using var f = new QuanLyNhanVienForm();
                Hide(); _ = f.ShowDialog(this); Show(); Activate();
            }
            catch
            {
                MessageBox.Show("Không tìm thấy form Quản lý NXB. Hãy kiểm tra namespace/tên lớp.", "Thông báo");
            }
        }

        private void OpenQuanLyTheLoai()
        {
            try
            {
                using var f = new QuanLyTheLoaiForm();
                Hide(); _ = f.ShowDialog(this); Show(); Activate();
            }
            catch
            {
                MessageBox.Show("Không tìm thấy form Quản lý thể loại. Hãy kiểm tra namespace/tên lớp.", "Thông báo");
            }
        }

        private void OpenQuanLyTacGia()
        {
            try
            {
                using var f = new QuanLyTacGiaForm();
                Hide(); _ = f.ShowDialog(this); Show(); Activate();
            }
            catch
            {
                MessageBox.Show("Không tìm thấy form Quản lý tác giả. Hãy kiểm tra namespace/tên lớp.", "Thông báo");
            }
        }
    }
}
