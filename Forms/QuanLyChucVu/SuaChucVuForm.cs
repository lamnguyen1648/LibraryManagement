using System.Data;
using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;

namespace LibraryManagement.Forms.QuanLyChucVu
{
    public partial class SuaChucVuForm : Form
    {
        private const string TableName = "ChucVu";
        private const int CompactHeight = 28;

        private sealed class ColumnMeta
        {
            public string Name = "";
            public string DataType = "";
            public int MaxLength;
            public bool IsNullable;
            public bool IsIdentity;
            public bool IsComputed;
            public int Ordinal;
        }

        private readonly List<ColumnMeta> _cols = new();
        private readonly Dictionary<string, Control> _controls = new(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> _required = new(StringComparer.OrdinalIgnoreCase);

        private string? _idColumn;
        private DataRow? _row;
        private readonly int _id;

        public SuaChucVuForm(int id)
        {
            _id = id;
            InitializeComponent();

            StartPosition = FormStartPosition.CenterParent;
            AcceptButton  = btnSave;
            CancelButton  = btnCancel;

            Load += (_, __) =>
            {
                try
                {
                    DetectIdColumn();
                    LoadSchema();
                    LoadExisting();
                    BuildDynamicFields();
                    BindRow();
                    WireValidation();
                    UpdateSaveEnabled();
                    BeginInvoke((Action)ShrinkToFitContent);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Không thể tải biểu mẫu: " + ex.Message,
                        "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    DialogResult = DialogResult.Cancel;
                    Close();
                }
            };

            btnSave.Click   += (_, __) => DoUpdate();
            btnCancel.Click += (_, __) => DialogResult = DialogResult.Cancel;
        }

        private void DetectIdColumn()
        {
            using var conn = Db.Create();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
SELECT c.name, c.is_identity
FROM sys.columns c
WHERE c.object_id = OBJECT_ID(N'dbo." + TableName + @"')
ORDER BY c.column_id;";
            conn.Open();
            using var rd = cmd.ExecuteReader();

            string? fallback = null;
            while (rd.Read())
            {
                var name = rd.GetString(0);
                bool isId = rd.GetBoolean(1);
                if (isId) { _idColumn = name; break; }
                if (fallback == null && (name.Equals("CV_ID", StringComparison.OrdinalIgnoreCase) ||
                                         name.Equals("ID", StringComparison.OrdinalIgnoreCase)))
                    fallback = name;
            }
            _idColumn ??= fallback ?? "CV_ID";
        }

        private void LoadSchema()
        {
            using var conn = Db.Create();
            using var cmd  = conn.CreateCommand();
            cmd.CommandText = @"
SELECT  c.name, t.name, c.max_length, c.is_nullable, c.is_identity, c.column_id, c.is_computed
FROM sys.columns c
JOIN sys.types   t ON c.user_type_id = t.user_type_id
WHERE c.object_id = OBJECT_ID(N'dbo." + TableName + @"')
ORDER BY c.column_id;";
            conn.Open();
            using var rd = cmd.ExecuteReader();

            _cols.Clear(); _required.Clear();
            while (rd.Read())
            {
                var m = new ColumnMeta
                {
                    Name       = rd.GetString(0),
                    DataType   = rd.GetString(1),
                    MaxLength  = rd.GetInt16(2),
                    IsNullable = rd.GetBoolean(3),
                    IsIdentity = rd.GetBoolean(4),
                    Ordinal    = rd.GetInt32(5),
                    IsComputed = rd.GetBoolean(6)
                };
                if (m.IsIdentity || m.IsComputed) continue;
                _cols.Add(m);
                if (!m.IsNullable) _required.Add(m.Name);
            }

            if (_cols.Any(c => c.Name.Equals("TenCV", StringComparison.OrdinalIgnoreCase)))
                _required.Add("TenCV");
        }

        private void LoadExisting()
        {
            if (_idColumn == null) throw new InvalidOperationException("Chưa xác định cột ID.");

            using var conn = Db.Create();
            using var cmd  = conn.CreateCommand();
            cmd.CommandText = $"SELECT TOP 1 * FROM {TableName} WHERE {_idColumn} = @id";
            cmd.Parameters.Add(new SqlParameter("@id", SqlDbType.Int) { Value = _id });

            using var da = new SqlDataAdapter(cmd);
            var dt = new DataTable();
            da.Fill(dt);

            if (dt.Rows.Count == 0)
                throw new Exception($"Không tìm thấy bản ghi với {_idColumn} = {_id}.");
            _row = dt.Rows[0];
        }

        // ===== UI =====
        private void BuildDynamicFields()
        {
            grid.RowStyles.Clear();
            grid.Controls.Clear();
            grid.RowCount = 0;
            _controls.Clear();

            foreach (var col in _cols)
            {
                grid.RowCount += 1;
                grid.RowStyles.Add(new RowStyle(SizeType.Percent, 1f));

                bool isLongText = IsLongText(col);

                // Label + *
                var labelPanel = new FlowLayoutPanel
                {
                    Dock = DockStyle.Top,
                    Anchor = AnchorStyles.Left | AnchorStyles.Right,
                    WrapContents = false,
                    AutoSize = false,
                    Height = CompactHeight,
                    Padding = new Padding(6, 0, 0, 0),
                    Margin = new Padding(0)
                };
                var lbl = new Label { AutoSize = true, Text = ToVN(col.Name), TextAlign = ContentAlignment.MiddleLeft };
                var star = new Label
                {
                    AutoSize = true,
                    Text = _required.Contains(col.Name) ? "*" : "",
                    ForeColor = Color.Firebrick,
                    Font = new Font("Segoe UI", 10F, FontStyle.Bold)
                };
                int labelTop = ComputeLabelTopMargin(lbl, CompactHeight);
                lbl.Margin  = new Padding(0, labelTop, 2, 0);
                star.Margin = new Padding(0, labelTop, 0, 0);
                labelPanel.Controls.Add(lbl);
                labelPanel.Controls.Add(star);
                grid.Controls.Add(labelPanel, 0, grid.RowCount - 1);

                // Input
                var tb = new TextBox
                {
                    AutoSize = false,
                    Dock = DockStyle.Top,
                    Anchor = AnchorStyles.Left | AnchorStyles.Right,
                    Margin = new Padding(0, 0, 0, 12),
                    Height = CompactHeight,
                    Multiline = isLongText,
                    ScrollBars = isLongText ? ScrollBars.Vertical : ScrollBars.None
                };
                if (isLongText) tb.Height = 96;

                _controls[col.Name] = tb;
                grid.Controls.Add(tb, 1, grid.RowCount - 1);
            }
        }

        private static bool IsLongText(ColumnMeta c)
        {
            var t = c.DataType.ToLowerInvariant();
            if (t is "text" or "ntext") return true;
            if (t is "nvarchar" or "varchar") return c.MaxLength < 0 || c.MaxLength >= 400;
            return false;
        }

        private void BindRow()
        {
            if (_row == null) return;

            foreach (var col in _cols)
            {
                if (!_controls.TryGetValue(col.Name, out var c)) continue;
                if (c is TextBox tb)
                {
                    var val = _row[col.Name];
                    tb.Text = (val == DBNull.Value || val == null) ? "" : Convert.ToString(val);
                }
            }
        }

        private static int ComputeLabelTopMargin(Label lbl, int targetHeight)
        {
            var h = TextRenderer.MeasureText("Ag", lbl.Font).Height;
            int top = Math.Max(0, (targetHeight - h) / 2);
            return Math.Max(0, top - 1);
        }

        // ===== Validation =====
        private void WireValidation()
        {
            foreach (var (name, c) in _controls)
            {
                if (c is TextBox tb)
                    tb.TextChanged += (_, __) => { ValidateField(name); UpdateSaveEnabled(); };
            }
        }

        private void UpdateSaveEnabled()
        {
            bool ok = true;
            foreach (var k in _controls.Keys)
                if (!ValidateField(k)) { ok = false; break; }
            btnSave.Enabled = ok;
        }

        private bool ValidateField(string colName)
        {
            if (!_controls.TryGetValue(colName, out var c)) return true;
            bool required = _required.Contains(colName);

            if (c is TextBox tb)
            {
                var t = tb.Text?.Trim() ?? "";
                if (required && string.IsNullOrWhiteSpace(t))
                {
                    errorProvider1.SetError(tb, "Trường này là bắt buộc.");
                    return false;
                }
                if (TryRegex(colName, out var rx, out var msg) && t.Length > 0 && !rx.IsMatch(t))
                {
                    errorProvider1.SetError(tb, msg);
                    return false;
                }
                errorProvider1.SetError(tb, "");
                return true;
            }
            errorProvider1.SetError(c, "");
            return true;
        }

        private static bool TryRegex(string col, out Regex regex, out string msg)
        {
            string n = col.ToLowerInvariant();
            if (n is "tencv" or "tenchucvu" or "ten")
            {
                regex = new Regex(@"^[\p{L}\p{M}0-9 .,'&()/-]{2,100}$", RegexOptions.Compiled);
                msg = "Tên 2–100 ký tự; cho phép chữ, số và ,.'&()/-.";
                return true;
            }
            if (n is "mota" or "ghichu" or "description")
            {
                regex = new Regex(@"^.{0,500}$", RegexOptions.Compiled | RegexOptions.Singleline);
                msg = "Mô tả tối đa 500 ký tự.";
                return true;
            }
            regex = null!;
            msg = "";
            return false;
        }

        // ===== Update =====
        private void DoUpdate()
        {
            try
            {
                if (_idColumn == null) throw new InvalidOperationException("Chưa xác định cột ID.");

                var sets = new System.Collections.Generic.List<string>();
                var prms = new System.Collections.Generic.List<SqlParameter>();

                foreach (var col in _cols)
                {
                    if (!_controls.TryGetValue(col.Name, out var c)) continue;

                    object? value = (c as TextBox)?.Text?.Trim();
                    if (string.IsNullOrWhiteSpace(Convert.ToString(value)) && !_required.Contains(col.Name))
                        value = DBNull.Value;

                    sets.Add($"{col.Name} = @{col.Name}");
                    prms.Add(new SqlParameter("@" + col.Name, MapType(col)) { Value = value ?? DBNull.Value });
                }

                if (sets.Count == 0)
                {
                    MessageBox.Show("Không có thay đổi để lưu.", "Thông báo");
                    return;
                }

                var sql = $"UPDATE {TableName} SET {string.Join(", ", sets)} WHERE {_idColumn} = @id";
                using var conn = Db.Create();
                using var cmd  = new SqlCommand(sql, conn);
                cmd.Parameters.AddRange(prms.ToArray());
                cmd.Parameters.Add(new SqlParameter("@id", SqlDbType.Int) { Value = _id });

                conn.Open();
                cmd.ExecuteNonQuery();

                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Không thể lưu thay đổi: " + ex.Message,
                    "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private static SqlDbType MapType(ColumnMeta c)
        {
            var t = c.DataType.ToLowerInvariant() switch
            {
                "nvarchar" => SqlDbType.NVarChar,
                "varchar" => SqlDbType.VarChar,
                "text" => SqlDbType.Text,
                "ntext" => SqlDbType.NText,
                "int" => SqlDbType.Int,
                "bigint" => SqlDbType.BigInt,
                "smallint" => SqlDbType.SmallInt,
                "tinyint" => SqlDbType.TinyInt,
                "bit" => SqlDbType.Bit,
                _ => SqlDbType.NVarChar
            };
            return t;
        }

        private static string ToVN(string col) => col.ToLowerInvariant() switch
        {
            "tencv" or "tenchucvu" or "ten" => "Tên chức vụ",
            "mota" or "ghichu" or "description" => "Mô tả",
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

        private void ShrinkToFitContent()
        {
            try
            {
                grid.PerformLayout();
                root.PerformLayout();

                int headerH = TextRenderer.MeasureText(lblTitle.Text, lblTitle.Font).Height
                              + lblTitle.Padding.Vertical + 8;

                int gridH = 0;
                foreach (var kv in _controls)
                    gridH += (kv.Value.Height + 12);

                int actionsH = Math.Max(btnSave.Height, btnCancel.Height) + 24;
                int chromePad = root.Padding.Vertical + 20;

                int desiredH = headerH + gridH + actionsH + chromePad;

                int maxLabelW = 0;
                for (int i = 0; i < grid.Controls.Count; i++)
                    if (grid.GetColumn(grid.Controls[i]) == 0)
                        maxLabelW = Math.Max(maxLabelW, grid.Controls[i].PreferredSize.Width);

                int inputMin = 360;
                int desiredW = Math.Max(560, maxLabelW + inputMin + 48);

                var wa = Screen.FromControl(this).WorkingArea;
                desiredW = Math.Min(desiredW, (int)(wa.Width * 0.9));
                desiredH = Math.Min(desiredH, (int)(wa.Height * 0.9));

                this.ClientSize = new Size(desiredW, desiredH);
            }
            catch
            {
                this.ClientSize = new Size(600, 420);
            }
        }
    }
}
