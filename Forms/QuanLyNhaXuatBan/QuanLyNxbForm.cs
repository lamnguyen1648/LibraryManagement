using System;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Microsoft.Data.SqlClient;

namespace LibraryManagement.Forms.QuanLyNhaXuatBan
{
    public partial class QuanLyNxbForm : Form
    {
        // Resolved at runtime to match your DB
        private string? _tableName;     // e.g., "NXB" or "NhaXuatBan"
        private string? _idColumnName;  // e.g., "NXB_ID" or "ID"
        private string? _nameColumn;    // e.g., "TenNXB" or "Ten" or "TenNhaXuatBan"

        private readonly DataTable _nxb = new DataTable();
        private readonly BindingSource _bs = new BindingSource();

        // Candidate names for auto-detection
        private static readonly string[] TableCandidates = { "NXB", "NhaXuatBan" };
        private static readonly string[] IdCandidates    = { "NXB_ID", "ID" };
        private static readonly string[] NameCandidates  = { "TenNXB", "Ten", "TenNhaXuatBan" };

        public QuanLyNxbForm()
        {
            InitializeComponent();

            Load              += (_, __) => ReloadNxb();
            btnThem.Click     -= BtnThem_Click;
            btnThem.Click     += BtnThem_Click;
            btnXoaNhieu.Click -= BtnXoaNhieu_Click;
            btnXoaNhieu.Click += BtnXoaNhieu_Click;
            btnTimKiem.Click  -= BtnTimKiem_Click;
            btnTimKiem.Click  += BtnTimKiem_Click;

            btnXoaNhieu.Enabled = false;

            dgvNXB.AutoGenerateColumns = false;
            dgvNXB.CurrentCellDirtyStateChanged += DgvNXB_CurrentCellDirtyStateChanged;
            dgvNXB.CellValueChanged             += DgvNXB_CellValueChanged;
            dgvNXB.DataBindingComplete          += (_, __) => UpdateSttValues();
            dgvNXB.Sorted                       += (_, __) => UpdateSttValues();

            // Inline Sửa/Xóa buttons
            dgvNXB.CellPainting   += DgvNXB_CellPainting;
            dgvNXB.CellMouseClick += DgvNXB_CellMouseClick;

            _bs.DataSource   = _nxb;
            dgvNXB.DataSource = _bs;
        }

        // ==== Toolbar handlers ====
        private void BtnThem_Click(object? s, EventArgs e)
        {
            // TODO: open ThemNXBForm when ready
            using (var f = new ThemNxbForm())
            {
                if (f.ShowDialog(this) == DialogResult.OK)
                    ReloadNxb(txtSearch.Text?.Trim());
            }
        }

        private void BtnTimKiem_Click(object? s, EventArgs e) => ReloadNxb(txtSearch.Text?.Trim());

        private void BtnXoaNhieu_Click(object? s, EventArgs e)
        {
            var selectCol = dgvNXB.Columns["Select"];
            if (selectCol == null) return;

            var ids = dgvNXB.Rows
                .Cast<DataGridViewRow>()
                .Where(r => Convert.ToBoolean(r.Cells[selectCol.Index].Value ?? false))
                .Select(GetRowId)
                .Where(v => v.HasValue)
                .Select(v => v!.Value)
                .ToArray();

            if (ids.Length == 0) return;

            var confirm = MessageBox.Show($"Xóa {ids.Length} Nhà Xuất Bản đã chọn?", "Xóa nhiều NXB",
                MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (confirm != DialogResult.Yes) return;

            ExecuteDeleteByIds(ids);
            ReloadNxb(txtSearch.Text?.Trim());
        }

        // ==== Data load ====
        private void ReloadNxb(string? search = null)
        {
            _nxb.Clear();

            using var conn = Db.Create();
            using var cmd  = conn.CreateCommand();

            if (_tableName == null || _idColumnName == null || _nameColumn == null)
                ResolveTableAndColumns(conn);

            if (_tableName == null)
            {
                MessageBox.Show("Không tìm thấy bảng NXB/Nhà Xuất Bản.", "Lỗi");
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
            da.Fill(_nxb);

            BuildGridColumnsIfNeeded();  // build once
            EnsureHiddenIdColumn();      // enforce hidden ID every load
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
                    var tmp = new DataTable();
                    da.Fill(tmp);
                    if (tmp.Columns.Count == 0) continue;

                    _tableName    = table;
                    _idColumnName = IdCandidates.FirstOrDefault(c => tmp.Columns.Contains(c)) ?? tmp.Columns[0].ColumnName;
                    _nameColumn   = NameCandidates.FirstOrDefault(c => tmp.Columns.Contains(c))
                                    ?? tmp.Columns.Cast<DataColumn>().FirstOrDefault(c => c.DataType == typeof(string))?.ColumnName;
                    return;
                }
                catch
                {
                    // try next table
                }
            }
        }

        // ==== Grid build ====
        private void BuildGridColumnsIfNeeded()
        {
            if (dgvNXB.Columns.Count > 0) return;

            // [0] Checkbox
            var colSelect = new DataGridViewCheckBoxColumn
            {
                Name = "Select",
                HeaderText = "Chọn",
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
                FillWeight = 8
            };
            dgvNXB.Columns.Add(colSelect);

            // [1] STT
            var colStt = new DataGridViewTextBoxColumn
            {
                Name = "STT",
                HeaderText = "STT",
                ReadOnly = true,
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
                FillWeight = 7
            };
            dgvNXB.Columns.Add(colStt);

            // (ID column is added in EnsureHiddenIdColumn so we can re-enforce each load)

            // [2] Tên NXB (bound to resolved name column)
            var colName = new DataGridViewTextBoxColumn
            {
                Name = _nameColumn ?? "TenNXB",
                DataPropertyName = _nameColumn ?? "TenNXB",
                HeaderText = "Tên Nhà Xuất Bản",
                ReadOnly = true,
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
                FillWeight = 73
            };
            dgvNXB.Columns.Add(colName);

            // [3] Function (Sửa/Xóa buttons)
            var colFunc = new DataGridViewTextBoxColumn
            {
                Name = "Function",
                HeaderText = "Chức năng",
                ReadOnly = true,
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
                FillWeight = 12
            };
            dgvNXB.Columns.Add(colFunc);
        }

