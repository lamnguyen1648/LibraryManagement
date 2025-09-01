using System.Data;
using Microsoft.Data.SqlClient;

namespace LibraryManagement.Forms.DocGiaRelated.QuanLyDocGia
{
    public partial class QuanLyDocGiaForm : Form
    {
        private const string TableName = "DocGia";

        private string? _idColumn;     // DG_ID | ID
        private string? _nameColumn;   // TenDG | HoTen | Ten
        private string? _emailColumn;  // Mail | Email
        private string? _phoneColumn;  // SoDienThoai | SDT | Phone

        private readonly DataTable _dg = new();
        private readonly BindingSource _bs = new();

        public QuanLyDocGiaForm()
        {
            // Use the renamed designer initializer (avoids InitializeComponent() ambiguity)
            InitializeComponent_DocGia();

            Load += (_, __) => Reload(txtSearch.Text?.Trim());
            btnThem.Click    += (_, __) => OnAdd();
            btnXoaNhieu.Click += (_, __) => OnBulkDelete();
            btnTimKiem.Click += (_, __) => Reload(txtSearch.Text?.Trim());

            dgvDG.AutoGenerateColumns = false;
            dgvDG.CurrentCellDirtyStateChanged += (_, __) =>
            {
                if (dgvDG.IsCurrentCellDirty && dgvDG.CurrentCell is DataGridViewCheckBoxCell)
                    dgvDG.CommitEdit(DataGridViewDataErrorContexts.Commit);
            };
            dgvDG.CellValueChanged += (_, e) =>
            {
                if (e.RowIndex >= 0 && dgvDG.Columns[e.ColumnIndex].Name == "Select")
                    UpdateBulkDeleteButtonState();
            };
            dgvDG.DataBindingComplete += (_, __) => UpdateSttValues();
            dgvDG.Sorted              += (_, __) => UpdateSttValues();
            dgvDG.CellPainting        += DgvDG_CellPainting;
            dgvDG.CellMouseClick      += DgvDG_CellMouseClick;

            _bs.DataSource = _dg;
            dgvDG.DataSource = _bs;
        }

        // ===== Load & schema
        private void EnsureSchema(SqlConnection conn)
        {
            if (_idColumn != null && _nameColumn != null) return;

            conn.Open();
            using var probe = new SqlCommand($"SELECT TOP(1) * FROM {TableName}", conn);
            using var da = new SqlDataAdapter(probe);
            var dt = new DataTable();
            da.Fill(dt);

            _idColumn   = new[] { "DG_ID", "ID" }.FirstOrDefault(dt.Columns.Contains) ?? dt.Columns[0].ColumnName;
            _nameColumn = new[] { "TenDG", "HoTen", "Ten" }.FirstOrDefault(dt.Columns.Contains)
                          ?? dt.Columns.Cast<DataColumn>().FirstOrDefault(c => c.DataType == typeof(string))?.ColumnName;
            _emailColumn = new[] { "Mail", "Email" }.FirstOrDefault(dt.Columns.Contains);
            _phoneColumn = new[] { "SoDienThoai", "DienThoai", "SDT", "Phone" }.FirstOrDefault(dt.Columns.Contains);
        }

        private void Reload(string? search)
        {
            _dg.Clear();

            using var conn = Db.Create();
            using var cmd  = conn.CreateCommand();
            EnsureSchema(conn);

            if (!string.IsNullOrWhiteSpace(search) && _nameColumn != null)
            {
                cmd.CommandText = $"SELECT * FROM {TableName} WHERE {_nameColumn} LIKE N'%' + @q + N'%'";
                cmd.Parameters.Add(new SqlParameter("@q", SqlDbType.NVarChar, 255) { Value = search! });
            }
            else
            {
                cmd.CommandText = $"SELECT * FROM {TableName}";
            }

            using var da = new SqlDataAdapter(cmd);
            da.Fill(_dg);

            BuildGridColumns();   // fixed order & widths
            LocalizeHeaders();
            EnsureHiddenId();
            UpdateEmptyState();
            UpdateBulkDeleteButtonState();
            UpdateSttValues();
        }

        // ===== Grid build
        private void BuildGridColumns()
        {
            dgvDG.Columns.Clear();

            // [0] Checkbox
            dgvDG.Columns.Add(new DataGridViewCheckBoxColumn
            {
                Name = "Select", HeaderText = "Chọn",
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill, FillWeight = 8
            });

            // [1] STT
            dgvDG.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "STT", HeaderText = "STT", ReadOnly = true,
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill, FillWeight = 7
            });

            // [2] Hidden ID
            if (_idColumn != null && _dg.Columns.Contains(_idColumn))
            {
                dgvDG.Columns.Add(new DataGridViewTextBoxColumn
                {
                    Name = _idColumn, DataPropertyName = _idColumn,
                    Visible = false, ReadOnly = true
                });
            }

