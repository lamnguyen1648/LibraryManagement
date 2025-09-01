using System.Data;
using LibraryManagement.Forms.NhanVienRelated.QuanLyChucVu;
using LibraryManagement.Forms.Operations;
using Microsoft.Data.SqlClient;

namespace LibraryManagement.Forms.NhanVienRelated.QuanLyNhanVien
{
    public partial class QuanLyNhanVienForm : Form
    {
        private string? _tableName;     // NhanVien
        private string? _idColumnName;  // NV_ID | ID
        private string? _nameColumn;    // TenNV
        private string? _emailColumn;   // Mail | Email
        private string? _phoneColumn;   // SoDienThoai | DienThoai | SDT | Phone
        private string? _roleColumn;    // CV_ID | ChucVu_ID | ChucVu
        private string? _statusColumn;  // Status | TrangThai | IsActive | Active

        // Lookup info for Chức vụ
        private string? _roleLookupTable;   // ChucVu
        private string? _rolePk;            // CV_ID | ID
        private string? _roleName;          // TenCV | TenChucVu | Ten

        private readonly DataTable _nv = new();
        private readonly BindingSource _bs = new();

        private static readonly string[] TableCandidates = { "NhanVien" };
        private static readonly string[] IdCandidates    = { "NV_ID", "ID" };
        private static readonly string[] NameCandidates  = { "TenNV", "HoTen", "Ten" };
        private static readonly string[] EmailCandidates = { "Mail", "Email" };
        private static readonly string[] PhoneCandidates = { "SoDienThoai", "DienThoai", "SDT", "Phone" };
        private static readonly string[] StatusCandidates = { "Status", "TrangThai", "IsActive", "Active" };

