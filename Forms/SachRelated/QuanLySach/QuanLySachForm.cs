using System.Data;
using LibraryManagement.Forms.SachRelated.QuanLyNhaXuatBan;
using LibraryManagement.Forms.SachRelated.QuanLyTacGia;
using LibraryManagement.Forms.SachRelated.QuanLyTheLoai;
using Microsoft.Data.SqlClient;

namespace LibraryManagement.Forms.SachRelated.QuanLySach
{
    public partial class QuanLySachForm : Form
    {
        private const string TableName    = "Sach";
        private const string IdColumnName = "Sach_ID"; // adjust if your PK differs

        // Hide technical columns + FK ids (we will show *_Name instead)
        private static readonly string[] HiddenColumns =
        {
            "ID", "Sach_ID", "TinhTrangSach", "QR_Code",
            "NXB_ID", "TG_ID", "TL_ID"
        };

        // Data
        private readonly DataTable _books = new DataTable();
        private readonly BindingSource _bs = new BindingSource();

        // Resolved lookup meta (auto-detected once)
        private string? _nxbTable, _nxbIdCol, _nxbNameCol;
        private string? _tgTable,  _tgIdCol,  _tgNameCol;
        private string? _tlTable,  _tlIdCol,  _tlNameCol;

        public QuanLySachForm()
        {
            InitializeComponent();

            Load += (_, __) => ReloadBooks();

            // Wiring (clear first to avoid duplicate handlers)
            btnQuanLyNXB.Click       -= BtnQuanLyNXB_Click;
            btnQuanLyTheLoai.Click   -= BtnQuanLyTheLoai_Click;
            btnQuanLyTacGia.Click    -= BtnQuanLyTacGia_Click;
            btnThemSach.Click        -= btnThemSach_Click;
            btnTimKiem.Click         -= BtnTimKiem_Click;
            btnXoaNhieu.Click        -= BtnXoaNhieu_Click;

            btnQuanLyNXB.Click       += BtnQuanLyNXB_Click;
            btnQuanLyTheLoai.Click   += BtnQuanLyTheLoai_Click;
            btnQuanLyTacGia.Click    += BtnQuanLyTacGia_Click;
            btnThemSach.Click        += btnThemSach_Click;
            btnTimKiem.Click         += BtnTimKiem_Click;
            btnXoaNhieu.Click        += BtnXoaNhieu_Click;

            btnXoaNhieu.Enabled = false;

            // Grid behavior
            dgvBooks.AutoGenerateColumns = false;
            dgvBooks.CellPainting += DgvBooks_CellPainting;       // draw Sửa/Xóa
            dgvBooks.CellMouseClick += DgvBooks_CellMouseClick;   // handle clicks
            dgvBooks.CurrentCellDirtyStateChanged += DgvBooks_CurrentCellDirtyStateChanged;
            dgvBooks.CellValueChanged += DgvBooks_CellValueChanged;
            dgvBooks.DataBindingComplete += (_, __) => UpdateSttValues();
            dgvBooks.Sorted += (_, __) => UpdateSttValues();

            _bs.DataSource = _books;
            dgvBooks.DataSource = _bs;
        }

        // ===== Toolbar actions =====
        private void BtnQuanLyNXB_Click(object? s, EventArgs e)
        {
            using (var f = new QuanLyNxbForm())
            {
                Hide();
                var result = f.ShowDialog(this);
                Show();
                if (result == DialogResult.OK)
                    ReloadBooks(txtSearch.Text?.Trim());
            }
        }
        private void BtnQuanLyTheLoai_Click(object? s, EventArgs e)
        {
            using (var f = new QuanLyTheLoaiForm())
            {
                Hide();
                var result = f.ShowDialog(this);
                Show();
                if (result == DialogResult.OK)
                    ReloadBooks(txtSearch.Text?.Trim());
            }
        }
        private void BtnQuanLyTacGia_Click(object? s, EventArgs e)
        {
            using (var f = new QuanLyTacGiaForm())
            {
                Hide();
                var result = f.ShowDialog(this);
                Show();
                if (result == DialogResult.OK)
                    ReloadBooks(txtSearch.Text?.Trim());
            }
        }

        private void BtnTimKiem_Click(object? s, EventArgs e) => ReloadBooks(txtSearch.Text?.Trim());

