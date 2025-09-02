using System;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Microsoft.Data.SqlClient;

namespace LibraryManagement.Forms.QuanLyPhieuPhat
{
    public partial class QuanLyPhieuPhatForm : Form
    {
        private const string TableName    = "PhieuPhat";
        private const string IdColumnName = "PP_ID";

        private readonly DataTable _pp = new();
        private readonly BindingSource _bs = new();

        public QuanLyPhieuPhatForm()
        {
            InitializeComponent();

            Load += (_, __) => ReloadData();

            // wire once
            btnThem.Click     -= BtnThem_Click;     btnThem.Click     += BtnThem_Click;
            btnXoaNhieu.Click -= BtnXoaNhieu_Click; btnXoaNhieu.Click += BtnXoaNhieu_Click;

            dgvPP.AutoGenerateColumns = false;
            dgvPP.CurrentCellDirtyStateChanged += (_, __) =>
            {
                if (dgvPP.IsCurrentCellDirty && dgvPP.CurrentCell is DataGridViewCheckBoxCell)
                    dgvPP.CommitEdit(DataGridViewDataErrorContexts.Commit);
            };
            dgvPP.CellValueChanged += (_, e) =>
            {
                if (e.RowIndex >= 0 && dgvPP.Columns[e.ColumnIndex].Name == "Select")
                    UpdateBulkDeleteButtonState();
            };
            dgvPP.DataBindingComplete += (_, __) => UpdateSttValues();
            dgvPP.Sorted              += (_, __) => UpdateSttValues();

            dgvPP.CellPainting   += DgvPP_CellPainting;   // draw Sửa/Xóa
            dgvPP.CellMouseClick += DgvPP_CellMouseClick; // handle click

            _bs.DataSource = _pp;
            dgvPP.DataSource = _bs;
        }

        private void BtnThem_Click(object? s, EventArgs e)
        {
            using var f = new ThemPhieuPhatForm();
            if (f.ShowDialog(this) == DialogResult.OK)
                ReloadData();
        }