        public QuanLyNhanVienForm()
        {
            InitializeComponent();

            // Hard gate — if someone tries to open this form directly
            Load += (_, __) =>
            {
                if (!Authorization.CanAccess(Authorization.FeatureQuanLyNhanVien))
                {
                    MessageBox.Show("Bạn không có quyền truy cập chức năng này.",
                        "Từ chối truy cập", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    DialogResult = DialogResult.Cancel;
                    Close();
                    return;
                }
                ReloadNhanVien();
            };

            btnQuanLyChucVu.Click += BtnQuanLyChucVu_Click;
            btnThem.Click         += BtnThem_Click;
            btnXoaNhieu.Click     += BtnXoaNhieu_Click;
            btnTimKiem.Click      += BtnTimKiem_Click;

            btnXoaNhieu.Enabled = false;

            dgvNV.AutoGenerateColumns = false;
            dgvNV.CurrentCellDirtyStateChanged += Dgv_CurrentCellDirtyStateChanged;
            dgvNV.CellValueChanged             += Dgv_CellValueChanged;
            dgvNV.DataBindingComplete          += (_, __) => UpdateSttValues();
            dgvNV.Sorted                       += (_, __) => UpdateSttValues();
            dgvNV.CellPainting                 += Dgv_CellPainting;
            dgvNV.CellMouseClick               += Dgv_CellMouseClick;

            _bs.DataSource = _nv;
            dgvNV.DataSource = _bs;
        }

        private void BtnQuanLyChucVu_Click(object? s, EventArgs e)
        {
            using var f = new QuanLyChucVuForm();
            Hide();
            try { _ = f.ShowDialog(this); }
            finally { Show(); Activate(); }
        }

        private void BtnThem_Click(object? s, EventArgs e)
        {
            using var f = new ThemNhanVienForm();
            if (f.ShowDialog(this) == DialogResult.OK)
                ReloadNhanVien(txtSearch.Text?.Trim());
        }

        private void BtnTimKiem_Click(object? s, EventArgs e) => ReloadNhanVien(txtSearch.Text?.Trim());

        private void BtnXoaNhieu_Click(object? s, EventArgs e)
        {
            var selectCol = dgvNV.Columns["Select"];
            if (selectCol == null) return;

            var ids = dgvNV.Rows.Cast<DataGridViewRow>()
                .Where(r => Convert.ToBoolean((bool)(r.Cells[selectCol.Index].Value ?? false)))
                .Select(GetRowId).Where(v => v.HasValue).Select(v => v!.Value).ToArray();

            if (ids.Length == 0) return;
            if (MessageBox.Show($"Xóa {ids.Length} nhân viên đã chọn?", "Xóa nhiều nhân viên",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;

            ExecuteDeleteByIds(ids);
            ReloadNhanVien(txtSearch.Text?.Trim());
        }

        private void ReloadNhanVien(string? search = null)
        {
            _nv.Clear();

            using var conn = Db.Create();
            using var cmd  = conn.CreateCommand();

            if (_tableName == null || _idColumnName == null || _nameColumn == null)
                ResolveTableAndColumns(conn);

            if (_tableName == null)
            {
                MessageBox.Show("Không tìm thấy bảng Nhân viên.", "Lỗi");
                return;
            }

            if (!string.IsNullOrWhiteSpace(search) && _nameColumn != null)
            {
                cmd.CommandText = $"SELECT * FROM {_tableName} WHERE {_nameColumn} LIKE N'%' + @q + N'%'";
                cmd.Parameters.Add(new SqlParameter("@q", SqlDbType.NVarChar, 255) { Value = search! });
            }
            else
            {
                cmd.CommandText = $"SELECT * FROM {_tableName}";
            }

            using var da = new SqlDataAdapter(cmd);
            da.Fill(_nv);

            // Add friendly columns
            ApplyRoleNames();
            ApplyStatusText();

            BuildGridColumns();
            LocalizeHeaders();
            HideSensitiveColumns();
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
                    using var probe = new SqlCommand($"SELECT TOP(1) * FROM {table}", conn);
                    using var da    = new SqlDataAdapter(probe);
                    var dt = new DataTable();
                    da.Fill(dt);
                    if (dt.Columns.Count == 0) continue;

                    _tableName    = table;
                    _idColumnName = IdCandidates.FirstOrDefault(c => dt.Columns.Contains(c)) ?? dt.Columns[0].ColumnName;
                    _nameColumn   = NameCandidates.FirstOrDefault(c => dt.Columns.Contains(c))
                                    ?? dt.Columns.Cast<DataColumn>().FirstOrDefault(c => c.DataType == typeof(string))?.ColumnName;
                    _emailColumn  = EmailCandidates.FirstOrDefault(c => dt.Columns.Contains(c));
                    _phoneColumn  = PhoneCandidates.FirstOrDefault(c => dt.Columns.Contains(c));
                    _roleColumn   = new[] { "CV_ID", "ChucVu_ID", "ChucVu" }.FirstOrDefault(dt.Columns.Contains);
                    _statusColumn = StatusCandidates.FirstOrDefault(dt.Columns.Contains);

                    ResolveRoleLookup(conn);
                    return;
                }
                catch { /* try next */ }
            }
        }

        private void ResolveRoleLookup(SqlConnection conn)
        {
            try
            {
                using var probe = new SqlCommand("SELECT TOP(1) * FROM ChucVu", conn);
                using var da = new SqlDataAdapter(probe);
                var dt = new DataTable();
                da.Fill(dt);
                if (dt.Columns.Count == 0) return;

                _roleLookupTable = "ChucVu";
                _rolePk   = new[] { "CV_ID", "ID" }.FirstOrDefault(dt.Columns.Contains) ?? dt.Columns[0].ColumnName;
                _roleName = new[] { "TenCV", "TenChucVu", "Ten" }.FirstOrDefault(dt.Columns.Contains)
                            ?? dt.Columns.Cast<DataColumn>().FirstOrDefault(c => c.DataType == typeof(string))?.ColumnName;
            }
            catch { /* ignore */ }
        }

        private void ApplyRoleNames()
        {
            if (_roleColumn == null) return;

            if (_roleLookupTable == null || _rolePk == null || _roleName == null)
            {
                using var conn = Db.Create();
                conn.Open();
                ResolveRoleLookup(conn);
                if (_roleLookupTable == null || _rolePk == null || _roleName == null) return;
            }

            var dict = new System.Collections.Generic.Dictionary<string, string>();
            try
            {
                using var conn = Db.Create();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = $"SELECT {_rolePk}, {_roleName} FROM {_roleLookupTable}";
                using var da = new SqlDataAdapter(cmd);
                var dt = new DataTable();
                da.Fill(dt);
                foreach (DataRow r in dt.Rows)
                {
                    var key = r[_rolePk]?.ToString() ?? "";
                    var val = r[_roleName]?.ToString() ?? "";
                    if (!dict.ContainsKey(key)) dict[key] = val;
                }
            }
            catch { /* ignore */ }

            if (!_nv.Columns.Contains("ChucVuText"))
                _nv.Columns.Add("ChucVuText", typeof(string));

            foreach (DataRow r in _nv.Rows)
            {
                var key = r[_roleColumn]?.ToString() ?? "";
                dict.TryGetValue(key, out var name);
                r["ChucVuText"] = name ?? "";
            }
        }

        private void ApplyStatusText()
        {
            if (_statusColumn == null || !_nv.Columns.Contains(_statusColumn)) return;

            if (!_nv.Columns.Contains("TrangThaiText"))
                _nv.Columns.Add("TrangThaiText", typeof(string));

            foreach (DataRow r in _nv.Rows)
            {
                var v = r[_statusColumn];
                var on = v != DBNull.Value && v != null && Convert.ToBoolean(v);
                r["TrangThaiText"] = on ? "Kích hoạt" : "Vô hiệu";
            }
        }

        // Build fixed order columns:
        // Chọn | STT | (hidden ID) | Tên NV | Chức vụ | Email | SĐT | Trạng thái | Chức năng
        private void BuildGridColumns()
        {
            dgvNV.Columns.Clear();

            dgvNV.Columns.Add(new DataGridViewCheckBoxColumn
            {
                Name = "Select", HeaderText = "Chọn",
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill, FillWeight = 8
            });
            dgvNV.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "STT", HeaderText = "STT", ReadOnly = true,
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill, FillWeight = 7
            });

