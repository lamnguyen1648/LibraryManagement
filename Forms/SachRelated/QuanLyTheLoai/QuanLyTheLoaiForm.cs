using System.Data;
using Microsoft.Data.SqlClient;

namespace LibraryManagement.Forms.SachRelated.QuanLyTheLoai
{
    public partial class QuanLyTheLoaiForm : Form
    {
        // Auto-resolved from DB
        private string? _tableName;      // "TheLoai"
        private string? _idColumnName;   // "TL_ID" or "ID"
        private string? _nameColumn;     // "TenTL" | "TenTheLoai" | "Ten"

        private readonly DataTable _tl = new DataTable();
        private readonly BindingSource _bs = new BindingSource();

        private static readonly string[] TableCandidates = { "TheLoai" };
        private static readonly string[] IdCandidates    = { "TL_ID", "ID" };
        private static readonly string[] NameCandidates  = { "TenTL", "TenTheLoai", "Ten" };

        public QuanLyTheLoaiForm()
        {
            InitializeComponent();

            Load              += (_, __) => ReloadTheLoai();
            btnThem.Click     -= BtnThem_Click;
            btnThem.Click     += BtnThem_Click;
            btnXoaNhieu.Click -= BtnXoaNhieu_Click;
            btnXoaNhieu.Click += BtnXoaNhieu_Click;
            btnTimKiem.Click  -= BtnTimKiem_Click;
            btnTimKiem.Click  += BtnTimKiem_Click;

            btnXoaNhieu.Enabled = false;

            dgvTheLoai.AutoGenerateColumns = false;
            dgvTheLoai.CurrentCellDirtyStateChanged += Dgv_CurrentCellDirtyStateChanged;
            dgvTheLoai.CellValueChanged             += Dgv_CellValueChanged;
            dgvTheLoai.DataBindingComplete          += (_, __) => UpdateSttValues();
            dgvTheLoai.Sorted                       += (_, __) => UpdateSttValues();

            dgvTheLoai.CellPainting   += Dgv_CellPainting;   // draw Sửa/Xóa
            dgvTheLoai.CellMouseClick += Dgv_CellMouseClick; // handle clicks

            _bs.DataSource = _tl;
            dgvTheLoai.DataSource = _bs;
        }

        // ===== Toolbar =====
        // in QuanLyTheLoaiForm.cs
        private void BtnThem_Click(object? s, EventArgs e)
        {
            using (var f = new ThemTheLoaiForm())
            {
                if (f.ShowDialog(this) == DialogResult.OK)
                    ReloadTheLoai(txtSearch.Text?.Trim());
            }
        }


        private void BtnTimKiem_Click(object? s, EventArgs e) => ReloadTheLoai(txtSearch.Text?.Trim());

        private void BtnXoaNhieu_Click(object? s, EventArgs e)
        {
            var selectCol = dgvTheLoai.Columns["Select"];
            if (selectCol == null) return;

            var ids = dgvTheLoai.Rows
                .Cast<DataGridViewRow>()
                .Where(r => Convert.ToBoolean(r.Cells[selectCol.Index].Value ?? false))
                .Select(GetRowId)
                .Where(v => v.HasValue)
                .Select(v => v!.Value)
                .ToArray();

            if (ids.Length == 0) return;

            var confirm = MessageBox.Show($"Xóa {ids.Length} thể loại đã chọn?",
                "Xóa nhiều thể loại", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (confirm != DialogResult.Yes) return;

            ExecuteDeleteByIds(ids);
            ReloadTheLoai(txtSearch.Text?.Trim());
        }

        // ===== Data =====
        private void ReloadTheLoai(string? search = null)
        {
            _tl.Clear();

            using var conn = Db.Create();
            using var cmd  = conn.CreateCommand();

            if (_tableName == null || _idColumnName == null || _nameColumn == null)
                ResolveTableAndColumns(conn);

            if (_tableName == null)
            {
                MessageBox.Show("Không tìm thấy bảng Thể loại.", "Lỗi");
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
            da.Fill(_tl);

            BuildGridColumnsIfNeeded(); // once
            EnsureHiddenIdColumn();     // always enforce
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
                    return;
                }
                catch
                {
                    // try next
                }
            }
        }

        // ===== Grid =====
        private void BuildGridColumnsIfNeeded()
        {
            if (dgvTheLoai.Columns.Count > 0) return;

            // [0] Checkbox
            var colSelect = new DataGridViewCheckBoxColumn
            {
                Name = "Select",
                HeaderText = "Chọn",
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
                FillWeight = 8
            };
            dgvTheLoai.Columns.Add(colSelect);

            // [1] STT
            var colStt = new DataGridViewTextBoxColumn
            {
                Name = "STT",
                HeaderText = "STT",
                ReadOnly = true,
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
                FillWeight = 7
            };
            dgvTheLoai.Columns.Add(colStt);

            // (Hidden ID inserted at index 2 by EnsureHiddenIdColumn)

            // Visible DB columns: name first (if present), then the rest (excluding ID & name)
            if (_nameColumn != null && _tl.Columns.Contains(_nameColumn))
            {
                var nameCol = new DataGridViewTextBoxColumn
                {
                    Name = _nameColumn,
                    DataPropertyName = _nameColumn,
                    HeaderText = "Tên thể loại",
                    ReadOnly = true,
                    AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
                    FillWeight = 46
                };
                dgvTheLoai.Columns.Add(nameCol);
            }

            foreach (DataColumn dc in _tl.Columns)
            {
                if (dc.ColumnName.Equals(_idColumnName, StringComparison.OrdinalIgnoreCase)) continue;
                if (_nameColumn != null && dc.ColumnName.Equals(_nameColumn, StringComparison.OrdinalIgnoreCase)) continue;

                var col = new DataGridViewTextBoxColumn
                {
                    Name = dc.ColumnName,
                    DataPropertyName = dc.ColumnName,
                    HeaderText = ToVietnameseHeader(dc.ColumnName),
                    ReadOnly = true,
                    AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
                    FillWeight = 18
                };
                dgvTheLoai.Columns.Add(col);
            }

            // [last] Function (Sửa/Xóa)
            var colFunc = new DataGridViewTextBoxColumn
            {
                Name = "Function",
                HeaderText = "Chức năng",
                ReadOnly = true,
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
                FillWeight = 12
            };
            dgvTheLoai.Columns.Add(colFunc);
        }

