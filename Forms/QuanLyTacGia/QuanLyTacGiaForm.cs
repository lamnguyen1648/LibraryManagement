using System.Data;
using Microsoft.Data.SqlClient;

namespace LibraryManagement.Forms.QuanLyTacGia
{
    public partial class QuanLyTacGiaForm : Form
    {
        // Auto-resolved to fit your DB
        private string? _tableName;     // "TacGia"
        private string? _idColumnName;  // "TG_ID" or "ID"
        private string? _nameColumn;    // "TenTG" | "HoTen" | "Ten" | "TenTacGia"

        private readonly DataTable _tg = new();
        private readonly BindingSource _bs = new();

        private static readonly string[] TableCandidates = { "TacGia" };
        private static readonly string[] IdCandidates    = { "TG_ID", "ID" };
        private static readonly string[] NameCandidates  = { "TenTG", "HoTen", "TenTacGia", "Ten" };

        public QuanLyTacGiaForm()
        {
            InitializeComponent();

            Load              += (_, __) => ReloadTacGia();
            btnThem.Click     -= BtnThem_Click;     btnThem.Click     += BtnThem_Click;
            btnXoaNhieu.Click -= BtnXoaNhieu_Click; btnXoaNhieu.Click += BtnXoaNhieu_Click;
            btnTimKiem.Click  -= BtnTimKiem_Click;  btnTimKiem.Click  += BtnTimKiem_Click;

            btnXoaNhieu.Enabled = false;

            dgvTacGia.AutoGenerateColumns = false;
            dgvTacGia.CurrentCellDirtyStateChanged += Dgv_CurrentCellDirtyStateChanged;
            dgvTacGia.CellValueChanged             += Dgv_CellValueChanged;
            dgvTacGia.DataBindingComplete          += (_, __) => UpdateSttValues();
            dgvTacGia.Sorted                       += (_, __) => UpdateSttValues();

            dgvTacGia.CellPainting   += Dgv_CellPainting;   // draw Sửa/Xóa
            dgvTacGia.CellMouseClick += Dgv_CellMouseClick; // clicks

            _bs.DataSource = _tg;
            dgvTacGia.DataSource = _bs;
        }

        // ===== Toolbar =====
        private void BtnThem_Click(object? s, EventArgs e)
        {
            using (var f = new ThemTacGiaForm())
            {
                if (f.ShowDialog(this) == DialogResult.OK)
                    ReloadTacGia(txtSearch.Text?.Trim());
            }
        }
        private void BtnTimKiem_Click(object? s, EventArgs e) => ReloadTacGia(txtSearch.Text?.Trim());

        private void BtnXoaNhieu_Click(object? s, EventArgs e)
        {
            var selectCol = dgvTacGia.Columns["Select"];
            if (selectCol == null) return;

            var ids = dgvTacGia.Rows
                .Cast<DataGridViewRow>()
                .Where(r => Convert.ToBoolean(r.Cells[selectCol.Index].Value ?? false))
                .Select(GetRowId)
                .Where(v => v.HasValue)
                .Select(v => v!.Value)
                .ToArray();

            if (ids.Length == 0) return;

            var confirm = MessageBox.Show($"Xóa {ids.Length} tác giả đã chọn?", "Xóa nhiều tác giả",
                MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (confirm != DialogResult.Yes) return;

            ExecuteDeleteByIds(ids);
            ReloadTacGia(txtSearch.Text?.Trim());
        }

        // ===== Data =====
        private void ReloadTacGia(string? search = null)
        {
            _tg.Clear();

            using var conn = Db.Create();
            using var cmd  = conn.CreateCommand();

            if (_tableName == null || _idColumnName == null || _nameColumn == null)
                ResolveTableAndColumns(conn);

            if (_tableName == null)
            {
                MessageBox.Show("Không tìm thấy bảng Tác giả.", "Lỗi");
                return;
            }

            if (!string.IsNullOrWhiteSpace(search) && _nameColumn != null)
            {
                cmd.CommandText = $"SELECT * FROM {_tableName} WHERE {_nameColumn} LIKE N'%' + @q + N'%'";
                cmd.Parameters.Add(new SqlParameter("@q", SqlDbType.NVarChar, 255) { Value = search! });
            }
            else cmd.CommandText = $"SELECT * FROM {_tableName}";

            using var da = new SqlDataAdapter(cmd);
            da.Fill(_tg);

            BuildGridColumnsIfNeeded();
            EnsureHiddenIdColumn();
            LocalizeHeaders();
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
                    var dt = new DataTable(); da.Fill(dt);
                    if (dt.Columns.Count == 0) continue;

                    _tableName    = table;
                    _idColumnName = IdCandidates.FirstOrDefault(c => dt.Columns.Contains(c)) ?? dt.Columns[0].ColumnName;
                    _nameColumn   = NameCandidates.FirstOrDefault(c => dt.Columns.Contains(c))
                                    ?? dt.Columns.Cast<DataColumn>().FirstOrDefault(c => c.DataType == typeof(string))?.ColumnName;
                    return;
                }
                catch { /* try next */ }
            }
        }