        // Ensure the hidden, bound ID column exists & stays hidden
        private void EnsureHiddenIdColumn()
        {
            if (_idColumnName == null) return;

            if (!dgvNXB.Columns.Contains(_idColumnName))
            {
                var idCol = new DataGridViewTextBoxColumn
                {
                    Name = _idColumnName,
                    DataPropertyName = _idColumnName,
                    Visible = false,
                    ReadOnly = true
                };
                dgvNXB.Columns.Insert(2, idCol); // keep order: Select, STT, [ID hidden], Tên, Function
            }
            else
            {
                dgvNXB.Columns[_idColumnName].Visible = false;
                dgvNXB.Columns[_idColumnName].ReadOnly = true;
            }
        }

        private void UpdateEmptyState() => lblEmpty.Visible = _nxb.Rows.Count == 0;

        private void UpdateSttValues()
        {
            var sttCol = dgvNXB.Columns["STT"];
            if (sttCol == null) return;

            for (int i = 0; i < dgvNXB.Rows.Count; i++)
            {
                var row = dgvNXB.Rows[i];
                if (!row.IsNewRow)
                    row.Cells[sttCol.Index].Value = (i + 1).ToString();
            }
        }

        private void DgvNXB_CurrentCellDirtyStateChanged(object? sender, EventArgs e)
        {
            if (dgvNXB.IsCurrentCellDirty && dgvNXB.CurrentCell is DataGridViewCheckBoxCell)
                dgvNXB.CommitEdit(DataGridViewDataErrorContexts.Commit);
        }

        private void DgvNXB_CellValueChanged(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0 && dgvNXB.Columns[e.ColumnIndex].Name == "Select")
                UpdateBulkDeleteButtonState();
        }

        private void UpdateBulkDeleteButtonState()
        {
            var selectCol = dgvNXB.Columns["Select"];
            if (selectCol == null) { btnXoaNhieu.Enabled = false; return; }

            bool anyChecked = dgvNXB.Rows
                .Cast<DataGridViewRow>()
                .Any(r => Convert.ToBoolean(r.Cells[selectCol.Index].Value ?? false));

            btnXoaNhieu.Enabled = anyChecked;
        }

        // ==== Paint & click Function buttons ====
        private void DgvNXB_CellPainting(object? sender, DataGridViewCellPaintingEventArgs e)
        {
            if (e.RowIndex < 0) return;
            if (dgvNXB.Columns[e.ColumnIndex].Name != "Function") return;

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

        private void DgvNXB_CellMouseClick(object? sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.RowIndex < 0) return;
            if (e.Button != MouseButtons.Left) return;
            if (dgvNXB.Columns[e.ColumnIndex].Name != "Function") return;

            var cell   = dgvNXB.GetCellDisplayRectangle(e.ColumnIndex, e.RowIndex, false);
            int pad    = Math.Max(2, cell.Height / 10);
            int w      = (cell.Width - (pad * 3)) / 2;
            var editR  = new Rectangle(cell.X + pad, cell.Y + pad, w, cell.Height - (pad * 2));
            var delR   = new Rectangle(editR.Right + pad, cell.Y + pad, w, cell.Height - (pad * 2));
            var click  = dgvNXB.PointToClient(Cursor.Position);

            if (editR.Contains(click)) EditRow(e.RowIndex);
            else if (delR.Contains(click)) DeleteSingle(e.RowIndex);
        }

        private void EditRow(int rowIndex)
        {
            var row = dgvNXB.Rows[rowIndex];
            var id  = GetRowId(row);
            if (id == null)
            {
                MessageBox.Show("Không tìm thấy ID bản ghi.", "Lỗi");
                return;
            }

            using (var f = new SuaNxbForm(id.Value))
            {
                if (f.ShowDialog(this) == DialogResult.OK)
                    ReloadNxb(txtSearch.Text?.Trim());
            }
        }


        private void DeleteSingle(int rowIndex)
        {
            var row = dgvNXB.Rows[rowIndex];
            var id  = GetRowId(row);
            if (id == null)
            {
                MessageBox.Show("Không tìm thấy ID bản ghi.", "Lỗi");
                return;
            }

            var confirm = MessageBox.Show("Bạn có chắc muốn xóa bản ghi này?",
                "Xóa NXB", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (confirm != DialogResult.Yes) return;

            ExecuteDeleteByIds(new[] { id.Value });
            ReloadNxb(txtSearch.Text?.Trim());
        }

        // Prefer bound DataRowView -> fallback hidden cell -> fallback "ID"
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

                if (dgvNXB.Columns.Contains(_idColumnName))
                {
                    var val = row.Cells[_idColumnName].Value;
                    if (val != null && val != DBNull.Value)
                    {
                        if (val is int i2) return i2;
                        if (val is long l2) return checked((int)l2);
                        if (int.TryParse(val.ToString(), out var parsed2)) return parsed2;
                    }
                }

                if (dgvNXB.Columns.Contains("ID"))
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
    }
}
