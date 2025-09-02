using System.Data;
using System.Reflection;
using LibraryManagement.Forms.PhieuMuonRelated.QuanLyPhieuPhat;
using Microsoft.Data.SqlClient;
// NEW

namespace LibraryManagement.Forms.PhieuMuonRelated.QuanLyPhieuMuon
{
    public partial class QuanLyPhieuMuonForm : Form
    {
        private string? _tableName;        // "PhieuMuon"
        private string? _idColumnName;     // "PM_ID"
        private string? _searchableColumn; // prefer "TinhTrang"
        
        private QuanLyPhieuPhatForm? _ppForm;

        private readonly DataTable _pm = new();
        private readonly BindingSource _bs = new();

        private static readonly string[] TableCandidates  = { "PhieuMuon" };
        private static readonly string[] IdCandidates     = { "PM_ID", "ID" };
        private static readonly string[] SearchCandidates = { "TinhTrang" };

        public QuanLyPhieuMuonForm()
        {
            InitializeComponent();

            Load += (_, __) => ReloadPhieuMuon();

            // wire once
            btnQuanLyPhieuPhat.Click -= BtnQuanLyPhieuPhat_Click; btnQuanLyPhieuPhat.Click += BtnQuanLyPhieuPhat_Click;
            btnThem.Click             -= BtnThem_Click;            btnThem.Click             += BtnThem_Click;
            btnXoaNhieu.Click         -= BtnXoaNhieu_Click;        btnXoaNhieu.Click         += BtnXoaNhieu_Click;
            btnTimKiem.Click          -= BtnTimKiem_Click;         btnTimKiem.Click          += BtnTimKiem_Click;

            dgvPhieuMuon.AutoGenerateColumns = false;
            dgvPhieuMuon.CurrentCellDirtyStateChanged += Dgv_CurrentCellDirtyStateChanged;
            dgvPhieuMuon.CellValueChanged             += Dgv_CellValueChanged;
            dgvPhieuMuon.DataBindingComplete          += (_, __) => UpdateSttValues();
            dgvPhieuMuon.Sorted                       += (_, __) => UpdateSttValues();

            dgvPhieuMuon.CellPainting   += Dgv_CellPainting;   // draw buttons
            dgvPhieuMuon.CellMouseClick += Dgv_CellMouseClick; // handle clicks

            _bs.DataSource = _pm;
            dgvPhieuMuon.DataSource = _bs;
        }

        // ===== Toolbar =====
        private void BtnQuanLyPhieuPhat_Click(object? s, EventArgs e)
        {
            if (_ppForm == null || _ppForm.IsDisposed)
            {
                _ppForm = new QuanLyPhieuPhatForm();

                _ppForm.FormClosed += (_, __) =>
                {
                    _ppForm = null;
                    this.Show();
                    this.Activate();
                    ReloadPhieuMuon(txtSearch.Text?.Trim());
                };
            }

            this.Hide();          // hide loans window while penalties is open
            _ppForm.ShowDialog(this); Show(); Activate();
        }


        private void BtnThem_Click(object? s, EventArgs e)
        {
            using var f = new ThemPhieuMuonForm();
            if (f.ShowDialog(this) == DialogResult.OK)
                ReloadPhieuMuon(txtSearch.Text?.Trim());
        }

        private void BtnTimKiem_Click(object? s, EventArgs e) => ReloadPhieuMuon(txtSearch.Text?.Trim());