            if (_idColumnName != null)
            {
                dgvNV.Columns.Add(new DataGridViewTextBoxColumn
                {
                    Name = _idColumnName, DataPropertyName = _idColumnName,
                    Visible = false, ReadOnly = true
                });
            }

            if (_nameColumn != null && _nv.Columns.Contains(_nameColumn))
                dgvNV.Columns.Add(new DataGridViewTextBoxColumn
                {
                    Name = _nameColumn, DataPropertyName = _nameColumn,
                    HeaderText = "Tên nhân viên", ReadOnly = true,
                    AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill, FillWeight = 30
                });

            if (_nv.Columns.Contains("ChucVuText"))
                dgvNV.Columns.Add(new DataGridViewTextBoxColumn
                {
                    Name = "ChucVuText", DataPropertyName = "ChucVuText",
                    HeaderText = "Chức vụ", ReadOnly = true,
                    AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill, FillWeight = 16
                });

            if (_emailColumn != null && _nv.Columns.Contains(_emailColumn))
                dgvNV.Columns.Add(new DataGridViewTextBoxColumn
                {
                    Name = _emailColumn, DataPropertyName = _emailColumn,
                    HeaderText = "Email", ReadOnly = true,
                    AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill, FillWeight = 20
                });

            if (_phoneColumn != null && _nv.Columns.Contains(_phoneColumn))
                dgvNV.Columns.Add(new DataGridViewTextBoxColumn
                {
                    Name = _phoneColumn, DataPropertyName = _phoneColumn,
                    HeaderText = "Số điện thoại", ReadOnly = true,
                    AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill, FillWeight = 17
                });

            // New: Trạng thái (friendly text)
            if (_nv.Columns.Contains("TrangThaiText"))
                dgvNV.Columns.Add(new DataGridViewTextBoxColumn
                {
                    Name = "TrangThaiText", DataPropertyName = "TrangThaiText",
                    HeaderText = "Trạng thái", ReadOnly = true,
                    AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill, FillWeight = 12
                });

