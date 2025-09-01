using System.Data;
using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;

// PasswordHasher, Db

namespace LibraryManagement.Forms.NhanVienRelated.QuanLyNhanVien
{
    public partial class ThemNhanVienForm : Form
    {
        private const string TableName = "NhanVien";
        private const int CompactHeight = 28;
        private const int NameHeight = 26;

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
        private readonly Dictionary<string, Control> _controls =
            new(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> _required =
            new(StringComparer.OrdinalIgnoreCase);

        // Lookup for CV_ID (Chức vụ)
        private string? _cvIdColumn; // CV_ID | ChucVu_ID
        private string? _cvTable;    // ChucVu
        private string? _cvPk;       // CV_ID | ID
        private string? _cvName;     // TenCV | TenChucVu | Ten

        public ThemNhanVienForm()
        {
            InitializeComponent();

            StartPosition = FormStartPosition.CenterParent;
            AcceptButton  = btnAdd;
            CancelButton  = btnCancel;

            Load += (_, __) =>
            {
                try
                {
                    LoadSchema();
                    ResolveChucVuLookup();
                    BuildDynamicFields();
                    WireValidation();
                    UpdateAddEnabled();
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

            btnAdd.Click    += (_, __) => DoInsert();
            btnCancel.Click += (_, __) => DialogResult = DialogResult.Cancel;
        }

        // ---------- DB schema ----------
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

            _cols.Clear();
            _required.Clear();
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
                if (m.IsIdentity || m.IsComputed) continue; // hide ID/Computed
                _cols.Add(m);
                if (!m.IsNullable) _required.Add(m.Name);
            }

            // Always require TenNV if present
            if (_cols.Any(c => c.Name.Equals("TenNV", StringComparison.OrdinalIgnoreCase)))
                _required.Add("TenNV");
        }

        private void ResolveChucVuLookup()
        {
            _cvIdColumn = new[] { "CV_ID", "ChucVu_ID" }
                .FirstOrDefault(n => _cols.Any(c => c.Name.Equals(n, StringComparison.OrdinalIgnoreCase)));
            if (_cvIdColumn == null) return;

            using var conn = Db.Create();
            conn.Open();
            try
            {
                using var probe = new SqlCommand("SELECT TOP(1) * FROM ChucVu", conn);
                using var da    = new SqlDataAdapter(probe);
                var tmp = new DataTable();
                da.Fill(tmp);
                if (tmp.Columns.Count > 0)
                {
                    _cvTable = "ChucVu";
                    _cvPk    = new[] { "CV_ID", "ID" }.FirstOrDefault(tmp.Columns.Contains) ?? tmp.Columns[0].ColumnName;
                    _cvName  = new[] { "TenCV", "TenChucVu", "Ten" }.FirstOrDefault(tmp.Columns.Contains)
                               ?? tmp.Columns.Cast<DataColumn>().FirstOrDefault(c => c.DataType == typeof(string))?.ColumnName;
                }
            }
            catch { /* ignore */ }
        }

        // ---------- UI ----------
        // Ensure MatKhau right under TenNV, and Status at the end
        private IEnumerable<ColumnMeta> OrderedCols()
        {
            var list = _cols.ToList();

            int nameIdx = list.FindIndex(c => c.Name.Equals("TenNV", StringComparison.OrdinalIgnoreCase));
            int pwIdx   = list.FindIndex(c => c.Name.Equals("MatKhau", StringComparison.OrdinalIgnoreCase));
            if (nameIdx >= 0 && pwIdx >= 0 && pwIdx != nameIdx + 1)
            {
                var pw = list[pwIdx];
                list.RemoveAt(pwIdx);
                list.Insert(nameIdx + 1, pw);
            }

            int stIdx = list.FindIndex(c => c.Name.Equals("Status", StringComparison.OrdinalIgnoreCase) ||
                                            c.Name.Equals("TrangThai", StringComparison.OrdinalIgnoreCase));
            if (stIdx >= 0 && stIdx != list.Count - 1)
            {
                var st = list[stIdx];
                list.RemoveAt(stIdx);
                list.Add(st);
            }

            return list;
        }

        private void BuildDynamicFields()
        {
            grid.RowStyles.Clear();
            grid.Controls.Clear();
            grid.RowCount = 0;
            _controls.Clear();

            foreach (var col in OrderedCols())
            {
                grid.RowCount += 1;
                grid.RowStyles.Add(new RowStyle(SizeType.Percent, 1f));

                bool isName     = col.Name.Equals("TenNV", StringComparison.OrdinalIgnoreCase);
                bool isPassword = col.Name.Equals("MatKhau", StringComparison.OrdinalIgnoreCase);
                bool isBool     = IsBoolType(col.DataType);

                // Left: label + *
                var labelPanel = new FlowLayoutPanel
                {
                    Dock = DockStyle.Top,
                    Anchor = AnchorStyles.Left | AnchorStyles.Right,
                    WrapContents = false,
                    AutoSize = false,
                    Height = isName ? NameHeight : CompactHeight,
                    Padding = new Padding(6, 0, 0, 0),
                    Margin = new Padding(0)
                };
                var lbl = new Label { AutoSize = true, Text = ToVietnameseLabel(col.Name), TextAlign = ContentAlignment.MiddleLeft };
                var star = new Label
                {
                    AutoSize = true,
                    Text = _required.Contains(col.Name) ? "*" : "",
                    ForeColor = Color.Firebrick,
                    Font = new Font("Segoe UI", 10F, FontStyle.Bold)
                };
                int labelTop = ComputeLabelTopMargin(lbl, isName ? NameHeight : CompactHeight);
                lbl.Margin  = new Padding(0, labelTop, 2, 0);
                star.Margin = new Padding(0, labelTop, 0, 0);
                labelPanel.Controls.Add(lbl);
                labelPanel.Controls.Add(star);
                grid.Controls.Add(labelPanel, 0, grid.RowCount - 1);

                // Right: input
                Control input;
                if (_cvIdColumn != null && col.Name.Equals(_cvIdColumn, StringComparison.OrdinalIgnoreCase) &&
                    _cvTable != null && _cvPk != null && _cvName != null)
                {
                    var cb = new ComboBox
                    {
                        Dock = DockStyle.Top,
                        Anchor = AnchorStyles.Left | AnchorStyles.Right,
                        DropDownStyle = ComboBoxStyle.DropDownList,
                        Margin = new Padding(0, 0, 0, 12),
                        Height = CompactHeight
                    };
                    LoadChucVuInto(cb);
                    input = cb;
                }
                else if (IsDateType(col.DataType))
                {
                    input = new DateTimePicker
                    {
                        Dock = DockStyle.Top,
                        Anchor = AnchorStyles.Left | AnchorStyles.Right,
                        Format = DateTimePickerFormat.Short,
                        Margin = new Padding(0, 0, 0, 12),
                        Height = CompactHeight
                    };
                }
                else if (isBool)
                {
                    var chk = new CheckBox
                    {
                        Text = "Kích hoạt",
                        Checked = true, // default ON
                        AutoSize = true,
                        Dock = DockStyle.Top,
                        Margin = new Padding(0, 4, 0, 12)
                    };
                    input = chk;
                }
                else
                {
                    var tb = new TextBox
                    {
                        AutoSize = false,
                        Dock = DockStyle.Top,
                        Anchor = AnchorStyles.Left | AnchorStyles.Right,
                        Margin = new Padding(0, 0, 0, 12),
                        Height = isName ? NameHeight : CompactHeight
                    };
                    if (isPassword) tb.UseSystemPasswordChar = true;
                    input = tb;
                }

                _controls[col.Name] = input;
                grid.Controls.Add(input, 1, grid.RowCount - 1);
            }
        }

        private void LoadChucVuInto(ComboBox cb)
        {
            try
            {
                using var conn = Db.Create();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = $"SELECT {_cvPk}, {_cvName} FROM {_cvTable} ORDER BY {_cvName}";
                using var da = new SqlDataAdapter(cmd);
                var dt = new DataTable();
                da.Fill(dt);

                cb.DisplayMember = _cvName;
                cb.ValueMember   = _cvPk;
                cb.DataSource    = dt;
            }
            catch { /* ignore */ }
        }

        private static int ComputeLabelTopMargin(Label lbl, int targetHeight)
        {
            var textH = TextRenderer.MeasureText("Ag", lbl.Font).Height;
            int top = Math.Max(0, (targetHeight - textH) / 2);
            return Math.Max(0, top - 1);
        }

        // ---------- Validation ----------
        private void WireValidation()
        {
            foreach (var (name, c) in _controls)
            {
                switch (c)
                {
                    case TextBox tb:
                        tb.TextChanged += (_, __) => { ValidateField(name); UpdateAddEnabled(); };
                        break;
                    case DateTimePicker dtp:
                        dtp.ValueChanged += (_, __) => { ValidateField(name); UpdateAddEnabled(); };
                        break;
                    case ComboBox cb:
                        cb.SelectedIndexChanged += (_, __) => { ValidateField(name); UpdateAddEnabled(); };
                        break;
                    case CheckBox _:
                        // no validation needed
                        break;
                }
            }
        }

        private void UpdateAddEnabled()
        {
            bool ok = true;
            foreach (var name in _controls.Keys)
                if (!ValidateField(name)) { ok = false; break; }
            btnAdd.Enabled = ok;
        }

        private bool ValidateField(string colName)
        {
            if (!_controls.TryGetValue(colName, out var c)) return true;

            // CheckBox and ComboBox always valid if present/selected
            if (c is CheckBox) { errorProvider1.SetError(c, ""); return true; }

            bool required = _required.Contains(colName);

            if (c is TextBox tb)
            {
                string text = tb.Text?.Trim() ?? "";
                if (required && string.IsNullOrWhiteSpace(text))
                {
                    errorProvider1.SetError(tb, "Trường này là bắt buộc.");
                    return false;
                }

                if (TryRegex(colName, out var rx, out var msg) && text.Length > 0)
                {
                    if (!rx.IsMatch(text))
                    {
                        errorProvider1.SetError(tb, msg);
                        return false;
                    }
                }

                errorProvider1.SetError(tb, "");
                return true;
            }

            if (c is ComboBox cbx)
            {
                if (required && cbx.SelectedIndex < 0)
                {
                    errorProvider1.SetError(cbx, "Vui lòng chọn.");
                    return false;
                }
                errorProvider1.SetError(cbx, "");
            }

            return true;
        }

        private static bool TryRegex(string col, out Regex regex, out string message)
        {
            string n = col.ToLowerInvariant();

            if (n is "tennv" or "hoten" or "ten")
            {
                regex = new Regex(@"^[\p{L}\p{M}0-9 .,'&()/-]{2,100}$", RegexOptions.Compiled);
                message = "Tên 2–100 ký tự; cho phép chữ, số và ,.'&()/-.";
                return true;
            }

            if (n is "mail" or "email")
            {
                regex = new Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
                message = "Email không hợp lệ.";
                return true;
            }

            if (n is "sodienthoai" or "dienthoai" or "sdt" or "phone")
            {
                regex = new Regex(@"^(?:\+?84|0)\d{9,10}$", RegexOptions.Compiled);
                message = "SĐT VN hợp lệ (0 hoặc +84, 10–11 số).";
                return true;
            }

            if (n is "matkhau")
            {
                regex = new Regex(@"^.{6,100}$", RegexOptions.Compiled);
                message = "Mật khẩu tối thiểu 6 ký tự.";
                return true;
            }

            if (n is "diachi")
            {
                regex = new Regex(@"^[\p{L}\p{M}0-9\s,./\-#()]{5,200}$", RegexOptions.Compiled);
                message = "Địa chỉ tối thiểu 5 ký tự.";
                return true;
            }

            regex = null!;
            message = "";
            return false;
        }

        // ---------- Insert ----------
        private void DoInsert()
        {
            try
            {
                var cols = new List<string>();
                var prms = new List<SqlParameter>();

                foreach (var col in _cols)
                {
                    if (!_controls.TryGetValue(col.Name, out var c)) continue;

                    object? value;
                    SqlDbType dbType;

                    if (c is ComboBox cb)
                    {
                        value = cb.SelectedValue ?? (object)DBNull.Value;
                        dbType = SqlDbType.Int;
                    }
                    else
                    {
                        value = GetValueFor(col, c, out dbType);
                    }

                    if ((value == null || value is string s && string.IsNullOrWhiteSpace(s)) && !_required.Contains(col.Name))
                        value = DBNull.Value;

                    if (col.Name.Equals("MatKhau", StringComparison.OrdinalIgnoreCase))
                    {
                        var raw = (value == DBNull.Value ? "" : Convert.ToString(value) ?? "");
                        if (string.IsNullOrEmpty(raw))
                        {
                            MessageBox.Show("Vui lòng nhập mật khẩu.", "Thiếu dữ liệu");
                            return;
                        }
                        value = PasswordHasher.HashPassword(raw);
                        dbType = SqlDbType.NVarChar;
                    }

                    cols.Add(col.Name);
                    prms.Add(new SqlParameter("@" + col.Name, dbType) { Value = value ?? DBNull.Value });
                }

                if (cols.Count == 0)
                {
                    MessageBox.Show("Không có dữ liệu để thêm.", "Thông báo");
                    return;
                }

                var sql = $"INSERT INTO {TableName} ({string.Join(",", cols)}) VALUES ({string.Join(",", cols.Select(c => "@" + c))});";
                using var conn = Db.Create();
                using var cmd  = new SqlCommand(sql, conn);
                cmd.Parameters.AddRange(prms.ToArray());

                conn.Open();
                cmd.ExecuteNonQuery();

                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Không thể thêm nhân viên: " + ex.Message,
                    "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private object? GetValueFor(ColumnMeta col, Control c, out SqlDbType dbType)
        {
            dbType = MapSqlDbType(col.DataType);

            if (c is DateTimePicker dtp) return dtp.Value;
            if (c is CheckBox chk) return chk.Checked;

            var txt = (c as TextBox)?.Text?.Trim();
            if (string.IsNullOrWhiteSpace(txt)) return null;

            if (IsNumeric(col.DataType))
            {
                if (col.DataType is "int" or "bigint" or "smallint" or "tinyint")
                {
                    if (long.TryParse(txt, out var l)) return l;
                    throw new Exception($"Giá trị không hợp lệ cho {ToVietnameseLabel(col.Name)}");
                }
                if (col.DataType is "float" or "real" or "decimal" or "numeric" or "money" or "smallmoney")
                {
                    if (decimal.TryParse(txt, out var d)) return d;
                    throw new Exception($"Giá trị không hợp lệ cho {ToVietnameseLabel(col.Name)}");
                }
            }

            return txt;
        }

        // ---------- Helpers ----------
        private static bool IsDateType(string dt) =>
            dt.Equals("date", StringComparison.OrdinalIgnoreCase) ||
            dt.Equals("datetime", StringComparison.OrdinalIgnoreCase) ||
            dt.Equals("datetime2", StringComparison.OrdinalIgnoreCase) ||
            dt.Equals("smalldatetime", StringComparison.OrdinalIgnoreCase);

        private static bool IsBoolType(string dt) => dt.Equals("bit", StringComparison.OrdinalIgnoreCase);

        private static bool IsNumeric(string dt)
        {
            dt = dt.ToLowerInvariant();
            return dt is "int" or "bigint" or "smallint" or "tinyint"
                   or "decimal" or "numeric" or "money" or "smallmoney"
                   or "float" or "real";
        }

        private static SqlDbType MapSqlDbType(string typeName) => typeName.ToLowerInvariant() switch
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

        private static string ToVietnameseLabel(string col) => col.ToLowerInvariant() switch
        {
            "tennv" or "hoten" or "ten" => "Tên nhân viên",
            "mail" or "email" => "Email",
            "sodienthoai" or "dienthoai" or "sdt" or "phone" => "Số điện thoại",
            "cv_id" or "chucvu_id" => "Chức vụ",
            "matkhau" => "Mật khẩu",
            "diachi" => "Địa chỉ",
            "trangthai" or "status" => "Trạng thái",
            _ => SplitPascal(col)
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
                    gridH += kv.Value.Height + 12;

                int actionsH = Math.Max(btnAdd.Height, btnCancel.Height) + 24;
                int chromePad = root.Padding.Vertical + 20;

                int desiredH = headerH + gridH + actionsH + chromePad;

                int maxLabelW = 0;
                for (int i = 0; i < grid.Controls.Count; i++)
                    if (grid.GetColumn(grid.Controls[i]) == 0)
                        maxLabelW = Math.Max(maxLabelW, grid.Controls[i].PreferredSize.Width);

                int inputMin = 360;
                int desiredW = Math.Max(560, maxLabelW + inputMin + 48);

                var wa = Screen.FromControl(this).WorkingArea;
                desiredW = Math.Min(desiredW, (int)(wa.Width  * 0.9));
                desiredH = Math.Min(desiredH, (int)(wa.Height * 0.9));

                this.ClientSize = new Size(desiredW, desiredH);
            }
            catch
            {
                this.ClientSize = new Size(600, 480);
            }
        }
    }
}