        private void BtnXoaNhieu_Click(object? s, EventArgs e)
        {
            var selectCol = dgvPhieuMuon.Columns["Select"];
            if (selectCol == null) return;

            var ids = dgvPhieuMuon.Rows
                .Cast<DataGridViewRow>()
                .Where(r => Convert.ToBoolean(r.Cells[selectCol.Index].Value ?? false))
                .Select(GetRowId)
                .Where(v => v.HasValue)
                .Select(v => v!.Value)
                .ToArray();

            if (ids.Length == 0) return;

            var confirm = MessageBox.Show($"Xóa {ids.Length} phiếu mượn đã chọn?",
                "Xóa nhiều phiếu mượn", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (confirm != DialogResult.Yes) return;

            ExecuteDeleteByIds(ids);
            ReloadPhieuMuon(txtSearch.Text?.Trim());
        }

        // ===== Auto mark overdues & create penalties =====
        private void AutoProcessOverdues()
        {
            int? nvId = GetCurrentNhanVienId(); // may be null; still mark overdue even if no NV
            using var conn = Db.Create(); conn.Open();
            using var tx = conn.BeginTransaction();

            // 1) Update Đang mượn -> Quá hạn where NgayTra < today
            var cmd = conn.CreateCommand();
            cmd.Transaction = tx;
            cmd.CommandText = @"
DECLARE @Today date = CAST(GETDATE() AS date);
;WITH Overdue AS (
    SELECT pm.PM_ID
    FROM dbo.PhieuMuon pm
    WHERE pm.TinhTrang = N'Đang mượn'
      AND pm.NgayTra IS NOT NULL
      AND CAST(pm.NgayTra AS date) < @Today
)
UPDATE pm
   SET pm.TinhTrang = N'Quá hạn'
FROM dbo.PhieuMuon pm
JOIN Overdue o ON o.PM_ID = pm.PM_ID;

-- create penalties if we know the current employee
IF @HasNv = 1
BEGIN
    ;WITH Overdue AS (
        SELECT pm.PM_ID
        FROM dbo.PhieuMuon pm
        WHERE pm.TinhTrang = N'Quá hạn'
          AND pm.NgayTra IS NOT NULL
          AND CAST(pm.NgayTra AS date) < @Today
    )
    INSERT INTO dbo.PhieuPhat (NV_ID, PM_ID, LyDo, SoTienPhat)
    SELECT @nv, o.PM_ID, N'Quá hạn trả sách', 50000
    FROM Overdue o
    WHERE NOT EXISTS (SELECT 1 FROM dbo.PhieuPhat p WHERE p.PM_ID = o.PM_ID);
END
";
            cmd.Parameters.Add(new SqlParameter("@nv",    System.Data.SqlDbType.Int) { Value = (object?)nvId ?? DBNull.Value });
            cmd.Parameters.Add(new SqlParameter("@HasNv", System.Data.SqlDbType.Bit) { Value = nvId.HasValue ? 1 : 0 });

            cmd.ExecuteNonQuery();
            tx.Commit();
        }

        // Try to read current NV_ID from a static UserSession (no hard dependency)
        private static int? GetCurrentNhanVienId()
        {
            try
            {
                var candidates = new[]
                {
                    "LibraryManagement.UserSession",
                    "LibraryManagement.Auth.UserSession",
                    "UserSession"
                };

                foreach (var typeName in candidates)
                {
                    var t = Type.GetType(typeName);
                    if (t == null) continue;

                    var pId = t.GetProperty("NV_ID", BindingFlags.Public | BindingFlags.Static)
                           ?? t.GetProperty("NhanVienId", BindingFlags.Public | BindingFlags.Static)
                           ?? t.GetProperty("UserId", BindingFlags.Public | BindingFlags.Static)
                           ?? t.GetProperty("NVId", BindingFlags.Public | BindingFlags.Static);

                    if (pId == null) continue;

                    var idObj = pId.GetValue(null);
                    if (idObj == null) continue;

                    return Convert.ToInt32(idObj);
                }
            }
            catch { /* ignore */ }
            return null;
        }

        // ===== Data =====
        private void ReloadPhieuMuon(string? search = null)
        {
            // run automation first so the grid shows fresh statuses and penalties
            try { AutoProcessOverdues(); } catch { /* keep UI responsive even if batch fails */ }

            _pm.Clear();

            using var conn = Db.Create();
            using var cmd  = conn.CreateCommand();

            if (_tableName == null || _idColumnName == null || _searchableColumn == null)
                ResolveTableAndColumns(conn);

            if (_tableName == null)
            {
                MessageBox.Show("Không tìm thấy bảng Phiếu mượn.", "Lỗi");
                return;
            }

            if (!string.IsNullOrWhiteSpace(search) && _searchableColumn != null)
            {
                cmd.CommandText = $"select * from {_tableName} where [{_searchableColumn}] like N'%' + @q + N'%'";
                cmd.Parameters.Add(new SqlParameter("@q", SqlDbType.NVarChar, 255) { Value = search! });
            }
            else cmd.CommandText = $"select * from {_tableName}";

            using (var da = new SqlDataAdapter(cmd))
            {
                da.Fill(_pm);
            }

            // Add a display column "DocGia" and fill with reader's name
            ApplyDocGiaDisplay();

            BuildGridColumnsIfNeeded();
            EnsureHiddenIdColumn();
            LocalizeHeaders();
            ArrangeColumns();

            UpdateEmptyState();
            UpdateBulkDeleteButtonState();
            UpdateSttValues();
        }

        private void ResolveTableAndColumns(SqlConnection conn)
        {
            conn.Open();
            foreach (var table in TableCandidates)
            {
                try
                {
                    using var probe = new SqlCommand($"select top(1) * from {table}", conn);
                    using var da = new SqlDataAdapter(probe);
                    var dt = new DataTable(); da.Fill(dt);
                    if (dt.Columns.Count == 0) continue;

                    _tableName = table;

                    using (var rd = new SqlCommand(@"
select c.name, c.is_identity
from sys.columns c
where c.object_id = object_id(N'dbo." + table + @"')
order by c.column_id;", conn).ExecuteReader())
                    {
                        string? fallback = null;
                        while (rd.Read())
                        {
                            var name = rd.GetString(0);
                            bool isIdent = rd.GetBoolean(1);
                            if (isIdent) { _idColumnName = name; break; }
                            if (fallback == null && IdCandidates.Contains(name, StringComparer.OrdinalIgnoreCase))
                                fallback = name;
                        }
                        _idColumnName ??= fallback ?? dt.Columns[0].ColumnName;
                    }

                    _searchableColumn = SearchCandidates.FirstOrDefault(c => dt.Columns.Contains(c))
                        ?? dt.Columns.Cast<DataColumn>().FirstOrDefault(c => c.DataType == typeof(string))?.ColumnName;

                    return;
                }
                catch { /* try next */ }
            }
        }

        private Dictionary<int, string> BuildDocGiaLookup()
        {
            var map = new Dictionary<int, string>();
            try
            {
                using var conn = Db.Create(); conn.Open();

                // figure out a nice display column
                var peek = new DataTable();
                using (var da = new SqlDataAdapter("select top(0) * from DocGia", conn)) da.Fill(peek);

                string displayCol =
                    new[] { "TenDocGia", "HoTen", "Ten", "HoVaTen", "TenDG", "Ten_DocGia" }
                    .FirstOrDefault(c => peek.Columns.Contains(c))
                    ?? peek.Columns.Cast<DataColumn>().FirstOrDefault(c => c.DataType == typeof(string))?.ColumnName
                    ?? "DG_ID";

                var sql = $"select DG_ID, [{displayCol}] as TenHienThi from DocGia";
                using var da2 = new SqlDataAdapter(sql, conn);
                var list = new DataTable(); da2.Fill(list);

                foreach (DataRow r in list.Rows)
                {
                    var id = Convert.ToInt32(r["DG_ID"]);
                    var name = r["TenHienThi"]?.ToString() ?? id.ToString();
                    if (!map.ContainsKey(id)) map.Add(id, name);
                }
            }
            catch { /* ignore, fallback to id */ }

            return map;
        }

        private void ApplyDocGiaDisplay()
        {
            if (!_pm.Columns.Contains("DG_ID")) return;

            // Add display column if missing
            if (!_pm.Columns.Contains("DocGia"))
                _pm.Columns.Add(new DataColumn("DocGia", typeof(string)));

            var map = BuildDocGiaLookup();

            foreach (DataRow row in _pm.Rows)
            {
                var idObj = row["DG_ID"];
                string display = "";
                if (idObj != DBNull.Value && idObj != null)
                {
                    var id = Convert.ToInt32(idObj);
                    display = map.TryGetValue(id, out var n) ? n : id.ToString();
                }
                row["DocGia"] = display;
            }
        }

        // ===== Grid =====
        private void BuildGridColumnsIfNeeded()
        {
            if (dgvPhieuMuon.Columns.Count > 0) return;

            // [0] Checkbox
            dgvPhieuMuon.Columns.Add(new DataGridViewCheckBoxColumn
            {
                Name = "Select",
                HeaderText = "Chọn",
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
                FillWeight = 8
            });

            // [1] STT
            dgvPhieuMuon.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "STT",
                HeaderText = "STT",
                ReadOnly = true,
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
                FillWeight = 7
            });

            // DB columns (skip hidden ids and raw DG_ID; we’ll show DocGia instead)
            foreach (DataColumn dc in _pm.Columns)
            {
                var name = dc.ColumnName;
                if (_idColumnName != null && name.Equals(_idColumnName, StringComparison.OrdinalIgnoreCase))
                    continue;
                if (name.Equals("DG_ID", StringComparison.OrdinalIgnoreCase))
                    continue;

                dgvPhieuMuon.Columns.Add(new DataGridViewTextBoxColumn
                {
                    Name = name,
                    DataPropertyName = name,
                    HeaderText = ToVietnameseHeader(name),
                    ReadOnly = true,
                    AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
                    FillWeight = name.Equals("DocGia", StringComparison.OrdinalIgnoreCase) ? 20 : 18
                });
            }

            // [last] Function (Xem chi tiết | Sửa | Xóa)
            dgvPhieuMuon.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Function",
                HeaderText = "Chức năng",
                ReadOnly = true,
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
                FillWeight = 18
            });
        }

