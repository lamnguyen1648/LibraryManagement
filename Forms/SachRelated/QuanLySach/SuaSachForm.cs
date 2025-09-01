using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using LibraryManagement.Forms.Operations;
using Microsoft.Data.SqlClient;

namespace LibraryManagement.Forms.SachRelated.QuanLySach
{
    public partial class SuaSachForm : Form
    {
        private const string TableName = "Sach";
        private const string IdColumnName = "Sach_ID"; // adjust if your PK differs

        // Only TenSach is required (UI-level)
        private static readonly HashSet<string> RequiredOnly = new(StringComparer.OrdinalIgnoreCase) { "TenSach" };

        // Exclude from editable UI
        private static readonly HashSet<string> ExcludedColumns =
            new(StringComparer.OrdinalIgnoreCase) { "Sach_ID", "ID", "QR_Code" };

        private sealed class ColumnMeta
        {
            public string Name = "";
            public string DataType = "";
            public int MaxLength;
            public bool IsNullable;
            public int Ordinal;
        }

        private readonly int _sachId;
        private readonly List<ColumnMeta> _cols = new();
        private readonly Dictionary<string, Control> _controls = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, ComboBox> _lookupCombos = new(StringComparer.OrdinalIgnoreCase)
        {
            ["NXB_ID"] = null!,
            ["TG_ID"]  = null!,
            ["TL_ID"]  = null!
        };

        private DataRow? _rowBefore;

        public SuaSachForm(int sachId)
        {
            _sachId = sachId;

            InitializeComponent();

            StartPosition = FormStartPosition.CenterParent;
            AcceptButton  = btnSave;
            CancelButton  = btnCancel;

            Load += (_, __) =>
            {
                try
                {
                    LoadSchema();
                    LoadExistingRow();
                    BuildDynamicFields();
                    LoadLookups();
                    BindRowToControls();
                    WireValidation();
                    UpdateSaveEnabled();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Không thể tải biểu mẫu: " + ex.Message, "Lỗi",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    DialogResult = DialogResult.Cancel;
                    Close();
                }
            };

            btnSave.Click   += (_, __) => DoUpdate();
            btnCancel.Click += (_, __) => DialogResult = DialogResult.Cancel;
        }

        // ===== Schema =====
        private void LoadSchema()
        {
            using var conn = Db.Create();
            using var cmd  = conn.CreateCommand();
            cmd.CommandText = @"
SELECT  c.name, t.name, c.max_length, c.is_nullable, c.column_id
FROM sys.columns c
JOIN sys.types   t ON c.user_type_id = t.user_type_id
WHERE c.object_id = OBJECT_ID(N'dbo." + TableName + @"')
ORDER BY c.column_id;";
            conn.Open();
            using var rd = cmd.ExecuteReader();

            _cols.Clear();
            while (rd.Read())
            {
                var m = new ColumnMeta
                {
                    Name       = rd.GetString(0),
                    DataType   = rd.GetString(1),
                    MaxLength  = rd.GetInt16(2),
                    IsNullable = rd.GetBoolean(3),
                    Ordinal    = rd.GetInt32(4)
                };
                if (ExcludedColumns.Contains(m.Name)) continue;
                _cols.Add(m);
            }
        }

        // ===== Load existing row =====
        private void LoadExistingRow()
        {
            using var conn = Db.Create();
            using var da = new SqlDataAdapter($"SELECT TOP(1) * FROM dbo.{TableName} WHERE {IdColumnName}=@id", conn);
            da.SelectCommand.Parameters.Add(new SqlParameter("@id", SqlDbType.Int) { Value = _sachId });
            var dt = new DataTable();
            da.Fill(dt);
            if (dt.Rows.Count == 0)
                throw new Exception("Không tìm thấy sách.");

            _rowBefore = dt.Rows[0];
        }

