using System.Data;
using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;

namespace LibraryManagement.Forms.SachRelated.QuanLyTacGia
{
    public partial class ThemTacGiaForm : Form
    {
        private const string TableName = "TacGia";
        private const int CompactNameHeight = 24;
        private const int DefaultLineHeight = 28;

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
        private readonly Dictionary<string, Control> _controlsByColumn =
            new(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> _requiredColumns =
            new(StringComparer.OrdinalIgnoreCase);

        public ThemTacGiaForm()
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

            btnAdd.Click   += (_, __) => DoInsert();
            btnCancel.Click += (_, __) => DialogResult = DialogResult.Cancel;
        }

        // ----- Schema -----
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

            _cols.Clear(); _requiredColumns.Clear();

            while (rd.Read())
            {
                var meta = new ColumnMeta
                {
                    Name       = rd.GetString(0),
                    DataType   = rd.GetString(1),
                    MaxLength  = rd.GetInt16(2),
                    IsNullable = rd.GetBoolean(3),
                    IsIdentity = rd.GetBoolean(4),
                    Ordinal    = rd.GetInt32(5),
                    IsComputed = rd.GetBoolean(6)
                };
                if (meta.IsIdentity || meta.IsComputed) continue; // hide ID/Computed
                _cols.Add(meta);
                if (!meta.IsNullable) _requiredColumns.Add(meta.Name);
            }

            var nameCol = new[] { "TenTG", "HoTen", "TenTacGia", "Ten" }
                .FirstOrDefault(n => _cols.Any(c => c.Name.Equals(n, StringComparison.OrdinalIgnoreCase)));
            if (nameCol != null) _requiredColumns.Add(nameCol);
        }

        // ----- UI -----
        private void BuildDynamicFields()
        {
            grid.RowStyles.Clear(); grid.Controls.Clear(); grid.RowCount = 0;
            _controlsByColumn.Clear();

            foreach (var col in _cols)
            {
                grid.RowCount += 1;
                grid.RowStyles.Add(new RowStyle(SizeType.Percent, 1f));

                bool isName = IsNameColumn(col.Name);

                // input first
                Control input; int inputHeight;
                if (IsDateType(col.DataType))
                {
                    var dtp = new DateTimePicker
                    {
                        Dock = DockStyle.Top, Anchor = AnchorStyles.Left | AnchorStyles.Right,
                        Format = DateTimePickerFormat.Short, Margin = new Padding(0, 0, 0, 12)
                    };
                    inputHeight = isName ? CompactNameHeight : DefaultLineHeight;
                    dtp.Height = inputHeight;
                    input = dtp;
                }
                else
                {
                    var tb = new TextBox { AutoSize = false, Margin = new Padding(0, 0, 0, 12) };
                    if (IsMultiline(col))
                    {
                        tb.Dock = DockStyle.Fill; tb.Multiline = true; tb.ScrollBars = ScrollBars.Vertical; tb.Height = 80;
                        inputHeight = tb.Height;
                    }
                    else
                    {
                        tb.Dock = DockStyle.Top; tb.Anchor = AnchorStyles.Left | AnchorStyles.Right;
                        inputHeight = isName ? CompactNameHeight : DefaultLineHeight; tb.Height = inputHeight;
                    }
                    input = tb;
                }

                // label + *
                var labelPanel = new FlowLayoutPanel
                {
                    Dock = DockStyle.Top, Anchor = AnchorStyles.Left | AnchorStyles.Right,
                    WrapContents = false, AutoSize = false, Height = inputHeight,
                    Padding = new Padding(6, 0, 0, 0), Margin = new Padding(0)
                };
                var lbl = new Label { AutoSize = true, Text = ToVietnameseLabel(col.Name), TextAlign = ContentAlignment.MiddleLeft };
                var star = new Label { AutoSize = true, Text = _requiredColumns.Contains(col.Name) ? "*" : "", ForeColor = Color.Firebrick, Font = new Font("Segoe UI", 10F, FontStyle.Bold) };
                int labelTop = ComputeLabelTopMargin(lbl, inputHeight);
                lbl.Margin  = new Padding(0, labelTop, 2, 0);
                star.Margin = new Padding(0, labelTop, 0, 0);

                labelPanel.Controls.Add(lbl); labelPanel.Controls.Add(star);
                grid.Controls.Add(labelPanel, 0, grid.RowCount - 1);
                _controlsByColumn[col.Name] = input;
                grid.Controls.Add(input, 1, grid.RowCount - 1);
            }
        }

        private static bool IsNameColumn(string col) =>
            col.Equals("TenTG", StringComparison.OrdinalIgnoreCase) ||
            col.Equals("HoTen", StringComparison.OrdinalIgnoreCase) ||
            col.Equals("TenTacGia", StringComparison.OrdinalIgnoreCase) ||
            col.Equals("Ten", StringComparison.OrdinalIgnoreCase);