        private void btnThemSach_Click(object? sender, EventArgs e)
        {
            using (var f = new ThemSachForm())
            {
                if (f.ShowDialog(this) == DialogResult.OK)
                    ReloadBooks(txtSearch.Text?.Trim());
            }
        }

        private void BtnXoaNhieu_Click(object? s, EventArgs e) => BulkDeleteSelected();

        // ===== Data loading (JOIN to show names) =====
        private void ReloadBooks(string? search = null)
        {
            _books.Clear();

            using var conn = Db.Create();
            using var cmd  = conn.CreateCommand();
            conn.Open();

            if (_nxbTable == null && _tgTable == null && _tlTable == null)
                ResolveLookups(conn);

            bool withSearch = !string.IsNullOrWhiteSpace(search);
            cmd.CommandText = BuildSelectSql(withSearch);
            if (withSearch)
                cmd.Parameters.Add(new SqlParameter("@q", SqlDbType.NVarChar, 255) { Value = search! });

            using var da = new SqlDataAdapter(cmd);
            da.Fill(_books);

            BuildGridColumnsIfNeeded(); // build once to keep widths stable
            EnsureHiddenColumns();      // hide *_ID etc.
            LocalizeHeaders();          // VN headers (incl. *_Name)
            UpdateEmptyState();
            UpdateBulkDeleteButtonState();
            UpdateSttValues();
        }

        private static (string table, string id, string name)? TryResolveLookup(
            SqlConnection conn, string[] tableCandidates, string[] idCandidates, string[] nameCandidates)
        {
            foreach (var t in tableCandidates)
            {
                try
                {
                    using var probe = new SqlCommand($"SELECT TOP(1) * FROM {t}", conn);
                    using var da = new SqlDataAdapter(probe);
                    var tmp = new DataTable();
                    da.Fill(tmp);
                    if (tmp.Columns.Count == 0) continue;

                    var id   = idCandidates.FirstOrDefault(c => tmp.Columns.Contains(c));
                    var name = nameCandidates.FirstOrDefault(c => tmp.Columns.Contains(c));
                    if (id != null && name != null) return (t, id, name);
                }
                catch { /* try next */ }
            }
            return null;
        }

        private void ResolveLookups(SqlConnection conn)
        {
            var nxb = TryResolveLookup(conn,
                new[] { "NXB", "NhaXuatBan" },
                new[] { "NXB_ID", "ID" },
                new[] { "TenNXB", "Ten", "TenNhaXuatBan" });
            if (nxb.HasValue) { _nxbTable = nxb.Value.table; _nxbIdCol = nxb.Value.id; _nxbNameCol = nxb.Value.name; }

            var tg = TryResolveLookup(conn,
                new[] { "TacGia" },
                new[] { "TG_ID", "ID" },
                new[] { "TenTG", "HoTen", "TenTacGia" });
            if (tg.HasValue) { _tgTable = tg.Value.table; _tgIdCol = tg.Value.id; _tgNameCol = tg.Value.name; }

            var tl = TryResolveLookup(conn,
                new[] { "TheLoai" },
                new[] { "TL_ID", "ID" },
                new[] { "TenTL", "TenTheLoai", "Ten" });
            if (tl.HasValue) { _tlTable = tl.Value.table; _tlIdCol = tl.Value.id; _tlNameCol = tl.Value.name; }
        }

        private string BuildSelectSql(bool withSearch)
        {
            // Stable alias columns for display: NXB_Name, TacGia_Name, TheLoai_Name
            var select = $@"
SELECT s.*,
       {(_nxbTable != null ? $"n.[{_nxbNameCol}]  AS NXB_Name"     : "NULL AS NXB_Name")},
       {(_tgTable  != null ? $"tg.[{_tgNameCol}]  AS TacGia_Name"  : "NULL AS TacGia_Name")},
       {(_tlTable  != null ? $"tl.[{_tlNameCol}]  AS TheLoai_Name" : "NULL AS TheLoai_Name")}
FROM {TableName} s
{(_nxbTable != null ? $"LEFT JOIN {_nxbTable} n  ON s.NXB_ID = n.[{_nxbIdCol}] "   : "")}
{(_tgTable  != null ? $"LEFT JOIN {_tgTable}  tg ON s.TG_ID  = tg.[{_tgIdCol}] "  : "")}
{(_tlTable  != null ? $"LEFT JOIN {_tlTable}  tl ON s.TL_ID  = tl.[{_tlIdCol}] "  : "")}";
            if (withSearch) select += "WHERE s.TenSach LIKE N'%' + @q + N'%' ";
            return select;
        }