        private void BtnXoaNhieu_Click(object? s, EventArgs e)
        {
            var selectCol = dgvPP.Columns["Select"];
            if (selectCol == null) return;

            var ids = dgvPP.Rows
                .Cast<DataGridViewRow>()
                .Where(r => Convert.ToBoolean(r.Cells[selectCol.Index].Value ?? false))
                .Select(GetRowId)
                .Where(v => v.HasValue)
                .Select(v => v!.Value)
                .ToArray();

            if (ids.Length == 0) return;

            var confirm = MessageBox.Show($"Xóa {ids.Length} phiếu phạt đã chọn?",
                "Xóa nhiều", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (confirm != DialogResult.Yes) return;

            ExecuteDeleteByIds(ids);
            ReloadData();
        }

        // ===== Data load =====
        private void ReloadData()
        {
            _pp.Clear();

            using var conn = Db.Create();
            using var cmd  = conn.CreateCommand();
            cmd.CommandText = $"select * from {TableName}";
            using (var da = new SqlDataAdapter(cmd)) da.Fill(_pp);

            // display columns
            ApplyNhanVienDisplay();
            ApplyPhieuMuonDisplay();

            BuildGridColumnsIfNeeded();
            EnsureHiddenIdColumns();
            LocalizeHeaders();
            ArrangeColumns();

            UpdateEmptyState();
            UpdateBulkDeleteButtonState();
            UpdateSttValues();
        }

        private void ApplyNhanVienDisplay()
        {
            if (!_pp.Columns.Contains("NV_ID")) return;
            if (!_pp.Columns.Contains("NhanVien"))
                _pp.Columns.Add(new DataColumn("NhanVien", typeof(string)));

            var map = BuildLookupForNhanVien();

            foreach (DataRow r in _pp.Rows)
            {
                var idObj = r["NV_ID"];
                string display = "";
                if (idObj != DBNull.Value && idObj != null)
                {
                    var id = Convert.ToInt32(idObj);
                    display = map.TryGetValue(id, out var name) ? name : id.ToString();
                }
                r["NhanVien"] = display;
            }
        }

        private System.Collections.Generic.Dictionary<int, string> BuildLookupForNhanVien()
        {
            var map = new System.Collections.Generic.Dictionary<int, string>();
            try
            {
                using var conn = Db.Create(); conn.Open();
                // choose a display column
                var probe = new DataTable();
                using (var da = new SqlDataAdapter("select top(0) * from NhanVien", conn)) da.Fill(probe);
                string displayCol =
                    new[] { "TenNV", "HoTen", "Ten", "HoVaTen" }.FirstOrDefault(probe.Columns.Contains)
                    ?? probe.Columns.Cast<DataColumn>().FirstOrDefault(c => c.DataType == typeof(string))?.ColumnName
                    ?? "NV_ID";

                using var da2 = new SqlDataAdapter($"select NV_ID, [{displayCol}] as TenHienThi from NhanVien", conn);
                var dt = new DataTable(); da2.Fill(dt);
                foreach (DataRow r in dt.Rows)
                {
                    int k = Convert.ToInt32(r["NV_ID"]);
                    string v = r["TenHienThi"]?.ToString() ?? k.ToString();
                    if (!map.ContainsKey(k)) map.Add(k, v);
                }
            }
            catch { }
            return map;
        }

        private void ApplyPhieuMuonDisplay()
        {
            if (!_pp.Columns.Contains("PM_ID")) return;
            if (!_pp.Columns.Contains("PhieuMuon"))
                _pp.Columns.Add(new DataColumn("PhieuMuon", typeof(string)));

            var map = BuildLookupForPhieuMuon();

            foreach (DataRow r in _pp.Rows)
            {
                var idObj = r["PM_ID"];
                string display = "";
                if (idObj != DBNull.Value && idObj != null)
                {
                    var id = Convert.ToInt32(idObj);
                    display = map.TryGetValue(id, out var name) ? name : $"PM {id}";
                }
                r["PhieuMuon"] = display;
            }
        }

        private System.Collections.Generic.Dictionary<int, string> BuildLookupForPhieuMuon()
        {
            var map = new System.Collections.Generic.Dictionary<int, string>();
            try
            {
                using var conn = Db.Create(); conn.Open();

                // detect DocGia name column
                var probeDG = new DataTable();
                using (var da0 = new SqlDataAdapter("select top(0) * from DocGia", conn)) da0.Fill(probeDG);
                string dgNameCol =
                    new[] { "TenDocGia", "HoTen", "Ten", "TenDG", "HoVaTen" }
                    .FirstOrDefault(probeDG.Columns.Contains)
                    ?? probeDG.Columns.Cast<DataColumn>().FirstOrDefault(c => c.DataType == typeof(string))?.ColumnName
                    ?? "DG_ID";

                // Join PM with DocGia to build nice display text
                string sql = $@"
select pm.PM_ID,
       'PM ' + cast(pm.PM_ID as nvarchar(20)) +
       case when dg.{dgNameCol} is not null then ' - ' + cast(dg.{dgNameCol} as nvarchar(255)) else '' end as TenHienThi
from PhieuMuon pm
left join DocGia dg on dg.DG_ID = pm.DG_ID";

                using var da = new SqlDataAdapter(sql, conn);
                var dt = new DataTable(); da.Fill(dt);
                foreach (DataRow r in dt.Rows)
                {
                    int k = Convert.ToInt32(r["PM_ID"]);
                    string v = r["TenHienThi"]?.ToString() ?? $"PM {k}";
                    if (!map.ContainsKey(k)) map.Add(k, v);
                }
            }
            catch { }
            return map;
        }

        // ===== Grid build & visuals =====
        private void BuildGridColumnsIfNeeded()
        {
            if (dgvPP.Columns.Count > 0) return;

            dgvPP.Columns.Add(new DataGridViewCheckBoxColumn
            {
                Name = "Select",
                HeaderText = "Chọn",
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
                FillWeight = 8
            });

            dgvPP.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "STT",
                HeaderText = "STT",
                ReadOnly = true,
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
                FillWeight = 7
            });