        private static int ComputeLabelTopMargin(Label lbl, int targetHeight)
        {
            var textH = TextRenderer.MeasureText("Ag", lbl.Font).Height;
            int top = Math.Max(0, (targetHeight - textH) / 2);
            return Math.Max(0, top - 1);
        }

        // ----- Validation -----
        private void WireValidation()
        {
            foreach (var (name, c) in _controlsByColumn)
            {
                if (c is TextBox tb)
                    tb.TextChanged += (_, __) => { ValidateField(name); UpdateAddEnabled(); };
                else if (c is DateTimePicker dtp)
                    dtp.ValueChanged += (_, __) => { ValidateField(name); UpdateAddEnabled(); };
            }
        }

        private void UpdateAddEnabled()
        {
            bool ok = true;
            foreach (var name in _controlsByColumn.Keys)
                if (!ValidateField(name)) { ok = false; break; }
            btnAdd.Enabled = ok;
        }

        private bool ValidateField(string colName)
        {
            if (!_controlsByColumn.TryGetValue(colName, out var c)) return true;
            bool required = _requiredColumns.Contains(colName);

            if (c is TextBox tb)
            {
                string text = tb.Text?.Trim() ?? "";
                if (required && string.IsNullOrWhiteSpace(text))
                {
                    errorProvider1.SetError(tb, "Trường này là bắt buộc."); return false;
                }

                if (TryGetRegex(colName, out var rx, out var msg) && !string.IsNullOrEmpty(text))
                {
                    if (!rx.IsMatch(text)) { errorProvider1.SetError(tb, msg); return false; }
                }

                errorProvider1.SetError(tb, string.Empty);
                return true;
            }

            errorProvider1.SetError(c, string.Empty);
            return true;
        }