        private void EnsureHiddenIdColumn()
        {
            if (_idColumnName == null) return;

            if (!dgvTheLoai.Columns.Contains(_idColumnName))
            {
                var idCol = new DataGridViewTextBoxColumn
                {
                    Name = _idColumnName,
                    DataPropertyName = _idColumnName,
                    Visible = false,
                    ReadOnly = true
                };
                dgvTheLoai.Columns.Insert(2, idCol);
            }
            else
            {
                dgvTheLoai.Columns[_idColumnName].Visible = false;
                dgvTheLoai.Columns[_idColumnName].ReadOnly = true;
            }
        }

        private void UpdateEmptyState() => lblEmpty.Visible = _tl.Rows.Count == 0;

        private void UpdateSttValues()
        {
            var sttCol = dgvTheLoai.Columns["STT"];
            if (sttCol == null) return;

            for (int i = 0; i < dgvTheLoai.Rows.Count; i++)
            {
                var row = dgvTheLoai.Rows[i];
                if (!row.IsNewRow)
                    row.Cells[sttCol.Index].Value = (i + 1).ToString();
            }
        }

        private void Dgv_CurrentCellDirtyStateChanged(object? sender, EventArgs e)
        {
            if (dgvTheLoai.IsCurrentCellDirty && dgvTheLoai.CurrentCell is DataGridViewCheckBoxCell)
                dgvTheLoai.CommitEdit(DataGridViewDataErrorContexts.Commit);
        }

        private void Dgv_CellValueChanged(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0 && dgvTheLoai.Columns[e.ColumnIndex].Name == "Select")
                UpdateBulkDeleteButtonState();
        }

        private void UpdateBulkDeleteButtonState()
        {
            var selectCol = dgvTheLoai.Columns["Select"];
            if (selectCol == null) { btnXoaNhieu.Enabled = false; return; }

            bool anyChecked = dgvTheLoai.Rows
                .Cast<DataGridViewRow>()
                .Any(r => Convert.ToBoolean(r.Cells[selectCol.Index].Value ?? false));

            btnXoaNhieu.Enabled = anyChecked;
        }

        // ===== Sửa/Xóa buttons =====
        private void Dgv_CellPainting(object? sender, DataGridViewCellPaintingEventArgs e)
        {
            if (e.RowIndex < 0) return;
            if (dgvTheLoai.Columns[e.ColumnIndex].Name != "Function") return;

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
            if (dgvTheLoai.Columns[e.ColumnIndex].Name != "Function") return;

            var cell  = dgvTheLoai.GetCellDisplayRectangle(e.ColumnIndex, e.RowIndex, false);
            int pad   = Math.Max(2, cell.Height / 10);
            int w     = (cell.Width - (pad * 3)) / 2;
            var editR = new Rectangle(cell.X + pad, cell.Y + pad, w, cell.Height - (pad * 2));
            var delR  = new Rectangle(editR.Right + pad, cell.Y + pad, w, cell.Height - (pad * 2));
            var click = dgvTheLoai.PointToClient(Cursor.Position);

            if (editR.Contains(click)) EditRow(e.RowIndex);
            else if (delR.Contains(click)) DeleteSingle(e.RowIndex);
        }

        private void EditRow(int rowIndex)
        {
            var row = dgvTheLoai.Rows[rowIndex];
            var id  = GetRowId(row);
            if (id == null) { MessageBox.Show("Không tìm thấy ID bản ghi.", "Lỗi"); return; }

            using (var f = new SuaTheLoaiForm(id.Value))
            {
                if (f.ShowDialog(this) == DialogResult.OK)
                    ReloadTheLoai(txtSearch.Text?.Trim());
            }
        }

        private void DeleteSingle(int rowIndex)
        {
            var row = dgvTheLoai.Rows[rowIndex];
            var id  = GetRowId(row);
            if (id == null) { MessageBox.Show("Không tìm thấy ID bản ghi.", "Lỗi"); return; }

            var confirm = MessageBox.Show("Bạn có chắc muốn xóa bản ghi này?",
                "Xóa thể loại", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (confirm != DialogResult.Yes) return;

            ExecuteDeleteByIds(new[] { id.Value });
            ReloadTheLoai(txtSearch.Text?.Trim());
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
                if (dgvTheLoai.Columns.Contains(_idColumnName))
                {
                    var val = row.Cells[_idColumnName].Value;
                    if (val != null && val != DBNull.Value)
                    {
                        if (val is int i2) return i2;
                        if (val is long l2) return checked((int)l2);
                        if (int.TryParse(val.ToString(), out var parsed2)) return parsed2;
                    }
                }
                if (dgvTheLoai.Columns.Contains("ID"))
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

        // ----- Header localization -----
        private static string ToVietnameseHeader(string col) => col.ToLowerInvariant() switch
        {
            "tentl" or "tentheloai" or "ten" => "Tên thể loại",
            "mota"       => "Mô tả",
            "ghichu"     => "Ghi chú",
            "trangthai"  => "Trạng thái",
            "ngaytao"    => "Ngày tạo",
            "ngaycapnhat"=> "Cập nhật",
            _            => SplitPascal(col)
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
