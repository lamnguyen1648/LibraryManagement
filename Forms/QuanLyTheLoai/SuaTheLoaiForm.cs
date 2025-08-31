using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using Microsoft.Data.SqlClient;

namespace LibraryManagement.Forms.QuanLyTheLoai
{
    public partial class SuaTheLoaiForm : Form
    {
        private const string TableName = "TheLoai";
        private const int CompactNameHeight = 24;
        private const int DefaultLineHeight = 28;

        private string? _idColumnName;   // detected (identity) or fallback TL_ID/ID
        private DataRow? _row;

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

        private readonly int _id;

        public SuaTheLoaiForm(int theLoaiId)
        {
            _id = theLoaiId;
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
                    LoadExistingRow();
                    BuildDynamicFields();
                    BindRowToControls();
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

        // ---- Detect PK/ID column (prefer identity) ----
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
                bool isIdent = rd.GetBoolean(1);
                if (isIdent) { _idColumnName = name; break; }
                if (fallback == null && (name.Equals("TL_ID", StringComparison.OrdinalIgnoreCase)
                                         || name.Equals("ID", StringComparison.OrdinalIgnoreCase)))
                    fallback = name;
            }
            _idColumnName ??= fallback ?? "TL_ID";
        }

        // ---- Schema (hide identity/computed; require name) ----
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
            _requiredColumns.Clear();

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