        // ===== UI build =====
        private void BuildDynamicFields()
        {
            grid.Controls.Clear();
            grid.RowStyles.Clear();
            grid.RowCount = 0;
            _controls.Clear();

            foreach (var col in _cols)
            {
                grid.RowCount += 1;
                grid.RowStyles.Add(new RowStyle(SizeType.Percent, 1f));

                bool isLong   = IsLongText(col);
                bool isDate   = IsDateType(col.DataType);
                bool isBool   = IsBoolType(col.DataType);
                bool isLookup = IsLookup(col.Name);

                // label + star (star only for TenSach)
                var pnl = new FlowLayoutPanel
                {
                    Dock = DockStyle.Top,
                    WrapContents = false,
                    AutoSize = false,
                    Height = 28,
                    Padding = new Padding(6, 0, 0, 0),
                    Margin = new Padding(0)
                };
                var lbl = new Label { AutoSize = true, Text = ToVN(col.Name) };
                var star = new Label
                {
                    AutoSize = true,
                    Text = RequiredOnly.Contains(col.Name) ? "*" : "",
                    ForeColor = Color.Firebrick,
                    Font = new Font("Segoe UI", 10F, FontStyle.Bold)
                };
                int top = ComputeTopAlign(lbl, 28);
                lbl.Margin  = new Padding(0, top, 2, 0);
                star.Margin = new Padding(0, top, 0, 0);
                pnl.Controls.Add(lbl);
                pnl.Controls.Add(star);
                grid.Controls.Add(pnl, 0, grid.RowCount - 1);

                // input control
                Control input;
                if (isLookup)
                {
                    var cb = new ComboBox
                    {
                        Dock = DockStyle.Top,
                        DropDownStyle = ComboBoxStyle.DropDownList,
                        Margin = new Padding(0, 0, 0, 12)
                    };
                    _lookupCombos[col.Name] = cb;
                    input = cb;
                }
                else if (isDate)
                {
                    input = new DateTimePicker
                    {
                        Dock = DockStyle.Top,
                        Format = DateTimePickerFormat.Short,
                        Margin = new Padding(0, 0, 0, 12)
                    };
                }
                else if (isBool)
                {
                    input = new CheckBox
                    {
                        Text = "Bật",
                        AutoSize = true,
                        Dock = DockStyle.Top,
                        Margin = new Padding(0, 4, 0, 12)
                    };
                }
                else
                {
                    var tb = new TextBox
                    {
                        AutoSize = false,
                        Dock = DockStyle.Top,
                        Height = isLong ? 96 : 28,
                        Multiline = isLong,
                        ScrollBars = isLong ? ScrollBars.Vertical : ScrollBars.None,
                        Margin = new Padding(0, 0, 0, 12)
                    };
                    input = tb;
                }

                _controls[col.Name] = input;
                grid.Controls.Add(input, 1, grid.RowCount - 1);
            }
        }

        private static bool IsLookup(string name) =>
            name.Equals("NXB_ID", StringComparison.OrdinalIgnoreCase) ||
            name.Equals("TG_ID",  StringComparison.OrdinalIgnoreCase) ||
            name.Equals("TL_ID",  StringComparison.OrdinalIgnoreCase);

        private static bool IsLongText(ColumnMeta c)
        {
            var t = c.DataType.ToLowerInvariant();
            if (t is "text" or "ntext") return true;
            if (t is "nvarchar" or "varchar") return c.MaxLength < 0 || c.MaxLength >= 400;
            return false;
        }
        private static bool IsDateType(string dt) =>
            dt.Equals("date", StringComparison.OrdinalIgnoreCase) ||
            dt.Equals("datetime", StringComparison.OrdinalIgnoreCase) ||
            dt.Equals("datetime2", StringComparison.OrdinalIgnoreCase) ||
            dt.Equals("smalldatetime", StringComparison.OrdinalIgnoreCase);
        private static bool IsBoolType(string dt) => dt.Equals("bit", StringComparison.OrdinalIgnoreCase);
        private static int ComputeTopAlign(Label lbl, int targetHeight)
        {
            var h = TextRenderer.MeasureText("Ag", lbl.Font).Height;
            return Math.Max(0, (targetHeight - h) / 2 - 1);
        }

        // ===== Lookups + bind =====
        private void LoadLookups()
        {
            using var conn = Db.Create();
            conn.Open();

            if (_lookupCombos.TryGetValue("NXB_ID", out var cboNXB) && cboNXB != null)
            {
                var dt = new DataTable();
                using var da = new SqlDataAdapter("SELECT NXB_ID, TenNXB FROM dbo.NhaXuatBan ORDER BY TenNXB", conn);
                da.Fill(dt);
                cboNXB.DisplayMember = "TenNXB";
                cboNXB.ValueMember   = "NXB_ID";
                cboNXB.DataSource    = dt;
            }
            if (_lookupCombos.TryGetValue("TG_ID", out var cboTG) && cboTG != null)
            {
                var dt = new DataTable();
                using var da = new SqlDataAdapter("SELECT TG_ID, TenTG FROM dbo.TacGia ORDER BY TenTG", conn);
                da.Fill(dt);
                cboTG.DisplayMember = "TenTG";
                cboTG.ValueMember   = "TG_ID";
                cboTG.DataSource    = dt;
            }
            if (_lookupCombos.TryGetValue("TL_ID", out var cboTL) && cboTL != null)
            {
                var dt = new DataTable();
                using var da = new SqlDataAdapter("SELECT TL_ID, TenTheLoai FROM dbo.TheLoai ORDER BY TenTheLoai", conn);
                da.Fill(dt);
                cboTL.DisplayMember = "TenTheLoai";
                cboTL.ValueMember   = "TL_ID";
                cboTL.DataSource    = dt;
            }
        }