            // Any remaining columns except the ones we handled & sensitive
            foreach (DataColumn dc in _nv.Columns)
            {
                var n = dc.ColumnName;
                if (_idColumnName != null && n.Equals(_idColumnName, StringComparison.OrdinalIgnoreCase)) continue;
                if (_nameColumn != null && n.Equals(_nameColumn, StringComparison.OrdinalIgnoreCase)) continue;
                if (_roleColumn != null && n.Equals(_roleColumn, StringComparison.OrdinalIgnoreCase)) continue;
                if (_emailColumn != null && n.Equals(_emailColumn, StringComparison.OrdinalIgnoreCase)) continue;
                if (_phoneColumn != null && n.Equals(_phoneColumn, StringComparison.OrdinalIgnoreCase)) continue;
                if (_statusColumn != null && n.Equals(_statusColumn, StringComparison.OrdinalIgnoreCase)) continue;
                if (n is "ChucVuText" or "TrangThaiText") continue;
                if (n is "DiaChi" or "MatKhau") continue;

                dgvNV.Columns.Add(new DataGridViewTextBoxColumn
                {
                    Name = n, DataPropertyName = n,
                    HeaderText = ToVietnameseHeader(n), ReadOnly = true,
                    AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill, FillWeight = 10
                });
            }