            foreach (DataColumn dc in _pp.Columns)
            {
                var name = dc.ColumnName;
                if (name.Equals(IdColumnName, StringComparison.OrdinalIgnoreCase)) continue;
                if (name.Equals("NV_ID", StringComparison.OrdinalIgnoreCase)) continue;
                if (name.Equals("PM_ID", StringComparison.OrdinalIgnoreCase)) continue;

                dgvPP.Columns.Add(new DataGridViewTextBoxColumn
                {
                    Name = name,
                    DataPropertyName = name,
                    HeaderText = ToVietnameseHeader(name),
                    ReadOnly = true,
                    AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
                    FillWeight = name is "PhieuMuon" or "NhanVien" ? 18 : 20
                });
            }

            dgvPP.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Function",
                HeaderText = "Chức năng",
                ReadOnly = true,
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
                FillWeight = 18
            });
        }

        private void EnsureHiddenIdColumns()
        {
            if (dgvPP.Columns.Contains(IdColumnName)) dgvPP.Columns[IdColumnName].Visible = false;
            if (dgvPP.Columns.Contains("NV_ID")) dgvPP.Columns["NV_ID"].Visible = false;
            if (dgvPP.Columns.Contains("PM_ID")) dgvPP.Columns["PM_ID"].Visible = false;
        }

        private void ArrangeColumns()
        {
            int idx = 2;
            void SetIndex(string name)
            {
                if (dgvPP.Columns.Contains(name))
                    dgvPP.Columns[name].DisplayIndex = idx++;
            }

            SetIndex("PhieuMuon");
            SetIndex("NhanVien");
            SetIndex("LyDo");
            SetIndex("SoTienPhat");

            if (dgvPP.Columns.Contains("Function"))
                dgvPP.Columns["Function"].DisplayIndex = dgvPP.Columns.Count - 1;
        }

        private void LocalizeHeaders()
        {
            foreach (DataGridViewColumn c in dgvPP.Columns)
            {
                if (c.Name is "Select" or "STT" or "Function") continue;
                c.HeaderText = ToVietnameseHeader(c.Name);
            }
        }

        private static string ToVietnameseHeader(string col) => col.ToLowerInvariant() switch
        {
            "pp_id"       => "Mã phiếu phạt",
            "nv_id"       => "Nhân viên",
            "pm_id"       => "Phiếu mượn",
            "nhanvien"    => "Nhân viên",
            "phieumuon"   => "Phiếu mượn",
            "lydo"        => "Lý do",
            "sotienphat"  => "Số tiền phạt",
            _             => SplitPascal(col)
        };

        private static string SplitPascal(string s)
        {
            var list = new System.Collections.Generic.List<char>(s.Length * 2);
            for (int i = 0; i < s.Length; i++)
            {
                char c = s[i];
                if (i > 0 && char.IsUpper(c) && (char.IsLower(s[i - 1]) || (i + 1 < s.Length && char.IsLower(s[i + 1]))))
                    list.Add(' ');
                list.Add(c);
            }
            return new string(list.ToArray());
        }

        private void UpdateEmptyState() => lblEmpty.Visible = _pp.Rows.Count == 0;

        private void UpdateSttValues()
        {
            var sttCol = dgvPP.Columns["STT"];
            if (sttCol == null) return;

            for (int i = 0; i < dgvPP.Rows.Count; i++)
            {
                var row = dgvPP.Rows[i];
                if (!row.IsNewRow) row.Cells[sttCol.Index].Value = (i + 1).ToString();
            }
        }

        private void UpdateBulkDeleteButtonState()
        {
            var selectCol = dgvPP.Columns["Select"];
            if (selectCol == null) { btnXoaNhieu.Enabled = false; return; }

            bool anyChecked = dgvPP.Rows
                .Cast<DataGridViewRow>()
                .Any(r => Convert.ToBoolean(r.Cells[selectCol.Index].Value ?? false));

            btnXoaNhieu.Enabled = anyChecked;
        }

        // ===== Function buttons: Sửa | Xóa =====
        private void DgvPP_CellPainting(object? sender, DataGridViewCellPaintingEventArgs e)
        {
            if (e.RowIndex < 0) return;
            if (dgvPP.Columns[e.ColumnIndex].Name != "Function") return;

            e.PaintBackground(e.ClipBounds, true);
            var cell = e.CellBounds;
            int pad = Math.Max(2, cell.Height / 10);
            int w = (cell.Width - (pad * 3)) / 2;
            int h = cell.Height - (pad * 2);

            var editR = new Rectangle(cell.X + pad, cell.Y + pad, w, h);
            var delR  = new Rectangle(editR.Right + pad, cell.Y + pad, w, h);

            ButtonRenderer.DrawButton(e.Graphics, editR, "Sửa", e.CellStyle.Font, false,
                System.Windows.Forms.VisualStyles.PushButtonState.Default);
            ButtonRenderer.DrawButton(e.Graphics, delR,  "Xóa", e.CellStyle.Font, false,
                System.Windows.Forms.VisualStyles.PushButtonState.Default);

            e.Handled = true;
        }

        private void DgvPP_CellMouseClick(object? sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.RowIndex < 0) return;
            if (e.Button != MouseButtons.Left) return;
            if (dgvPP.Columns[e.ColumnIndex].Name != "Function") return;

            var cell = dgvPP.GetCellDisplayRectangle(e.ColumnIndex, e.RowIndex, false);
            int pad = Math.Max(2, cell.Height / 10);
            int w = (cell.Width - (pad * 3)) / 2;

            var editR = new Rectangle(cell.X + pad, cell.Y + pad, w, cell.Height - (pad * 2));
            var delR  = new Rectangle(editR.Right + pad, cell.Y + pad, w, cell.Height - (pad * 2));
            var click = dgvPP.PointToClient(Cursor.Position);

            if (editR.Contains(click)) EditRow(e.RowIndex);
            else if (delR.Contains(click)) DeleteSingle(e.RowIndex);
        }

        private void EditRow(int rowIndex)
        {
            var id = GetRowId(dgvPP.Rows[rowIndex]);
            if (id == null) { MessageBox.Show("Không tìm thấy ID.", "Lỗi"); return; }

            using var f = new SuaPhieuPhatForm(id.Value);
            if (f.ShowDialog(this) == DialogResult.OK)
                ReloadData();
        }

        private void DeleteSingle(int rowIndex)
        {
            var id = GetRowId(dgvPP.Rows[rowIndex]);
            if (id == null) { MessageBox.Show("Không tìm thấy ID.", "Lỗi"); return; }

            var confirm = MessageBox.Show("Xóa phiếu phạt này?",
                "Xóa", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (confirm != DialogResult.Yes) return;

            ExecuteDeleteByIds(new[] { id.Value });
            ReloadData();
        }

        private int? GetRowId(DataGridViewRow row)
        {
            try
            {
                if (row.DataBoundItem is DataRowView drv)
                {
                    var obj = drv.Row[IdColumnName];
                    if (obj != DBNull.Value && obj != null)
                    {
                        if (obj is int i) return i;
                        if (obj is long l) return checked((int)l);
                        if (int.TryParse(obj.ToString(), out var p)) return p;
                    }
                }
                if (dgvPP.Columns.Contains(IdColumnName))
                {
                    var val = row.Cells[IdColumnName].Value;
                    if (val != null && val != DBNull.Value)
                    {
                        if (val is int i2) return i2;
                        if (val is long l2) return checked((int)l2);
                        if (int.TryParse(val.ToString(), out var p2)) return p2;
                    }
                }
            }
            catch { }
            return null;
        }

        private void ExecuteDeleteByIds(int[] ids)
        {
            using var conn = Db.Create(); conn.Open();
            var parms = ids.Select((_, i) => $"@p{i}").ToArray();
            var sql = $"delete from {TableName} where {IdColumnName} in ({string.Join(",", parms)})";
            using var cmd = new SqlCommand(sql, conn);
            for (int i = 0; i < ids.Length; i++)
                cmd.Parameters.Add(new SqlParameter(parms[i], SqlDbType.Int) { Value = ids[i] });
            cmd.ExecuteNonQuery();
        }
    }
}