            // [3] Tên độc giả
            if (_nameColumn != null && _dg.Columns.Contains(_nameColumn))
                dgvDG.Columns.Add(new DataGridViewTextBoxColumn
                {
                    Name = _nameColumn, DataPropertyName = _nameColumn,
                    HeaderText = "Tên độc giả", ReadOnly = true,
                    AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill, FillWeight = 30
                });

            // [4] Email
            if (_emailColumn != null && _dg.Columns.Contains(_emailColumn))
                dgvDG.Columns.Add(new DataGridViewTextBoxColumn
                {
                    Name = _emailColumn, DataPropertyName = _emailColumn,
                    HeaderText = "Email", ReadOnly = true,
                    AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill, FillWeight = 22
                });

            // [5] Số điện thoại
            if (_phoneColumn != null && _dg.Columns.Contains(_phoneColumn))
                dgvDG.Columns.Add(new DataGridViewTextBoxColumn
                {
                    Name = _phoneColumn, DataPropertyName = _phoneColumn,
                    HeaderText = "Số điện thoại", ReadOnly = true,
                    AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill, FillWeight = 19
                });

            // Any remaining columns (except hidden/sensitive)
            foreach (DataColumn dc in _dg.Columns)
            {
                var n = dc.ColumnName;

                // Skip columns already handled
                if (_idColumn != null && n.Equals(_idColumn, StringComparison.OrdinalIgnoreCase)) continue;
                if (_nameColumn != null && n.Equals(_nameColumn, StringComparison.OrdinalIgnoreCase)) continue;
                if (_emailColumn != null && n.Equals(_emailColumn, StringComparison.OrdinalIgnoreCase)) continue;
                if (_phoneColumn != null && n.Equals(_phoneColumn, StringComparison.OrdinalIgnoreCase)) continue;

                // ✅ Explicitly remove Địa chỉ from the table
                if (n.Equals("DiaChi", StringComparison.OrdinalIgnoreCase) ||
                    n.Equals("Address", StringComparison.OrdinalIgnoreCase))
                    continue;

                dgvDG.Columns.Add(new DataGridViewTextBoxColumn
                {
                    Name = n, DataPropertyName = n,
                    HeaderText = ToVNHeader(n), ReadOnly = true,
                    AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill, FillWeight = 12
                });
            }

