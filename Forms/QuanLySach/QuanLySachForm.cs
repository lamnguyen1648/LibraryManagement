using System;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using LibraryManagement.Forms.QuanLyNhaXuatBan;
using Microsoft.Data.SqlClient;

namespace LibraryManagement.Forms.QuanLySach
{
    public partial class QuanLySachForm : Form
    {
        private const string TableName = "Sach";
        private const string IdColumnName = "Sach_ID"; // adjust if your PK differs

        private static readonly string[] HiddenColumns =
        {
            "ID", "Sach_ID", "TinhTrangSach", "QR_Code"
        };

        private readonly DataTable _books = new DataTable();
        private readonly BindingSource _bs = new BindingSource();

        public QuanLySachForm()
        {
            InitializeComponent();

            Load += (_, __) => ReloadBooks();

            btnQuanLyNXB.Click       -= BtnQuanLyNXB_Click;
            btnQuanLyNXB.Click       += BtnQuanLyNXB_Click;

            btnQuanLyTheLoai.Click   -= BtnQuanLyTheLoai_Click;
            btnQuanLyTheLoai.Click   += BtnQuanLyTheLoai_Click;

            btnQuanLyTacGia.Click    -= BtnQuanLyTacGia_Click;
            btnQuanLyTacGia.Click    += BtnQuanLyTacGia_Click;

            btnThemSach.Click        -= btnThemSach_Click;
            btnThemSach.Click        += btnThemSach_Click;

            btnTimKiem.Click         -= BtnTimKiem_Click;
            btnTimKiem.Click         += BtnTimKiem_Click;

            btnXoaNhieu.Enabled = false;
            btnXoaNhieu.Click   -= BtnXoaNhieu_Click;
            btnXoaNhieu.Click   += BtnXoaNhieu_Click;

            // Grid behavior
            dgvBooks.AutoGenerateColumns = false;
            dgvBooks.CellPainting += DgvBooks_CellPainting;       // draw Sửa/Xóa buttons
            dgvBooks.CellMouseClick += DgvBooks_CellMouseClick;   // handle Sửa/Xóa clicks
            dgvBooks.CurrentCellDirtyStateChanged += DgvBooks_CurrentCellDirtyStateChanged;
            dgvBooks.CellValueChanged += DgvBooks_CellValueChanged;
            dgvBooks.Sorted += (_, __) => UpdateSttValues();      // refresh STT after sort
            dgvBooks.DataBindingComplete += (_, __) => UpdateSttValues();

            _bs.DataSource = _books;
            dgvBooks.DataSource = _bs;
        }

        private void BtnQuanLyNXB_Click(object? sender, EventArgs e)
        {
            using (var f = new QuanLyNxbForm())
            {
                Hide();
                var result = f.ShowDialog(this);
                Show();
                if (result == DialogResult.OK)
                {
                    // Optional: refresh books view after coming back, if needed
                    ReloadBooks(txtSearch.Text?.Trim());
                }
            }
        }
        private void BtnQuanLyTheLoai_Click(object? s, EventArgs e)=> MessageBox.Show("TODO: Mở quản lý thể loại", "Placeholder");
        private void BtnQuanLyTacGia_Click(object? s, EventArgs e) => MessageBox.Show("TODO: Mở quản lý tác giả", "Placeholder");

        private void BtnTimKiem_Click(object? s, EventArgs e) => ReloadBooks(txtSearch.Text?.Trim());
        private void BtnXoaNhieu_Click(object? s, EventArgs e) => BulkDeleteSelected();

        // Open ThemSachForm
        private void btnThemSach_Click(object? sender, EventArgs e)
        {
            using (var f = new ThemSachForm())
            {
                var result = f.ShowDialog(this);
                if (result == DialogResult.OK)
                {
                    ReloadBooks(txtSearch.Text?.Trim());
                }
            }
        }

        // Load data (initial or search) — keep existing columns to preserve widths
        private void ReloadBooks(string? search = null)
        {
            _books.Clear();

            using var conn = Db.Create();
            using var cmd = conn.CreateCommand();

            if (string.IsNullOrWhiteSpace(search))
            {
                cmd.CommandText = $"select * from {TableName}";
            }
            else
            {
                // adjust TenSach if your title column differs
                cmd.CommandText = $"select * from {TableName} where TenSach like N'%' + @q + N'%'";
                cmd.Parameters.Add(new SqlParameter("@q", SqlDbType.NVarChar, 255) { Value = search });
            }

            using var da = new SqlDataAdapter(cmd);
            da.Fill(_books);

            BuildGridColumnsIfNeeded(); // build once
            EnsureHiddenColumns();      // enforce hidden cols every load
            LocalizeHeaders();          // ensure localized headers
            UpdateEmptyState();
            UpdateBulkDeleteButtonState();
            UpdateSttValues();
        }