        private void EnsureHiddenIdColumn()
        {
            if (_idColumnName != null && dgvPhieuMuon.Columns.Contains(_idColumnName))
                dgvPhieuMuon.Columns[_idColumnName].Visible = false;

            if (dgvPhieuMuon.Columns.Contains("DG_ID"))
                dgvPhieuMuon.Columns["DG_ID"].Visible = false;
        }

        private void ArrangeColumns()
        {
            int idx = 2;
            void SetIndex(string name)
            {
                if (dgvPhieuMuon.Columns.Contains(name))
                    dgvPhieuMuon.Columns[name].DisplayIndex = idx++;
            }

            SetIndex("DocGia");
            SetIndex("NgayMuon");
            SetIndex("NgayTra");
            SetIndex("TinhTrang");

            if (dgvPhieuMuon.Columns.Contains("Function"))
                dgvPhieuMuon.Columns["Function"].DisplayIndex = dgvPhieuMuon.Columns.Count - 1;
        }

        private void LocalizeHeaders()
        {
            foreach (DataGridViewColumn c in dgvPhieuMuon.Columns)
            {
                if (c.Name is "Select" or "STT" or "Function") continue;
                c.HeaderText = ToVietnameseHeader(c.Name);
            }
        }

        private static string ToVietnameseHeader(string col) => col.ToLowerInvariant() switch
        {
            "pm_id"     => "Mã phiếu mượn",
            "dg_id"     => "Độc giả",
            "docgia"    => "Độc giả",
            "ngaymuon"  => "Ngày mượn",
            "ngaytra"   => "Ngày trả",
            "tinhtrang" => "Tình trạng",
            _           => SplitPascal(col)
        };

