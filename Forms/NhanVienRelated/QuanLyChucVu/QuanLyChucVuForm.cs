using System;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Microsoft.Data.SqlClient;

namespace LibraryManagement.Forms.NhanVienRelated.QuanLyChucVu
{
    public partial class QuanLyChucVuForm : Form
    {
        private const string TableName = "ChucVu";
        private const string IdFallback = "CV_ID";  // typical PK
        private const string NameFallback = "TenCV";

        private readonly DataTable _cv = new();
        private readonly BindingSource _bs = new();

        private string? _idColumn;   // CV_ID | ID
        private string? _nameColumn; // TenCV | TenChucVu | Ten
        private string? _descColumn; // MoTa | GhiChu | Description

        public QuanLyChucVuForm()
        {
            InitializeComponent();

            Load += (_, __) => Reload(txtSearch.Text?.Trim());

            btnThem.Click    += (_, __) => OnAdd();
            btnXoaNhieu.Click += (_, __) => OnBulkDelete();
            btnTimKiem.Click += (_, __) => Reload(txtSearch.Text?.Trim());

            dgvCV.AutoGenerateColumns = false;
            dgvCV.CurrentCellDirtyStateChanged += (_, __) =>
            {
                if (dgvCV.IsCurrentCellDirty && dgvCV.CurrentCell is DataGridViewCheckBoxCell)
                    dgvCV.CommitEdit(DataGridViewDataErrorContexts.Commit);
            };
            dgvCV.CellValueChanged += (_, e) =>
            {
                if (e.RowIndex >= 0 && dgvCV.Columns[e.ColumnIndex].Name == "Select")
                    UpdateBulkDeleteButtonState();
            };
            dgvCV.DataBindingComplete += (_, __) => UpdateSttValues();
            dgvCV.Sorted              += (_, __) => UpdateSttValues();
            dgvCV.CellPainting        += Dgv_CellPainting;   // Sửa/Xóa
            dgvCV.CellMouseClick      += Dgv_CellMouseClick;

            _bs.DataSource = _cv;
            dgvCV.DataSource = _bs;
        }

        // ===== Load & schema =====
        private void EnsureSchema(SqlConnection conn)
        {
            if (_idColumn != null && _nameColumn != null) return;

            conn.Open();
            using var probe = new SqlCommand($"SELECT TOP(1) * FROM {TableName}", conn);
            using var da = new SqlDataAdapter(probe);
            var dt = new DataTable();
            da.Fill(dt);

            _idColumn = new[] { "CV_ID", "ID" }.FirstOrDefault(dt.Columns.Contains) ?? dt.Columns[0].ColumnName;
            _nameColumn = new[] { "TenCV", "TenChucVu", "Ten" }.FirstOrDefault(dt.Columns.Contains)
                          ?? dt.Columns.Cast<DataColumn>().FirstOrDefault(c => c.DataType == typeof(string))?.ColumnName
                          ?? NameFallback;
            _descColumn = new[] { "MoTa", "GhiChu", "Description" }.FirstOrDefault(dt.Columns.Contains);
        }

        private void Reload(string? search)
        {
            _cv.Clear();

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
            da.Fill(_cv);

            BuildGridColumns();  // fixed order & widths
            LocalizeHeaders();
            EnsureHiddenId();
            UpdateEmptyState();
            UpdateBulkDeleteButtonState();
            UpdateSttValues();
        }

        // ===== Grid =====
        private void BuildGridColumns()
        {
            dgvCV.Columns.Clear();

            // [0] Checkbox
            dgvCV.Columns.Add(new DataGridViewCheckBoxColumn
            {
                Name = "Select",
                HeaderText = "Chọn",
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
                FillWeight = 8
            });

            // [1] STT
            dgvCV.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "STT",
                HeaderText = "STT",
                ReadOnly = true,
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
                FillWeight = 7
            });

            // [2] Hidden ID
            if (_idColumn != null && _cv.Columns.Contains(_idColumn))
            {
                dgvCV.Columns.Add(new DataGridViewTextBoxColumn
                {
                    Name = _idColumn,
                    DataPropertyName = _idColumn,
                    Visible = false,
                    ReadOnly = true
                });
            }

            // [3] Tên chức vụ
            if (_nameColumn != null && _cv.Columns.Contains(_nameColumn))
                dgvCV.Columns.Add(new DataGridViewTextBoxColumn
                {
                    Name = _nameColumn,
                    DataPropertyName = _nameColumn,
                    HeaderText = "Tên chức vụ",
                    ReadOnly = true,
                    AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
                    FillWeight = 38
                });

            // [4] Mô tả (optional)
            if (_descColumn != null && _cv.Columns.Contains(_descColumn))
                dgvCV.Columns.Add(new DataGridViewTextBoxColumn
                {
                    Name = _descColumn,
                    DataPropertyName = _descColumn,
                    HeaderText = "Mô tả",
                    ReadOnly = true,
                    AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
                    FillWeight = 35
                });