            var nameCol = new[] { "TenTL", "TenTheLoai", "Ten" }
                .FirstOrDefault(n => _cols.Any(c => c.Name.Equals(n, StringComparison.OrdinalIgnoreCase)));
            if (nameCol != null) _requiredColumns.Add(nameCol);
        }

        // ---- Load existing record ----
        private void LoadExistingRow()
        {
            if (_idColumnName == null) throw new InvalidOperationException("Chưa xác định cột ID.");

            using var conn = Db.Create();
            using var cmd  = conn.CreateCommand();
            cmd.CommandText = $"SELECT TOP 1 * FROM {TableName} WHERE {_idColumnName} = @id";
            cmd.Parameters.Add(new SqlParameter("@id", SqlDbType.Int) { Value = _id });

            using var da = new SqlDataAdapter(cmd);
            var dt = new DataTable();
            da.Fill(dt);

            if (dt.Rows.Count == 0)
                throw new Exception($"Không tìm thấy bản ghi với {_idColumnName} = {_id}.");
            _row = dt.Rows[0];
        }

        // ---- UI build (compact & aligned name) ----
        private void BuildDynamicFields()
        {
            grid.RowStyles.Clear();
            grid.Controls.Clear();
            grid.RowCount = 0;
            _controlsByColumn.Clear();

            foreach (var col in _cols)
            {
                grid.RowCount += 1;
                grid.RowStyles.Add(new RowStyle(SizeType.Percent, 1f));

                bool isName = IsNameColumn(col.Name);

                // input first
                Control input;
                int inputHeight;

                if (IsDateType(col.DataType))
                {
                    var dtp = new DateTimePicker
                    {
                        Dock = DockStyle.Top,
                        Anchor = AnchorStyles.Left | AnchorStyles.Right,
                        Format = DateTimePickerFormat.Short,
                        Margin = new Padding(0, 0, 0, 12)
                    };
                    inputHeight = isName ? CompactNameHeight : DefaultLineHeight;
                    dtp.Height = inputHeight;
                    input = dtp;
                }
                else
                {
                    var tb = new TextBox
                    {
                        AutoSize = false,
                        Margin = new Padding(0, 0, 0, 12)
                    };

                    if (IsMultiline(col))
                    {
                        tb.Dock = DockStyle.Fill;
                        tb.Multiline = true;
                        tb.ScrollBars = ScrollBars.Vertical;
                        tb.Height = 80;
                        inputHeight = tb.Height;
                    }
                    else
                    {
                        tb.Dock = DockStyle.Top;
                        tb.Anchor = AnchorStyles.Left | AnchorStyles.Right;
                        inputHeight = isName ? CompactNameHeight : DefaultLineHeight;
                        tb.Height = inputHeight;
                    }
                    input = tb;
                }

                // label + *
                var labelPanel = new FlowLayoutPanel
                {
                    Dock = DockStyle.Top,
                    Anchor = AnchorStyles.Left | AnchorStyles.Right,
                    WrapContents = false,
                    AutoSize = false,
                    Height = inputHeight,
                    Padding = new Padding(6, 0, 0, 0),
                    Margin = new Padding(0)
                };

                var lbl = new Label
                {
                    AutoSize = true,
                    Text = ToVietnameseLabel(col.Name),
                    TextAlign = ContentAlignment.MiddleLeft
                };
                var star = new Label
                {
                    AutoSize = true,
                    Text = _requiredColumns.Contains(col.Name) ? "*" : "",
                    ForeColor = Color.Firebrick,
                    Font = new Font("Segoe UI", 10F, FontStyle.Bold)
                };

                int labelTop = ComputeLabelTopMargin(lbl, inputHeight);
                lbl.Margin  = new Padding(0, labelTop, 2, 0);
                star.Margin = new Padding(0, labelTop, 0, 0);

                labelPanel.Controls.Add(lbl);
                labelPanel.Controls.Add(star);

                grid.Controls.Add(labelPanel, 0, grid.RowCount - 1);
                _controlsByColumn[col.Name] = input;
                grid.Controls.Add(input, 1, grid.RowCount - 1);
            }
        }

        private void BindRowToControls()
        {
            if (_row == null) return;

            foreach (var col in _cols)
            {
                if (!_controlsByColumn.TryGetValue(col.Name, out var c)) continue;
                object? value = _row[col.Name];
                if (value is DBNull) value = null;

                if (c is DateTimePicker dtp)
                {
                    if (value is DateTime d) dtp.Value = d;
                }
                else if (c is TextBox tb)
                {
                    tb.Text = value?.ToString() ?? "";
                }
            }
        }

        // ---- Validation (same regex as add) ----
        private void WireValidation()
        {
            foreach (var (name, c) in _controlsByColumn)
            {
                if (c is TextBox tb)
                    tb.TextChanged += (_, __) => { ValidateField(name); UpdateSaveEnabled(); };
                else if (c is DateTimePicker dtp)
                    dtp.ValueChanged += (_, __) => { ValidateField(name); UpdateSaveEnabled(); };
            }
        }

        private void UpdateSaveEnabled()
        {
            bool ok = true;
            foreach (var name in _controlsByColumn.Keys)
                if (!ValidateField(name)) { ok = false; break; }
            btnSave.Enabled = ok;
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
                    errorProvider1.SetError(tb, "Trường này là bắt buộc.");
                    return false;
                }

                if (TryGetRegex(colName, out var rx, out var msg) && !string.IsNullOrEmpty(text))
                {
                    if (!rx.IsMatch(text))
                    {
                        errorProvider1.SetError(tb, msg);
                        return false;
                    }
                }

                errorProvider1.SetError(tb, string.Empty);
                return true;
            }

            errorProvider1.SetError(c, string.Empty);
            return true;
        }

        private static bool TryGetRegex(string col, out Regex regex, out string message)
        {
            string n = col.Replace("_", "").ToLowerInvariant();

            if (n is "tentl" or "tentheloai" or "ten")
            {
                regex = new Regex(@"^[\p{L}\p{M}0-9 .,'&()/-]{2,100}$", RegexOptions.Compiled);
                message = "Tên thể loại 2–100 ký tự; cho phép chữ, số và ,.'&()/-.";
                return true;
            }

            if (n is "mota" or "ghichu")
            {
                regex = new Regex(@"^[\p{L}\p{M}0-9\s,.;:!?()/\-#&%'" + "\"" + @"]{0,500}$", RegexOptions.Compiled);
                message = "Chỉ dùng chữ/số/ký tự cơ bản; tối đa 500 ký tự.";
                return true;
            }

            if (n is "trangthai" or "status")
            {
                regex = new Regex(@"^[\p{L}\p{M}0-9 _-]{1,50}$", RegexOptions.Compiled);
                message = "Trạng thái 1–50 ký tự.";
                return true;
            }

            regex = null!;
            message = "";
            return false;
        }

        // ---- UPDATE ----
        private void DoUpdate()
        {
            try
            {
                if (_idColumnName == null) throw new InvalidOperationException("Chưa xác định cột ID.");

                var sets = new List<string>();
                var prms = new List<SqlParameter>();

                foreach (var col in _cols)
                {
                    if (!_controlsByColumn.TryGetValue(col.Name, out var c)) continue;

                    object? value = GetValueFor(col, c, out var dbType);
                    if (value is null || (value is string s && string.IsNullOrWhiteSpace(s)))
                    {
                        if (col.IsNullable) value = DBNull.Value;
                        else { MessageBox.Show($"Vui lòng nhập: {ToVietnameseLabel(col.Name)}", "Thiếu dữ liệu"); return; }
                    }

                    sets.Add($"{col.Name} = @{col.Name}");
                    prms.Add(new SqlParameter("@" + col.Name, dbType) { Value = value ?? DBNull.Value });
                }

                if (sets.Count == 0)
                {
                    MessageBox.Show("Không có thay đổi để lưu.", "Thông báo");
                    return;
                }

                var sql = $"UPDATE {TableName} SET {string.Join(", ", sets)} WHERE {_idColumnName} = @id";
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

        // ---- Helpers ----
        private static bool IsNameColumn(string col) =>
            col.Equals("TenTL", StringComparison.OrdinalIgnoreCase) ||
            col.Equals("TenTheLoai", StringComparison.OrdinalIgnoreCase) ||
            col.Equals("Ten", StringComparison.OrdinalIgnoreCase);

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

        // auto-fit popup to content
        private void ShrinkToFitContent()
        {
            try
            {
                grid.PerformLayout();
                root.PerformLayout();

                int headerH = TextRenderer.MeasureText(lblTitle.Text, lblTitle.Font).Height
                              + lblTitle.Padding.Vertical + 8;

                int gridH = 0;
                foreach (var kv in _controlsByColumn)
                    gridH += kv.Value.Height + 12;

                int actionsH = Math.Max(btnSave.Height, btnCancel.Height) + 24;
                int chromePad = root.Padding.Vertical + 20;

                int desiredH = headerH + gridH + actionsH + chromePad;

                int maxLabelW = 0;
                for (int i = 0; i < grid.Controls.Count; i++)
                    if (grid.GetColumn(grid.Controls[i]) == 0)
                        maxLabelW = Math.Max(maxLabelW, grid.Controls[i].PreferredSize.Width);

                int inputMin = 360;
                int desiredW = Math.Max(540, maxLabelW + inputMin + 48);

                var wa = Screen.FromControl(this).WorkingArea;
                desiredW = Math.Min(desiredW, (int)(wa.Width  * 0.9));
                desiredH = Math.Min(desiredH, (int)(wa.Height * 0.9));

                this.ClientSize = new Size(desiredW, desiredH);
            }
            catch
            {
                this.ClientSize = new Size(600, 460);
            }
        }

        private static int ComputeLabelTopMargin(Label lbl, int targetHeight)
        {
            var textH = TextRenderer.MeasureText("Ag", lbl.Font).Height;
            int top = Math.Max(0, (targetHeight - textH) / 2);
            return Math.Max(0, top - 1);
        }
    }
}