        private static string SplitPascal(string s)
        {
            var chars = new List<char>(s.Length * 2);
            for (int i = 0; i < s.Length; i++)
            {
                char c = s[i];
                if (i > 0 && char.IsUpper(c) && (char.IsLower(s[i - 1]) || (i + 1 < s.Length && char.IsLower(s[i + 1]))))
                    chars.Add(' ');
                chars.Add(c);
            }
            return new string(chars.ToArray());
        }

        private void UpdateEmptyState() => lblEmpty.Visible = _pm.Rows.Count == 0;

        private void UpdateSttValues()
        {
            var sttCol = dgvPhieuMuon.Columns["STT"];
            if (sttCol == null) return;

            for (int i = 0; i < dgvPhieuMuon.Rows.Count; i++)
            {
                var row = dgvPhieuMuon.Rows[i];
                if (!row.IsNewRow) row.Cells[sttCol.Index].Value = (i + 1).ToString();
            }
        }

        private void Dgv_CurrentCellDirtyStateChanged(object? sender, EventArgs e)
        {
            if (dgvPhieuMuon.IsCurrentCellDirty && dgvPhieuMuon.CurrentCell is DataGridViewCheckBoxCell)
                dgvPhieuMuon.CommitEdit(DataGridViewDataErrorContexts.Commit);
        }