        // Build grid columns once (Select | STT | DB cols | Function)
        private void BuildGridColumnsIfNeeded()
        {
            if (dgvBooks.Columns.Count > 0) return;

            // 1) Checkbox
            var colSelect = new DataGridViewCheckBoxColumn
            {
                Name = "Select",
                HeaderText = "Chọn",
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
                FillWeight = 6
            };
            dgvBooks.Columns.Add(colSelect);

            // 2) STT
            var colStt = new DataGridViewTextBoxColumn
            {
                Name = "STT",
                HeaderText = "STT",
                ReadOnly = true,
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
                FillWeight = 5
            };
            dgvBooks.Columns.Add(colStt);

            // 3) DB columns (localized headers)
            foreach (DataColumn dc in _books.Columns)
            {
                string colName = dc.ColumnName;

                var col = new DataGridViewTextBoxColumn
                {
                    DataPropertyName = colName,
                    Name = colName,
                    HeaderText = ToVietnameseHeader(colName), // localized
                    ReadOnly = true,
                    AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
                    FillWeight = 1
                };
                dgvBooks.Columns.Add(col);
            }

            // 4) Function column
            var colFunc = new DataGridViewTextBoxColumn
            {
                Name = "Function",
                HeaderText = "Chức năng",
                ReadOnly = true,
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
                FillWeight = 12
            };
            dgvBooks.Columns.Add(colFunc);
        }

        // Hide columns that shouldn't show in UI
        private void EnsureHiddenColumns()
        {
            foreach (var name in HiddenColumns)
            {
                var col = dgvBooks.Columns[name];
                if (col != null) col.Visible = false;
            }
            var pkCol = dgvBooks.Columns[IdColumnName];
            if (pkCol != null) pkCol.Visible = false;
        }

        // Localize headers (in case schema changes between loads)
        private void LocalizeHeaders()
        {
            foreach (DataGridViewColumn c in dgvBooks.Columns)
            {
                if (c.Name is "Select" or "STT" or "Function") continue;
                c.HeaderText = ToVietnameseHeader(c.Name);
            }
        }

        // STT numbering (1-based)
        private void UpdateSttValues()
        {
            var sttCol = dgvBooks.Columns["STT"];
            if (sttCol == null) return;

            for (int i = 0; i < dgvBooks.Rows.Count; i++)
            {
                var row = dgvBooks.Rows[i];
                if (!row.IsNewRow)
                    row.Cells[sttCol.Index].Value = (i + 1).ToString();
            }
        }

        private void UpdateEmptyState() => lblEmpty.Visible = _books.Rows.Count == 0;

        private void DgvBooks_CurrentCellDirtyStateChanged(object? sender, EventArgs e)
        {
            if (dgvBooks.IsCurrentCellDirty && dgvBooks.CurrentCell is DataGridViewCheckBoxCell)
                dgvBooks.CommitEdit(DataGridViewDataErrorContexts.Commit);
        }

        private void DgvBooks_CellValueChanged(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0 && dgvBooks.Columns[e.ColumnIndex].Name == "Select")
                UpdateBulkDeleteButtonState();
        }

        private void UpdateBulkDeleteButtonState()
        {
            var selectCol = dgvBooks.Columns["Select"];
            if (selectCol == null) { btnXoaNhieu.Enabled = false; return; }

            bool anyChecked = dgvBooks.Rows
                .Cast<DataGridViewRow>()
                .Any(r => Convert.ToBoolean(r.Cells[selectCol.Index].Value ?? false));

            btnXoaNhieu.Enabled = anyChecked;
        }

        // Draw Sửa / Xóa buttons in Function column
        private void DgvBooks_CellPainting(object? sender, DataGridViewCellPaintingEventArgs e)
        {
            if (e.RowIndex < 0) return;
            if (dgvBooks.Columns[e.ColumnIndex].Name != "Function") return;

            e.PaintBackground(e.ClipBounds, true);
            var cell = e.CellBounds;
            int padding = Math.Max(2, cell.Height / 10);
            int btnWidth = (cell.Width - (padding * 3)) / 2;
            int btnHeight = cell.Height - (padding * 2);

            var editRect = new Rectangle(cell.X + padding, cell.Y + padding, btnWidth, btnHeight);
            var delRect  = new Rectangle(editRect.Right + padding, cell.Y + padding, btnWidth, btnHeight);

            ButtonRenderer.DrawButton(e.Graphics, editRect, "Sửa", e.CellStyle.Font, false,
                System.Windows.Forms.VisualStyles.PushButtonState.Default);
            ButtonRenderer.DrawButton(e.Graphics, delRect, "Xóa", e.CellStyle.Font, false,
                System.Windows.Forms.VisualStyles.PushButtonState.Default);

            e.Handled = true;
        }