        // ===== Grid =====
        private void BuildGridColumnsIfNeeded()
        {
            if (dgvTacGia.Columns.Count > 0) return;

            // [0] Checkbox
            dgvTacGia.Columns.Add(new DataGridViewCheckBoxColumn
            {
                Name = "Select",
                HeaderText = "Chọn",
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
                FillWeight = 8
            });

            // [1] STT
            dgvTacGia.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "STT",
                HeaderText = "STT",
                ReadOnly = true,
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
                FillWeight = 7
            });

            // (Hidden ID inserted at [2] by EnsureHiddenIdColumn)

            // Name first (if exists)
            if (_nameColumn != null && _tg.Columns.Contains(_nameColumn))
            {
                dgvTacGia.Columns.Add(new DataGridViewTextBoxColumn
                {
                    Name = _nameColumn,
                    DataPropertyName = _nameColumn,
                    HeaderText = "Tên tác giả",
                    ReadOnly = true,
                    AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
                    FillWeight = 36
                });
            }

            // Other visible DB columns (exclude ID & name)
            foreach (DataColumn dc in _tg.Columns)
            {
                if (_idColumnName != null && dc.ColumnName.Equals(_idColumnName, StringComparison.OrdinalIgnoreCase)) continue;
                if (_nameColumn != null && dc.ColumnName.Equals(_nameColumn, StringComparison.OrdinalIgnoreCase)) continue;

                dgvTacGia.Columns.Add(new DataGridViewTextBoxColumn
                {
                    Name = dc.ColumnName,
                    DataPropertyName = dc.ColumnName,
                    HeaderText = ToVietnameseHeader(dc.ColumnName),
                    ReadOnly = true,
                    AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
                    FillWeight = 18
                });
            }

            // [last] Function
            dgvTacGia.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Function",
                HeaderText = "Chức năng",
                ReadOnly = true,
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
                FillWeight = 12
            });
        }

        private void EnsureHiddenIdColumn()
        {
            if (_idColumnName == null) return;

            if (!dgvTacGia.Columns.Contains(_idColumnName))
            {
                dgvTacGia.Columns.Insert(2, new DataGridViewTextBoxColumn
                {
                    Name = _idColumnName,
                    DataPropertyName = _idColumnName,
                    Visible = false,
                    ReadOnly = true
                });
            }
            else
            {
                dgvTacGia.Columns[_idColumnName].Visible = false;
                dgvTacGia.Columns[_idColumnName].ReadOnly = true;
            }
        }

        private void LocalizeHeaders()
        {
            foreach (DataGridViewColumn c in dgvTacGia.Columns)
            {
                if (c.Name is "Select" or "STT" or "Function") continue;
                c.HeaderText = ToVietnameseHeader(c.Name);
            }
        }

        private static string ToVietnameseHeader(string col) => col.ToLowerInvariant() switch
        {
            "tentg" or "hoten" or "ten" or "tentacgia" => "Tên tác giả",
            "ngaysinh" => "Ngày sinh",
            "gioitinh" => "Giới tính",
            "quoctich" => "Quốc tịch",
            "email" or "mail" => "Email",
            "sodienthoai" or "dienthoai" or "sdt" or "phone" => "Số điện thoại",
            "diachi" => "Địa chỉ",
            "website" or "web" => "Website",
            "ghichu" => "Ghi chú",
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

        private void UpdateEmptyState() => lblEmpty.Visible = _tg.Rows.Count == 0;

        private void UpdateSttValues()
        {
            var sttCol = dgvTacGia.Columns["STT"];
            if (sttCol == null) return;

            for (int i = 0; i < dgvTacGia.Rows.Count; i++)
            {
                var row = dgvTacGia.Rows[i];
                if (!row.IsNewRow) row.Cells[sttCol.Index].Value = (i + 1).ToString();
            }
        }

        private void Dgv_CurrentCellDirtyStateChanged(object? sender, EventArgs e)
        {
            if (dgvTacGia.IsCurrentCellDirty && dgvTacGia.CurrentCell is DataGridViewCheckBoxCell)
                dgvTacGia.CommitEdit(DataGridViewDataErrorContexts.Commit);
        }

        private void Dgv_CellValueChanged(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0 && dgvTacGia.Columns[e.ColumnIndex].Name == "Select")
                UpdateBulkDeleteButtonState();
        }

        private void UpdateBulkDeleteButtonState()
        {
            var selectCol = dgvTacGia.Columns["Select"];
            if (selectCol == null) { btnXoaNhieu.Enabled = false; return; }

            bool anyChecked = dgvTacGia.Rows
                .Cast<DataGridViewRow>()
                .Any(r => Convert.ToBoolean(r.Cells[selectCol.Index].Value ?? false));

            btnXoaNhieu.Enabled = anyChecked;
        }

        // ===== Sửa/Xóa inline =====
        private void Dgv_CellPainting(object? sender, DataGridViewCellPaintingEventArgs e)
        {
            if (e.RowIndex < 0) return;
            if (dgvTacGia.Columns[e.ColumnIndex].Name != "Function") return;

            e.PaintBackground(e.ClipBounds, true);
            var cell = e.CellBounds;
            int pad = Math.Max(2, cell.Height / 10);
            int w = (cell.Width - (pad * 3)) / 2;
            int h = cell.Height - (pad * 2);

            var editR = new Rectangle(cell.X + pad, cell.Y + pad, w, h);
            var delR  = new Rectangle(editR.Right + pad, cell.Y + pad, w, h);

            ButtonRenderer.DrawButton(e.Graphics, editR, "Sửa", e.CellStyle.Font, false,
                System.Windows.Forms.VisualStyles.PushButtonState.Default);
            ButtonRenderer.DrawButton(e.Graphics, delR, "Xóa", e.CellStyle.Font, false,
                System.Windows.Forms.VisualStyles.PushButtonState.Default);

            e.Handled = true;
        }

        private void Dgv_CellMouseClick(object? sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.RowIndex < 0) return;
            if (e.Button != MouseButtons.Left) return;
            if (dgvTacGia.Columns[e.ColumnIndex].Name != "Function") return;

            var cell = dgvTacGia.GetCellDisplayRectangle(e.ColumnIndex, e.RowIndex, false);
            int pad = Math.Max(2, cell.Height / 10);
            int w = (cell.Width - (pad * 3)) / 2;
            var editR = new Rectangle(cell.X + pad, cell.Y + pad, w, cell.Height - (pad * 2));
            var delR  = new Rectangle(editR.Right + pad, cell.Y + pad, w, cell.Height - (pad * 2));
            var click = dgvTacGia.PointToClient(Cursor.Position);

            if (editR.Contains(click)) EditRow(e.RowIndex);
            else if (delR.Contains(click)) DeleteSingle(e.RowIndex);
        }

        private void EditRow(int rowIndex)
        {
            var id = GetRowId(dgvTacGia.Rows[rowIndex]);
            if (id == null) { MessageBox.Show("Không tìm thấy ID bản ghi.", "Lỗi"); return; }

            using (var f = new SuaTacGiaForm(id.Value))
            {
                if (f.ShowDialog(this) == DialogResult.OK)
                    ReloadTacGia(txtSearch.Text?.Trim());
            }
        }

        private void DeleteSingle(int rowIndex)
        {
            var id = GetRowId(dgvTacGia.Rows[rowIndex]);
            if (id == null) { MessageBox.Show("Không tìm thấy ID bản ghi.", "Lỗi"); return; }

            var confirm = MessageBox.Show("Bạn có chắc muốn xóa bản ghi này?",
                "Xóa tác giả", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (confirm != DialogResult.Yes) return;

            ExecuteDeleteByIds(new[] { id.Value });
            ReloadTacGia(txtSearch.Text?.Trim());
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
                if (dgvTacGia.Columns.Contains(_idColumnName))
                {
                    var val = row.Cells[_idColumnName].Value;
                    if (val != null && val != DBNull.Value)
                    {
                        if (val is int i2) return i2;
                        if (val is long l2) return checked((int)l2);
                        if (int.TryParse(val.ToString(), out var p2)) return p2;
                    }
                }
                if (dgvTacGia.Columns.Contains("ID"))
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
            using var cmd = new SqlCommand($"DELETE FROM {_tableName} WHERE {_idColumnName} IN ({string.Join(",", parms)})", conn);
            for (int i = 0; i < ids.Length; i++)
                cmd.Parameters.Add(new SqlParameter(parms[i], SqlDbType.Int) { Value = ids[i] });
            cmd.ExecuteNonQuery();
        }
    }
}