        private void BindRowToControls()
        {
            if (_rowBefore == null) return;

            foreach (var col in _cols)
            {
                if (!_controls.TryGetValue(col.Name, out var ctl)) continue;

                object v = _rowBefore[col.Name];

                switch (ctl)
                {
                    case ComboBox cb:
                        try { cb.SelectedValue = v == DBNull.Value ? null : v; } catch { /* ignore */ }
                        break;
                    case DateTimePicker dtp:
                        dtp.Value = v == DBNull.Value ? DateTime.Today : Convert.ToDateTime(v);
                        break;
                    case CheckBox chk:
                        chk.Checked = v != DBNull.Value && Convert.ToBoolean(v);
                        break;
                    case TextBox tb:
                        tb.Text = v == DBNull.Value ? "" : v.ToString();
                        break;
                }
            }
        }

        // ===== Validation =====
        private void WireValidation()
        {
            foreach (var (_, ctl) in _controls)
            {
                switch (ctl)
                {
                    case TextBox tb:
                        tb.TextChanged += (_, __) => UpdateSaveEnabled();
                        break;
                    case ComboBox cb:
                        cb.SelectedIndexChanged += (_, __) => UpdateSaveEnabled();
                        break;
                    case DateTimePicker dtp:
                        dtp.ValueChanged += (_, __) => UpdateSaveEnabled();
                        break;
                }
            }
        }

        private void UpdateSaveEnabled()
        {
            // Only TenSach is required
            if (!_controls.TryGetValue("TenSach", out var ctl) || ctl is not TextBox tb)
            {
                btnSave.Enabled = true; // if TenSach field is missing for any reason
                return;
            }

            btnSave.Enabled = !string.IsNullOrWhiteSpace(tb.Text);
        }