        private void DgvBooks_CellMouseClick(object? sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.RowIndex < 0) return;
            if (e.Button != MouseButtons.Left) return;
            if (dgvBooks.Columns[e.ColumnIndex].Name != "Function") return;

            var cell = dgvBooks.GetCellDisplayRectangle(e.ColumnIndex, e.RowIndex, false);
            int padding = Math.Max(2, cell.Height / 10);
            int btnWidth = (cell.Width - (padding * 3)) / 2;
            var editRect = new Rectangle(cell.X + padding, cell.Y + padding, btnWidth, cell.Height - (padding * 2));
            var delRect  = new Rectangle(editRect.Right + padding, cell.Y + padding, btnWidth, cell.Height - (padding * 2));
            var click    = dgvBooks.PointToClient(Cursor.Position);

            if (editRect.Contains(click)) EditRow(e.RowIndex);
            else if (delRect.Contains(click)) DeleteSingle(e.RowIndex);
        }

        private void EditRow(int rowIndex)
        {
            var row = dgvBooks.Rows[rowIndex];
            var id = GetRowId(row);
            if (id == null) { MessageBox.Show("Không tìm thấy ID bản ghi.", "Lỗi"); return; }

            using (var f = new SuaSachForm(id.Value))
            {
                var result = f.ShowDialog(this);
                if (result == DialogResult.OK)
                {
                    ReloadBooks(txtSearch.Text?.Trim());
                }
            }
        }

        private void DeleteSingle(int rowIndex)
        {
            var row = dgvBooks.Rows[rowIndex];
            var id = GetRowId(row);
            if (id == null) { MessageBox.Show("Không tìm thấy ID bản ghi.", "Lỗi"); return; }

            var confirm = MessageBox.Show("Bạn có chắc muốn xóa bản ghi này?",
                "Xóa sách", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (confirm != DialogResult.Yes) return;

            ExecuteDeleteByIds(new[] { id.Value });
            ReloadBooks(txtSearch.Text?.Trim());
        }

        private void BulkDeleteSelected()
        {
            var selectCol = dgvBooks.Columns["Select"];
            if (selectCol == null) return;

            var ids = dgvBooks.Rows
                .Cast<DataGridViewRow>()
                .Where(r => Convert.ToBoolean(r.Cells[selectCol.Index].Value ?? false))
                .Select(GetRowId)
                .Where(v => v.HasValue)
                .Select(v => v!.Value)
                .ToArray();

            if (ids.Length == 0) return;

            var confirm = MessageBox.Show($"Xóa {ids.Length} sách đã chọn?",
                "Xóa nhiều sách", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (confirm != DialogResult.Yes) return;

            ExecuteDeleteByIds(ids);
            ReloadBooks(txtSearch.Text?.Trim());
        }

        private int? GetRowId(DataGridViewRow row)
        {
            var pkCol = dgvBooks.Columns[IdColumnName] ?? dgvBooks.Columns["ID"];
            if (pkCol == null) return null;

            var val = row.Cells[pkCol.Index].Value?.ToString();
            return int.TryParse(val, out int id) ? id : null;
        }

        private void ExecuteDeleteByIds(int[] ids)
        {
            using var conn = Db.Create();
            conn.Open();

            var parms = ids.Select((_, i) => $"@p{i}").ToArray();
            var sql = $"delete from {TableName} where {IdColumnName} in ({string.Join(",", parms)})";

            using var cmd = new SqlCommand(sql, conn);
            for (int i = 0; i < ids.Length; i++)
                cmd.Parameters.Add(new SqlParameter(parms[i], SqlDbType.Int) { Value = ids[i] });

            cmd.ExecuteNonQuery();
        }

        // ===== Header localization helpers =====
        private static string ToVietnameseHeader(string col) => col.ToLowerInvariant() switch
        {
            "tensach"       => "Tên sách",
            "namxuatban"    => "Năm xuất bản",
            "tinhtrangsach" => "Tình trạng sách",
            "nxb_id"        => "Nhà Xuất Bản",
            "tg_id"         => "Tác Giả",
            "tl_id"         => "Thể Loại",
            "soluong"       => "Số lượng",
            "gia"           => "Giá",
            "mota"          => "Mô tả",
            "ngaynhap"      => "Ngày nhập",
            "vitri"         => "Vị trí",
            "masach"        => "Mã sách",
            _               => SplitPascal(col)
        };

        private static string SplitPascal(string s)
        {
            var chars = new System.Collections.Generic.List<char>(s.Length * 2);
            for (int i = 0; i < s.Length; i++)
            {
                char c = s[i];
                if (i > 0 && char.IsUpper(c) && (char.IsLower(s[i - 1]) || (i + 1 < s.Length && char.IsLower(s[i + 1]))))
                    chars.Add(' ');
                chars.Add(c);
            }
            return new string(chars.ToArray());
        }
    }
}