        private void Dgv_CellValueChanged(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0 && dgvPhieuMuon.Columns[e.ColumnIndex].Name == "Select")
                UpdateBulkDeleteButtonState();
        }

        private void UpdateBulkDeleteButtonState()
        {
            var selectCol = dgvPhieuMuon.Columns["Select"];
            if (selectCol == null) { btnXoaNhieu.Enabled = false; return; }

            bool anyChecked = dgvPhieuMuon.Rows
                .Cast<DataGridViewRow>()
                .Any(r => Convert.ToBoolean(r.Cells[selectCol.Index].Value ?? false));

            btnXoaNhieu.Enabled = anyChecked;
        }

        // ===== Inline buttons: Xem chi tiết | Sửa | Xóa =====
        private void Dgv_CellPainting(object? sender, DataGridViewCellPaintingEventArgs e)
        {
            if (e.RowIndex < 0) return;
            if (dgvPhieuMuon.Columns[e.ColumnIndex].Name != "Function") return;

            e.PaintBackground(e.ClipBounds, true);
            var cell = e.CellBounds;
            int pad = Math.Max(2, cell.Height / 10);
            int w = (cell.Width - (pad * 4)) / 3;
            int h = cell.Height - (pad * 2);

            var viewR = new Rectangle(cell.X + pad, cell.Y + pad, w, h);
            var editR = new Rectangle(viewR.Right + pad, cell.Y + pad, w, h);
            var delR  = new Rectangle(editR.Right + pad,  cell.Y + pad, w, h);

            ButtonRenderer.DrawButton(e.Graphics, viewR, "Xem", e.CellStyle.Font, false,
                System.Windows.Forms.VisualStyles.PushButtonState.Default);
            ButtonRenderer.DrawButton(e.Graphics, editR, "Sửa", e.CellStyle.Font, false,
                System.Windows.Forms.VisualStyles.PushButtonState.Default);
            ButtonRenderer.DrawButton(e.Graphics, delR,  "Xóa", e.CellStyle.Font, false,
                System.Windows.Forms.VisualStyles.PushButtonState.Default);

            e.Handled = true;
        }

        private void Dgv_CellMouseClick(object? sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.RowIndex < 0) return;
            if (e.Button != MouseButtons.Left) return;
            if (dgvPhieuMuon.Columns[e.ColumnIndex].Name != "Function") return;

            var cell = dgvPhieuMuon.GetCellDisplayRectangle(e.ColumnIndex, e.RowIndex, false);
            int pad = Math.Max(2, cell.Height / 10);
            int w = (cell.Width - (pad * 4)) / 3;
            var viewR = new Rectangle(cell.X + pad, cell.Y + pad, w, cell.Height - (pad * 2));
            var editR = new Rectangle(viewR.Right + pad, cell.Y + pad, w, cell.Height - (pad * 2));
            var delR  = new Rectangle(editR.Right + pad,  cell.Y + pad, w, cell.Height - (pad * 2));
            var click = dgvPhieuMuon.PointToClient(Cursor.Position);

            if (viewR.Contains(click)) ViewDetails(e.RowIndex);
            else if (editR.Contains(click)) EditRow(e.RowIndex);
            else if (delR.Contains(click)) DeleteSingle(e.RowIndex);
        }

        private void ViewDetails(int rowIndex)
        {
            var id = GetRowId(dgvPhieuMuon.Rows[rowIndex]);
            if (id == null) { MessageBox.Show("Không tìm thấy ID bản ghi.", "Lỗi"); return; }

            using var f = new ChiTietPhieuMuonForm(id.Value);
            f.ShowDialog(this);
        }

        private void EditRow(int rowIndex)
        {
            var id = GetRowId(dgvPhieuMuon.Rows[rowIndex]);
            if (id == null) { MessageBox.Show("Không tìm thấy ID bản ghi.", "Lỗi"); return; }

            using var f = new SuaPhieuMuonForm(id.Value);
            if (f.ShowDialog(this) == DialogResult.OK)
                ReloadPhieuMuon(txtSearch.Text?.Trim());
        }

        private void DeleteSingle(int rowIndex)
        {
            var id = GetRowId(dgvPhieuMuon.Rows[rowIndex]);
            if (id == null) { MessageBox.Show("Không tìm thấy ID bản ghi.", "Lỗi"); return; }

            var confirm = MessageBox.Show("Bạn có chắc muốn xóa bản ghi này?",
                "Xóa phiếu mượn", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (confirm != DialogResult.Yes) return;

            ExecuteDeleteByIds(new[] { id.Value });
            ReloadPhieuMuon(txtSearch.Text?.Trim());
        }

        private int? GetRowId(DataGridViewRow row)
        {
            try
            {
                if (_idColumnName == null) return null;

                if (row.DataBoundItem is DataRowView drv)
                {
                    var obj = drv.Row[_idColumnName];
                    if (obj != DBNull.Value && obj != null)
                    {
                        if (obj is int i) return i;
                        if (obj is long l) return checked((int)l);
                        if (int.TryParse(obj.ToString(), out var p)) return p;
                    }
                }
                if (dgvPhieuMuon.Columns.Contains(_idColumnName))
                {
                    var val = row.Cells[_idColumnName].Value;
                    if (val != null && val != DBNull.Value)
                    {
                        if (val is int i2) return i2;
                        if (val is long l2) return checked((int)l2);
                        if (int.TryParse(val.ToString(), out var p2)) return p2;
                    }
                }
                if (dgvPhieuMuon.Columns.Contains("ID"))
                {
                    var val = row.Cells["ID"].Value;
                    if (val != null && int.TryParse(val.ToString(), out var p3)) return p3;
                }
            }
            catch { }
            return null;
        }

        private void ExecuteDeleteByIds(int[] ids)
        {
            if (_tableName == null || _idColumnName == null) return;

            using var conn = Db.Create(); conn.Open();
            var parms = ids.Select((_, i) => $"@p{i}").ToArray();
            using var cmd = new SqlCommand($"delete from {_tableName} where {_idColumnName} in ({string.Join(",", parms)})", conn);
            for (int i = 0; i < ids.Length; i++)
                cmd.Parameters.Add(new SqlParameter(parms[i], SqlDbType.Int) { Value = ids[i] });
            cmd.ExecuteNonQuery();
        }
    }
}