        // ===== Update + (optional) history =====
        private void DoUpdate()
        {
            try
            {
                // Enforce only TenSach
                if (!_controls.TryGetValue("TenSach", out var tenCtl) ||
                    tenCtl is not TextBox tenTb ||
                    string.IsNullOrWhiteSpace(tenTb.Text))
                {
                    MessageBox.Show("Vui lòng nhập: Tên sách", "Thiếu dữ liệu");
                    return;
                }

                using var conn = Db.Create();
                conn.Open();

                // Re-read BEFORE snapshot to diff latest
                var before = new DataTable();
                using (var da = new SqlDataAdapter($"SELECT TOP(1) * FROM dbo.{TableName} WHERE {IdColumnName}=@id", conn))
                {
                    da.SelectCommand.Parameters.Add(new SqlParameter("@id", SqlDbType.Int) { Value = _sachId });
                    da.Fill(before);
                }
                if (before.Rows.Count == 0)
                {
                    MessageBox.Show("Không tìm thấy sách.", "Lỗi");
                    return;
                }

                var sets = new List<string>();
                var prms = new List<SqlParameter>();

                foreach (var col in _cols)
                {
                    if (!_controls.TryGetValue(col.Name, out var ctl)) continue;

                    object? value = ctl switch
                    {
                        ComboBox cb      => cb.SelectedValue,
                        DateTimePicker d => d.Value,
                        CheckBox chk     => chk.Checked,
                        TextBox tb       => (object?)tb.Text?.Trim(),
                        _                => null
                    };

                    // Only TenSach is required; others become NULL when empty
                    if (col.Name.Equals("TenSach", StringComparison.OrdinalIgnoreCase))
                    {
                        if (value is string s0 && string.IsNullOrWhiteSpace(s0))
                        {
                            MessageBox.Show("Vui lòng nhập: Tên sách", "Thiếu dữ liệu");
                            return;
                        }
                    }
                    else
                    {
                        if (value is string s && string.IsNullOrWhiteSpace(s))
                            value = DBNull.Value;
                    }

                    sets.Add($"{col.Name}=@{col.Name}");
                    prms.Add(new SqlParameter("@" + col.Name, MapSqlType(col)) { Value = value ?? DBNull.Value });
                }

                if (sets.Count == 0)
                {
                    MessageBox.Show("Không có thay đổi để lưu.", "Thông báo");
                    return;
                }

                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = $"UPDATE dbo.{TableName} SET {string.Join(",", sets)} WHERE {IdColumnName}=@id;";
                    cmd.Parameters.AddRange(prms.ToArray());
                    cmd.Parameters.Add(new SqlParameter("@id", SqlDbType.Int) { Value = _sachId });
                    cmd.ExecuteNonQuery();
                }

                // AFTER for history
                var after = new DataTable();
                using (var da = new SqlDataAdapter($"SELECT TOP(1) * FROM dbo.{TableName} WHERE {IdColumnName}=@id", conn))
                {
                    da.SelectCommand.Parameters.Add(new SqlParameter("@id", SqlDbType.Int) { Value = _sachId });
                    da.Fill(after);
                }

                // History logging (kept as-is unless you want it disabled here too)
                string detail = BuildDiff(before.Rows[0], after.Rows[0]);
                if (string.IsNullOrWhiteSpace(detail))
                    detail = $"Cập nhật sách ID={_sachId}";
                InsertHistory(conn, _sachId, "Update", detail);

                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Không thể lưu cập nhật: " + ex.Message, "Lỗi",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private static SqlDbType MapSqlType(ColumnMeta c)
        {
            var t = c.DataType.ToLowerInvariant();
            return t switch
            {
                "int" => SqlDbType.Int,
                "bigint" => SqlDbType.BigInt,
                "smallint" => SqlDbType.SmallInt,
                "tinyint" => SqlDbType.TinyInt,
                "bit" => SqlDbType.Bit,
                "decimal" or "numeric" => SqlDbType.Decimal,
                "money" => SqlDbType.Money,
                "smallmoney" => SqlDbType.SmallMoney,
                "float" => SqlDbType.Float,
                "real" => SqlDbType.Real,
                "date" => SqlDbType.Date,
                "datetime" => SqlDbType.DateTime,
                "datetime2" => SqlDbType.DateTime2,
                "smalldatetime" => SqlDbType.SmallDateTime,
                "time" => SqlDbType.Time,
                "char" => SqlDbType.Char,
                "nchar" => SqlDbType.NChar,
                "varchar" => SqlDbType.VarChar,
                "nvarchar" => SqlDbType.NVarChar,
                "text" => SqlDbType.Text,
                "ntext" => SqlDbType.NText,
                "uniqueidentifier" => SqlDbType.UniqueIdentifier,
                "binary" => SqlDbType.Binary,
                "varbinary" => SqlDbType.VarBinary,
                _ => SqlDbType.NVarChar
            };
        }

        // ===== Helpers =====
        private static string ToVN(string col) => col.ToLowerInvariant() switch
        {
            "tensach"       => "Tên sách",
            "namxuatban"    => "Năm xuất bản",
            "nxb_id"        => "Nhà Xuất Bản",
            "tg_id"         => "Tác Giả",
            "tl_id"         => "Thể Loại",
            "tinhtrangsach" => "Tình trạng sách",
            _               => SplitPascal(col)
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

        private static int CurrentUserId()
        {
            try { return (int)UserSession.NV_ID!; } catch { return 0; }
        }

        private static void InsertHistory(SqlConnection conn, int sachId, string action, string detail)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"INSERT INTO dbo.LichSuCapNhatSach (NV_ID, SACH_ID, HinhThucCapNhat, ChiTietCapNhat)
VALUES (@nv, @sid, @act, @detail);";
            cmd.Parameters.Add(new SqlParameter("@nv", SqlDbType.Int) { Value = CurrentUserId() });
            cmd.Parameters.Add(new SqlParameter("@sid", SqlDbType.Int) { Value = sachId });
            cmd.Parameters.Add(new SqlParameter("@act", SqlDbType.NVarChar, 20) { Value = action });
            cmd.Parameters.Add(new SqlParameter("@detail", SqlDbType.NVarChar, -1) { Value = detail ?? "" });
            cmd.ExecuteNonQuery();
        }

        private static string BuildDiff(DataRow before, DataRow after)
        {
            static string VN(string col) => col.ToLowerInvariant() switch
            {
                "tensach"       => "Tên sách",
                "namxuatban"    => "Năm xuất bản",
                "nxb_id"        => "Nhà Xuất Bản",
                "tg_id"         => "Tác Giả",
                "tl_id"         => "Thể Loại",
                "tinhtrangsach" => "Tình trạng sách",
                _               => col
            };

            var parts = new List<string>();
            foreach (DataColumn c in before.Table.Columns)
            {
                if (string.Equals(c.ColumnName, IdColumnName, StringComparison.OrdinalIgnoreCase))
                    continue;

                object b = before[c.ColumnName];
                object a = after[c.ColumnName];
                string bs = b == DBNull.Value ? "NULL" : b.ToString();
                string as_ = a == DBNull.Value ? "NULL" : a.ToString();

                if (!string.Equals(bs, as_, StringComparison.Ordinal))
                    parts.Add($"{VN(c.ColumnName)}: {bs} -> {as_}");
            }
            return parts.Count == 0 ? "" : "Cập nhật: " + string.Join("; ", parts);
        }
    }
}