        // Regexes for author fields
        private static bool TryGetRegex(string col, out Regex regex, out string message)
        {
            string n = col.Replace("_", "").ToLowerInvariant();

            if (n is "tentg" or "hoten" or "ten" or "tentacgia")
            {
                regex = new Regex(@"^[\p{L}\p{M} .,'-]{2,100}$", RegexOptions.Compiled);
                message = "Tên 2–100 ký tự; cho phép chữ, khoảng trắng và .,'-"; return true;
            }
            if (n is "email" or "mail")
            {
                regex = new Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
                message = "Email không hợp lệ."; return true;
            }
            if (n is "sodienthoai" or "dienthoai" or "sdt" or "phone")
            {
                regex = new Regex(@"^(?:\+?84|0)\d{9,10}$", RegexOptions.Compiled);
                message = "Số điện thoại VN hợp lệ (0 hoặc +84, 10–11 số)."; return true;
            }
            if (n is "website" or "web")
            {
                regex = new Regex(@"^(https?://)?([A-Za-z0-9-]+\.)+[A-Za-z]{2,}(/.*)?$", RegexOptions.Compiled);
                message = "URL không hợp lệ (vd: https://tenmien.com)."; return true;
            }
            if (n is "diachi")
            {
                regex = new Regex(@"^[\p{L}\p{M}0-9\s,./\-#()]{5,200}$", RegexOptions.Compiled);
                message = "Địa chỉ tối thiểu 5 ký tự (chữ, số và ,./-#())."; return true;
            }
            if (n is "gioitinh")
            {
                regex = new Regex(@"^(Nam|Nữ|Khác)$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
                message = "Giới tính: Nam/Nữ/Khác."; return true;
            }
            if (n is "quoctich")
            {
                regex = new Regex(@"^[\p{L}\p{M} .'-]{2,60}$", RegexOptions.Compiled);
                message = "Quốc tịch 2–60 ký tự."; return true;
            }
            if (n is "ghichu" or "mota")
            {
                regex = new Regex(@"^[\p{L}\p{M}0-9\s,.;:!?()/\-#&%'" + "\"" + @"]{0,500}$", RegexOptions.Compiled);
                message = "Tối đa 500 ký tự, ký tự cơ bản."; return true;
            }

            regex = null!; message = ""; return false;
        }

        // ----- Insert -----
        private void DoInsert()
        {
            try
            {
                var cols = new System.Collections.Generic.List<string>();
                var prms = new System.Collections.Generic.List<SqlParameter>();

                foreach (var col in _cols)
                {
                    if (!_controlsByColumn.TryGetValue(col.Name, out var c)) continue;
                    object? value = GetValueFor(col, c, out var dbType);

                    if (value is null || (value is string s && string.IsNullOrWhiteSpace(s)))
                    {
                        if (col.IsNullable) value = DBNull.Value;
                        else { MessageBox.Show($"Vui lòng nhập: {ToVietnameseLabel(col.Name)}", "Thiếu dữ liệu"); return; }
                    }

                    cols.Add(col.Name);
                    prms.Add(new SqlParameter("@" + col.Name, dbType) { Value = value ?? DBNull.Value });
                }

                if (cols.Count == 0) { MessageBox.Show("Không có dữ liệu để thêm.", "Thông báo"); return; }

                using var conn = Db.Create();
                using var cmd  = new SqlCommand(
                    $"INSERT INTO {TableName} ({string.Join(",", cols)}) VALUES ({string.Join(",", cols.Select(c => "@" + c))});",
                    conn);
                cmd.Parameters.AddRange(prms.ToArray());
                conn.Open(); cmd.ExecuteNonQuery();

                DialogResult = DialogResult.OK; Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Không thể thêm tác giả: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private object? GetValueFor(ColumnMeta col, Control c, out SqlDbType dbType)
        {
            dbType = MapSqlDbType(col.DataType);
            if (c is DateTimePicker dtp) return dtp.Value;

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

        // ----- Helpers -----
        private static bool IsDateType(string dt) =>
            dt.Equals("date", StringComparison.OrdinalIgnoreCase) ||
            dt.Equals("datetime", StringComparison.OrdinalIgnoreCase) ||
            dt.Equals("datetime2", StringComparison.OrdinalIgnoreCase) ||
            dt.Equals("smalldatetime", StringComparison.OrdinalIgnoreCase);

        private static bool IsMultiline(ColumnMeta c)
        {
            var dt = c.DataType.ToLowerInvariant();
            return dt is "ntext" or "text" || (dt is "nvarchar" or "varchar" && c.MaxLength < 0);
        }
        private static bool IsNumeric(string dt)
        {
            dt = dt.ToLowerInvariant();
            return dt is "int" or "bigint" or "smallint" or "tinyint"
                   or "decimal" or "numeric" or "money" or "smallmoney"
                   or "float" or "real";
        }
        private static SqlDbType MapSqlDbType(string typeName) => typeName.ToLowerInvariant() switch
        {
            "int" => SqlDbType.Int, "bigint" => SqlDbType.BigInt, "smallint" => SqlDbType.SmallInt, "tinyint" => SqlDbType.TinyInt,
            "bit" => SqlDbType.Bit, "decimal" or "numeric" => SqlDbType.Decimal, "money" => SqlDbType.Money, "smallmoney" => SqlDbType.SmallMoney,
            "float" => SqlDbType.Float, "real" => SqlDbType.Real, "date" => SqlDbType.Date, "datetime" => SqlDbType.DateTime,
            "datetime2" => SqlDbType.DateTime2, "smalldatetime" => SqlDbType.SmallDateTime, "time" => SqlDbType.Time,
            "char" => SqlDbType.Char, "nchar" => SqlDbType.NChar, "varchar" => SqlDbType.VarChar, "nvarchar" => SqlDbType.NVarChar,
            "text" => SqlDbType.Text, "ntext" => SqlDbType.NText, "uniqueidentifier" => SqlDbType.UniqueIdentifier,
            "binary" => SqlDbType.Binary, "varbinary" => SqlDbType.VarBinary, _ => SqlDbType.NVarChar
        };

        private static string ToVietnameseLabel(string col) => col.ToLowerInvariant() switch
        {
            "tentg" or "hoten" or "ten" or "tentacgia" => "Tên tác giả",
            "ngaysinh" => "Ngày sinh", "gioitinh" => "Giới tính", "quoctich" => "Quốc tịch",
            "email" or "mail" => "Email", "sodienthoai" or "dienthoai" or "sdt" or "phone" => "Số điện thoại",
            "diachi" => "Địa chỉ", "website" or "web" => "Website", "ghichu" => "Ghi chú",
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

        // size to fit
        private void ShrinkToFitContent()
        {
            try
            {
                grid.PerformLayout(); root.PerformLayout();
                int headerH = TextRenderer.MeasureText(lblTitle.Text, lblTitle.Font).Height + lblTitle.Padding.Vertical + 8;
                int gridH = 0; foreach (var kv in _controlsByColumn) gridH += kv.Value.Height + 12;
                int actionsH = Math.Max(btnAdd.Height, btnCancel.Height) + 24;
                int desiredH = headerH + gridH + actionsH + root.Padding.Vertical + 20;

                int maxLabelW = 0;
                for (int i = 0; i < grid.Controls.Count; i++)
                    if (grid.GetColumn(grid.Controls[i]) == 0)
                        maxLabelW = Math.Max(maxLabelW, grid.Controls[i].PreferredSize.Width);

                int desiredW = Math.Max(540, maxLabelW + 360 + 48);
                var wa = Screen.FromControl(this).WorkingArea;
                ClientSize = new Size(Math.Min(desiredW, (int)(wa.Width * 0.9)), Math.Min(desiredH, (int)(wa.Height * 0.9)));
            }
            catch { ClientSize = new Size(600, 460); }
        }
    }
}