            // Any remaining columns except ID/Name/Desc
            foreach (DataColumn dc in _cv.Columns)
            {
                var n = dc.ColumnName;
                if (_idColumn != null && n.Equals(_idColumn, StringComparison.OrdinalIgnoreCase)) continue;
                if (_nameColumn != null && n.Equals(_nameColumn, StringComparison.OrdinalIgnoreCase)) continue;
                if (_descColumn != null && n.Equals(_descColumn, StringComparison.OrdinalIgnoreCase)) continue;

                dgvCV.Columns.Add(new DataGridViewTextBoxColumn
                {
                    Name = n,
                    DataPropertyName = n,
                    HeaderText = ToVNHeader(n),
                    ReadOnly = true,
                    AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
                    FillWeight = 12
                });
            }

            // [last] Chức năng
            dgvCV.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Function",
                HeaderText = "Chức năng",
                ReadOnly = true,
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
                FillWeight = 12
            });
        }

        private void EnsureHiddenId()
        {
            if (_idColumn != null && dgvCV.Columns.Contains(_idColumn))
            {
                dgvCV.Columns[_idColumn].Visible = false;
                dgvCV.Columns[_idColumn].ReadOnly = true;
            }
        }

        private void LocalizeHeaders()
        {
            foreach (DataGridViewColumn c in dgvCV.Columns)
            {
                if (c.Name is "Select" or "STT" or "Function") continue;
                c.HeaderText = ToVNHeader(c.Name);
            }
        }

        private static string ToVNHeader(string name) => name.ToLowerInvariant() switch
        {
            "tencv" or "tenchucvu" or "ten" => "Tên chức vụ",
            "mota" or "ghichu" or "description" => "Mô tả",
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
            var sttCol = dgvCV.Columns["STT"];
            if (sttCol == null) return;

            for (int i = 0; i < dgvCV.Rows.Count; i++)
            {
                var row = dgvCV.Rows[i];
                if (!row.IsNewRow)
                    row.Cells[sttCol.Index].Value = (i + 1).ToString();
            }
        }

        private void UpdateEmptyState() => lblEmpty.Visible = _cv.Rows.Count == 0;

        private void UpdateBulkDeleteButtonState()
        {
            var selCol = dgvCV.Columns["Select"];
            if (selCol == null) { btnXoaNhieu.Enabled = false; return; }

            bool any = dgvCV.Rows.Cast<DataGridViewRow>()
                .Any(r => Convert.ToBoolean(r.Cells[selCol.Index].Value ?? false));
            btnXoaNhieu.Enabled = any;
        }

        // ===== Toolbar actions =====
        private void OnAdd()
        {
            using var f = new ThemChucVuForm();
            if (f.ShowDialog(this) == DialogResult.OK)
                Reload(txtSearch.Text?.Trim());
        }

        private void OnBulkDelete()
        {
            var selCol = dgvCV.Columns["Select"];
            if (selCol == null) return;

            var ids = dgvCV.Rows.Cast<DataGridViewRow>()
                .Where(r => Convert.ToBoolean(r.Cells[selCol.Index].Value ?? false))
                .Select(GetRowId)
                .Where(v => v.HasValue)
                .Select(v => v!.Value)
                .ToArray();

            if (ids.Length == 0) return;

            if (MessageBox.Show($"Xóa {ids.Length} chức vụ đã chọn?", "Xóa nhiều",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;

            ExecuteDelete(ids);
            Reload(txtSearch.Text?.Trim());
        }

        // ===== Row buttons (Sửa/Xóa) =====
        private void Dgv_CellPainting(object? sender, DataGridViewCellPaintingEventArgs e)
        {
            if (e.RowIndex < 0) return;
            if (dgvCV.Columns[e.ColumnIndex].Name != "Function") return;

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

        private void Dgv_CellMouseClick(object? sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.RowIndex < 0) return;
            if (e.Button != MouseButtons.Left) return;
            if (dgvCV.Columns[e.ColumnIndex].Name != "Function") return;

            var cell = dgvCV.GetCellDisplayRectangle(e.ColumnIndex, e.RowIndex, false);
            int pad = Math.Max(2, cell.Height / 10);
            int w   = (cell.Width - (pad * 3)) / 2;

            var editR = new Rectangle(cell.X + pad, cell.Y + pad, w, cell.Height - (pad * 2));
            var delR  = new Rectangle(editR.Right + pad, cell.Y + pad, w, cell.Height - (pad * 2));

            var click = dgvCV.PointToClient(Cursor.Position);
            if (editR.Contains(click)) EditRow(e.RowIndex);
            else if (delR.Contains(click)) DeleteSingle(e.RowIndex);
        }

        private void EditRow(int rowIndex)
        {
            var id = GetRowId(dgvCV.Rows[rowIndex]);
            if (id == null) { MessageBox.Show("Không tìm thấy ID.", "Lỗi"); return; }

            using var f = new SuaChucVuForm(id.Value);
            if (f.ShowDialog(this) == DialogResult.OK)
                Reload(txtSearch.Text?.Trim());
        }

        private void DeleteSingle(int rowIndex)
        {
            var id = GetRowId(dgvCV.Rows[rowIndex]);
            if (id == null) { MessageBox.Show("Không tìm thấy ID.", "Lỗi"); return; }

            if (MessageBox.Show("Bạn có chắc muốn xóa bản ghi này?", "Xóa chức vụ",
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

                if (dgvCV.Columns.Contains(_idColumn))
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