            // [last] Chức năng
            dgvDG.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Function", HeaderText = "Chức năng", ReadOnly = true,
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill, FillWeight = 12
            });
        }

        private void EnsureHiddenId()
        {
            if (_idColumn != null && dgvDG.Columns.Contains(_idColumn))
            {
                dgvDG.Columns[_idColumn].Visible = false;
                dgvDG.Columns[_idColumn].ReadOnly = true;
            }
        }

        private void LocalizeHeaders()
        {
            foreach (DataGridViewColumn c in dgvDG.Columns)
            {
                if (c.Name is "Select" or "STT" or "Function") continue;
                c.HeaderText = ToVNHeader(c.Name);
            }
        }

        private static string ToVNHeader(string name) => name.ToLowerInvariant() switch
        {
            "tendg" or "hoten" or "ten" => "Tên độc giả",
            "mail" or "email" => "Email",
            "sodienthoai" or "dienthoai" or "sdt" or "phone" => "Số điện thoại",
            "diachi" => "Địa chỉ", // still localized for any other use; it just won't be added to the grid
            "ngaysinh" => "Ngày sinh",
            _ => SplitPascal(name)
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

        private void UpdateSttValues()
        {
            var sttCol = dgvDG.Columns["STT"];
            if (sttCol == null) return;

            for (int i = 0; i < dgvDG.Rows.Count; i++)
                if (!dgvDG.Rows[i].IsNewRow)
                    dgvDG.Rows[i].Cells[sttCol.Index].Value = (i + 1).ToString();
        }

        private void UpdateEmptyState() => lblEmpty.Visible = _dg.Rows.Count == 0;

        private void UpdateBulkDeleteButtonState()
        {
            var selCol = dgvDG.Columns["Select"];
            if (selCol == null) { btnXoaNhieu.Enabled = false; return; }

            bool any = dgvDG.Rows.Cast<DataGridViewRow>()
                .Any(r => Convert.ToBoolean((bool)(r.Cells[selCol.Index].Value ?? false)));
            btnXoaNhieu.Enabled = any;
        }

        // ===== Toolbar actions
        private void OnAdd()
        {
            using var f = new ThemDocGiaForm();
            if (f.ShowDialog(this) == DialogResult.OK)
                Reload(txtSearch.Text?.Trim());
        }

        private void OnBulkDelete()
        {
            var selCol = dgvDG.Columns["Select"];
            if (selCol == null) return;

            var ids = dgvDG.Rows.Cast<DataGridViewRow>()
                .Where(r => Convert.ToBoolean((bool)(r.Cells[selCol.Index].Value ?? false)))
                .Select(GetRowId).Where(v => v.HasValue).Select(v => v!.Value).ToArray();

            if (ids.Length == 0) return;

            if (MessageBox.Show($"Xóa {ids.Length} độc giả đã chọn?", "Xóa nhiều",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;

            ExecuteDelete(ids);
            Reload(txtSearch.Text?.Trim());
        }

        // ===== Row buttons (Sửa/Xóa)
        private void DgvDG_CellPainting(object? sender, DataGridViewCellPaintingEventArgs e)
        {
            if (e.RowIndex < 0) return;
            if (dgvDG.Columns[e.ColumnIndex].Name != "Function") return;

            e.PaintBackground(e.ClipBounds, true);
            var cell = e.CellBounds;
            int pad = Math.Max(2, cell.Height / 10);
            int btnW = (cell.Width - (pad * 3)) / 2;
            int btnH = cell.Height - (pad * 2);

            var editR = new Rectangle(cell.X + pad, cell.Y + pad, btnW, btnH);
            var delR  = new Rectangle(editR.Right + pad, cell.Y + pad, btnW, btnH);

            ButtonRenderer.DrawButton(e.Graphics, editR, "Sửa", e.CellStyle.Font, false,
                System.Windows.Forms.VisualStyles.PushButtonState.Default);
            ButtonRenderer.DrawButton(e.Graphics, delR, "Xóa", e.CellStyle.Font, false,
                System.Windows.Forms.VisualStyles.PushButtonState.Default);

            e.Handled = true;
        }

        private void DgvDG_CellMouseClick(object? sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.RowIndex < 0) return;
            if (e.Button != MouseButtons.Left) return;
            if (dgvDG.Columns[e.ColumnIndex].Name != "Function") return;

            var cell = dgvDG.GetCellDisplayRectangle(e.ColumnIndex, e.RowIndex, false);
            int pad = Math.Max(2, cell.Height / 10);
            int w   = (cell.Width - (pad * 3)) / 2;

            var editR = new Rectangle(cell.X + pad, cell.Y + pad, w, cell.Height - (pad * 2));
            var delR  = new Rectangle(editR.Right + pad, cell.Y + pad, w, cell.Height - (pad * 2));
            var click = dgvDG.PointToClient(Cursor.Position);

            if (editR.Contains(click)) EditRow(e.RowIndex);
            else if (delR.Contains(click)) DeleteSingle(e.RowIndex);
        }

        private void EditRow(int rowIndex)
        {
            var id = GetRowId(dgvDG.Rows[rowIndex]);
            if (id == null) { MessageBox.Show("Không tìm thấy ID.", "Lỗi"); return; }

            using var f = new SuaDocGiaForm(id.Value);
            if (f.ShowDialog(this) == DialogResult.OK)
                Reload(txtSearch.Text?.Trim());
        }

        private void DeleteSingle(int rowIndex)
        {
            var id = GetRowId(dgvDG.Rows[rowIndex]);
            if (id == null) { MessageBox.Show("Không tìm thấy ID.", "Lỗi"); return; }

            if (MessageBox.Show("Bạn có chắc muốn xóa bản ghi này?", "Xóa độc giả",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;

            ExecuteDelete(new[] { id.Value });
            Reload(txtSearch.Text?.Trim());
        }

        private int? GetRowId(DataGridViewRow row)
        {
            try
            {
                if (_idColumn == null) return null;

                if (row.DataBoundItem is DataRowView drv)
                {
                    var obj = drv.Row[_idColumn];
                    if (obj == null || obj == DBNull.Value) return null;
                    if (obj is int i) return i;
                    if (obj is long l) return checked((int)l);
                    if (int.TryParse(obj.ToString(), out var parsed)) return parsed;
                }
                if (dgvDG.Columns.Contains(_idColumn))
                {
                    var v = row.Cells[_idColumn].Value;
                    if (v == null || v == DBNull.Value) return null;
                    if (v is int i2) return i2;
                    if (v is long l2) return checked((int)l2);
                    if (int.TryParse(v.ToString(), out var parsed2)) return parsed2;
                }
            }
            catch { }
            return null;
        }

        private void ExecuteDelete(int[] ids)
        {
            if (_idColumn == null) return;

            using var conn = Db.Create();
            conn.Open();
            var parms = ids.Select((_, i) => $"@p{i}").ToArray();
            var sql = $"DELETE FROM {TableName} WHERE {_idColumn} IN ({string.Join(",", parms)})";
            using var cmd = new SqlCommand(sql, conn);
            for (int i = 0; i < ids.Length; i++)
                cmd.Parameters.Add(new SqlParameter(parms[i], SqlDbType.Int) { Value = ids[i] });
            cmd.ExecuteNonQuery();
        }
    }
}