            dgvNV.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Function", HeaderText = "Chức năng", ReadOnly = true,
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill, FillWeight = 12
            });
        }

        private void LocalizeHeaders()
        {
            foreach (DataGridViewColumn c in dgvNV.Columns)
            {
                if (c.Name is "Select" or "STT" or "Function") continue;
                if (c.Name.Equals("ChucVuText", StringComparison.OrdinalIgnoreCase)) { c.HeaderText = "Chức vụ"; continue; }
                if (c.Name.Equals("TrangThaiText", StringComparison.OrdinalIgnoreCase)) { c.HeaderText = "Trạng thái"; continue; }
                c.HeaderText = ToVietnameseHeader(c.Name);
            }
        }

        private void HideSensitiveColumns()
        {
            if (dgvNV.Columns.Contains("MatKhau")) dgvNV.Columns["MatKhau"].Visible = false;
            if (dgvNV.Columns.Contains("DiaChi"))  dgvNV.Columns["DiaChi"].Visible  = false;
            if (_roleColumn != null && dgvNV.Columns.Contains(_roleColumn)) dgvNV.Columns[_roleColumn].Visible = false;
            if (_statusColumn != null && dgvNV.Columns.Contains(_statusColumn)) dgvNV.Columns[_statusColumn].Visible = false;
        }

        private static string ToVietnameseHeader(string col) => col.ToLowerInvariant() switch
        {
            "tennv" or "hoten" or "ten" => "Tên nhân viên",
            "mail" or "email" => "Email",
            "sodienthoai" or "dienthoai" or "sdt" or "phone" => "Số điện thoại",
            "cv_id" or "chucvu_id" or "chucvu" => "Chức vụ",
            "trangthai" or "status" => "Trạng thái",
            "chucvutext" => "Chức vụ",
            "trangthaitext" => "Trạng thái",
            _ => SplitPascal(col)
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

        private void UpdateEmptyState() => lblEmpty.Visible = _nv.Rows.Count == 0;

        private void UpdateSttValues()
        {
            var sttCol = dgvNV.Columns["STT"];
            if (sttCol == null) return;

            for (int i = 0; i < dgvNV.Rows.Count; i++)
            {
                var row = dgvNV.Rows[i];
                if (!row.IsNewRow)
                    row.Cells[sttCol.Index].Value = (i + 1).ToString();
            }
        }

        private void Dgv_CurrentCellDirtyStateChanged(object? sender, EventArgs e)
        {
            if (dgvNV.IsCurrentCellDirty && dgvNV.CurrentCell is DataGridViewCheckBoxCell)
                dgvNV.CommitEdit(DataGridViewDataErrorContexts.Commit);
        }

        private void Dgv_CellValueChanged(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0 && dgvNV.Columns[e.ColumnIndex].Name == "Select")
                UpdateBulkDeleteButtonState();
        }

        private void UpdateBulkDeleteButtonState()
        {
            var selectCol = dgvNV.Columns["Select"];
            if (selectCol == null) { btnXoaNhieu.Enabled = false; return; }

            bool anyChecked = dgvNV.Rows.Cast<DataGridViewRow>()
                .Any(r => Convert.ToBoolean((bool)(r.Cells[selectCol.Index].Value ?? false)));

            btnXoaNhieu.Enabled = anyChecked;
        }

        private void Dgv_CellPainting(object? sender, DataGridViewCellPaintingEventArgs e)
        {
            if (e.RowIndex < 0) return;
            if (dgvNV.Columns[e.ColumnIndex].Name != "Function") return;

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

        private void Dgv_CellMouseClick(object? sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.RowIndex < 0) return;
            if (e.Button != MouseButtons.Left) return;
            if (dgvNV.Columns[e.ColumnIndex].Name != "Function") return;

            var cell  = dgvNV.GetCellDisplayRectangle(e.ColumnIndex, e.RowIndex, false);
            int pad   = Math.Max(2, cell.Height / 10);
            int w     = (cell.Width - (pad * 3)) / 2;
            var editR = new Rectangle(cell.X + pad, cell.Y + pad, w, cell.Height - (pad * 2));
            var delR  = new Rectangle(editR.Right + pad, cell.Y + pad, w, cell.Height - (pad * 2));
            var click = dgvNV.PointToClient(Cursor.Position);

            if (editR.Contains(click)) EditRow(e.RowIndex);
            else if (delR.Contains(click)) DeleteSingle(e.RowIndex);
        }

        private void EditRow(int rowIndex)
        {
            var row = dgvNV.Rows[rowIndex];
            var id  = GetRowId(row);
            if (id == null) { MessageBox.Show("Không tìm thấy ID bản ghi.", "Lỗi"); return; }

            using var f = new SuaNhanVienForm(id.Value);
            if (f.ShowDialog(this) == DialogResult.OK)
                ReloadNhanVien(txtSearch.Text?.Trim());
        }

        private void DeleteSingle(int rowIndex)
        {
            var row = dgvNV.Rows[rowIndex];
            var id  = GetRowId(row);
            if (id == null) { MessageBox.Show("Không tìm thấy ID bản ghi.", "Lỗi"); return; }

            if (MessageBox.Show("Bạn có chắc muốn xóa bản ghi này?", "Xóa nhân viên",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;

            ExecuteDeleteByIds(new[] { id.Value });
            ReloadNhanVien(txtSearch.Text?.Trim());
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
                        if (int.TryParse(obj.ToString(), out var parsed)) return parsed;
                    }
                }
                if (dgvNV.Columns.Contains(_idColumnName))
                {
                    var val = row.Cells[_idColumnName].Value;
                    if (val != null && val != DBNull.Value)
                    {
                        if (val is int i2) return i2;
                        if (val is long l2) return checked((int)l2);
                        if (int.TryParse(val.ToString(), out var parsed2)) return parsed2;
                    }
                }
            }
            catch { }
            return null;
        }

        private void ExecuteDeleteByIds(int[] ids)
        {
            if (_tableName == null || _idColumnName == null) return;

            using var conn = Db.Create();
            conn.Open();

            var parms = ids.Select((_, i) => $"@p{i}").ToArray();
            var sql = $"DELETE FROM {_tableName} WHERE {_idColumnName} IN ({string.Join(",", parms)})";

            using var cmd = new SqlCommand(sql, conn);
            for (int i = 0; i < ids.Length; i++)
                cmd.Parameters.Add(new SqlParameter(parms[i], SqlDbType.Int) { Value = ids[i] });

            cmd.ExecuteNonQuery();
        }
    }
}