        // ===== Grid build / visuals =====
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

            // 3) DB columns (from DataTable; we’ll hide technical ones later)
            foreach (DataColumn dc in _books.Columns)
            {
                string colName = dc.ColumnName;
                var col = new DataGridViewTextBoxColumn
                {
                    DataPropertyName = colName,
                    Name = colName,
                    HeaderText = ToVietnameseHeader(colName),
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

        private void EnsureHiddenColumns()
        {
            foreach (var name in HiddenColumns)
            {
                var col = dgvBooks.Columns[name];
                if (col != null) col.Visible = false;
            }
            var pk = dgvBooks.Columns[IdColumnName];
            if (pk != null) pk.Visible = false;
        }

        private void LocalizeHeaders()
        {
            foreach (DataGridViewColumn c in dgvBooks.Columns)
            {
                if (c.Name is "Select" or "STT" or "Function") continue;
                c.HeaderText = ToVietnameseHeader(c.Name);
            }
        }

        // VN headers, including resolved *_Name columns
        private static string ToVietnameseHeader(string col) => col.ToLowerInvariant() switch
        {
            "tensach"       => "Tên sách",
            "namxuatban"    => "Năm xuất bản",
            "tinhtrangsach" => "Tình trạng sách",

            // FK ids (hidden) and display names
            "nxb_id"        => "Nhà Xuất Bản",
            "tg_id"         => "Tác Giả",
            "tl_id"         => "Thể Loại",
            "nxb_name"      => "Nhà Xuất Bản",
            "tacgia_name"   => "Tác Giả",
            "theloai_name"  => "Thể Loại",

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

        // ===== STT / empty state =====
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

        // ===== Checkbox & bulk delete enable =====
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

        // ===== Function buttons (Sửa/Xóa) =====
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
            var id = GetRowId(dgvBooks.Rows[rowIndex]);
            if (id == null) { MessageBox.Show("Không tìm thấy ID bản ghi.", "Lỗi"); return; }

            using (var f = new SuaSachForm(id.Value))
            {
                if (f.ShowDialog(this) == DialogResult.OK)
                    ReloadBooks(txtSearch.Text?.Trim());
            }
        }

        private void DeleteSingle(int rowIndex)
        {
            var id = GetRowId(dgvBooks.Rows[rowIndex]);
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
            try
            {
                // Prefer bound value
                if (row.DataBoundItem is DataRowView drv)
                {
                    var obj = drv.Row[IdColumnName];
                    if (obj != DBNull.Value && obj != null)
                    {
                        if (obj is int i) return i;
                        if (obj is long l) return checked((int)l);
                        if (int.TryParse(obj.ToString(), out var parsed)) return parsed;
                    }
                }
                // Fallback: by cell
                if (dgvBooks.Columns.Contains(IdColumnName))
                {
                    var val = row.Cells[IdColumnName].Value;
                    if (val != null && val != DBNull.Value)
                    {
                        if (val is int i2) return i2;
                        if (val is long l2) return checked((int)l2);
                        if (int.TryParse(val.ToString(), out var parsed2)) return parsed2;
                    }
                }
                if (dgvBooks.Columns.Contains("ID"))
                {
                    var val = row.Cells["ID"].Value;
                    if (val != null && int.TryParse(val.ToString(), out var parsed3)) return parsed3;
                }
            }
            catch { /* ignore */ }
            return null;
        }

        private void ExecuteDeleteByIds(int[] ids)
        {
            using var conn = Db.Create();
            conn.Open();

            var parms = ids.Select((_, i) => $"@p{i}").ToArray();
            var sql = $"DELETE FROM {TableName} WHERE {IdColumnName} IN ({string.Join(",", parms)})";

            using var cmd = new SqlCommand(sql, conn);
            for (int i = 0; i < ids.Length; i++)
                cmd.Parameters.Add(new SqlParameter(parms[i], SqlDbType.Int) { Value = ids[i] });

            cmd.ExecuteNonQuery();
        }
    }
}
