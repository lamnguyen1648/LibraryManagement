using System.Data;
using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;

namespace LibraryManagement.Forms.SachRelated.QuanLyNhaXuatBan
{
    public partial class SuaNxbForm : Form
    {
        // Resolved from DB at runtime
        private string? _tableName;       // "NXB" or "NhaXuatBan"
        private string? _idColumnName;    // "NXB_ID" or "ID"
        private string? _nameColumn;      // "TenNXB" or "Ten" or "TenNhaXuatBan"

        private readonly int _id;

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
        private DataRow? _row;

        public SuaNxbForm(int nxbId)
        {
            _id = nxbId;
            InitializeComponent();

            StartPosition = FormStartPosition.CenterParent;
            AcceptButton  = btnSave;
            CancelButton  = btnCancel;

            Load += (_, __) =>
            {
                try
                {
                    ResolveTableAndColumns();
                    LoadSchema();
                    LoadExistingRow();
                    BuildDynamicFields();
                    BindRowToControls();
                    WireValidation();
                    UpdateSaveEnabled();
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

        // ---------- Resolve table/columns ----------
        private void ResolveTableAndColumns()
        {
            using var conn = Db.Create();
            conn.Open();

            foreach (var table in new[] { "NXB", "NhaXuatBan" })
            {
                try
                {
                    using var probe = new SqlCommand($"SELECT TOP(1) * FROM {table}", conn);
                    using var da = new SqlDataAdapter(probe);
                    var dt = new DataTable();
                    da.Fill(dt);
                    if (dt.Columns.Count == 0) continue;

                    _tableName    = table;
                    _idColumnName = new[] { "NXB_ID", "ID" }.FirstOrDefault(c => dt.Columns.Contains(c))
                                    ?? dt.Columns[0].ColumnName;
                    _nameColumn   = new[] { "TenNXB", "Ten", "TenNhaXuatBan" }.FirstOrDefault(c => dt.Columns.Contains(c))
                                    ?? dt.Columns.Cast<DataColumn>().FirstOrDefault(c => c.DataType == typeof(string))?.ColumnName;
                    return;
                }
                catch { /* try next */ }
            }

            throw new Exception("Không tìm thấy bảng NXB/Nhà Xuất Bản.");
        }

        // ---------- Schema & existing row ----------
        private void LoadSchema()
        {
            if (_tableName == null) throw new InvalidOperationException("Chưa xác định bảng NXB.");

            using var conn = Db.Create();
            using var cmd  = conn.CreateCommand();
            cmd.CommandText = @"
SELECT  c.name, t.name, c.max_length, c.is_nullable, c.is_identity, c.column_id, c.is_computed
FROM sys.columns c
JOIN sys.types   t ON c.user_type_id = t.user_type_id
WHERE c.object_id = OBJECT_ID(N'dbo." + _tableName + @"')
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

                // Do not show identity/computed columns
                if (meta.IsIdentity || meta.IsComputed) continue;

                _cols.Add(meta);
                if (!meta.IsNullable) _requiredColumns.Add(meta.Name);
            }

            // Always require name column
            if (_nameColumn != null) _requiredColumns.Add(_nameColumn);
        }

        private void LoadExistingRow()
        {
            if (_tableName == null || _idColumnName == null)
                throw new InvalidOperationException("Chưa xác định bảng/ID.");

            using var conn = Db.Create();
            using var cmd  = conn.CreateCommand();
            cmd.CommandText = $"SELECT TOP 1 * FROM {_tableName} WHERE {_idColumnName} = @id";
            cmd.Parameters.Add(new SqlParameter("@id", SqlDbType.Int) { Value = _id });

            using var da = new SqlDataAdapter(cmd);
            var dt = new DataTable();
            da.Fill(dt);

            if (dt.Rows.Count == 0)
                throw new Exception($"Không tìm thấy bản ghi với {_idColumnName} = {_id}.");
            _row = dt.Rows[0];
        }

        // ---------- UI ----------
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

                // Label + *
                var labelPanel = new FlowLayoutPanel
                {
                    Dock = DockStyle.Fill,
                    FlowDirection = FlowDirection.LeftToRight,
                    WrapContents = false,
                    AutoSize = false,
                    Padding = new Padding(6, 4, 0, 0),
                    Margin = new Padding(0)
                };

                var lbl = new Label
                {
                    AutoSize = true,
                    Text = ToVietnameseLabel(col.Name),
                    TextAlign = ContentAlignment.MiddleLeft,
                    Margin = new Padding(0, 4, 2, 0)
                };
                var star = new Label
                {
                    AutoSize = true,
                    Text = _requiredColumns.Contains(col.Name) ? "*" : "",
                    ForeColor = Color.Firebrick,
                    Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                    Margin = new Padding(0, 4, 0, 0)
                };

                labelPanel.Controls.Add(lbl);
                labelPanel.Controls.Add(star);
                grid.Controls.Add(labelPanel, 0, grid.RowCount - 1);

                // Input control
                Control input;
                if (IsDateType(col.DataType))
                {
                    input = new DateTimePicker
                    {
                        Dock = DockStyle.Fill,
                        Format = DateTimePickerFormat.Short,
                        Margin = new Padding(0, 0, 0, 12)
                    };
                }
                else
                {
                    var tb = new TextBox
                    {
                        Dock = DockStyle.Fill,
                        AutoSize = false,
                        Margin = new Padding(0, 0, 0, 12)
                    };
                    if (IsMultiline(col))
                    {
                        tb.Multiline = true;
                        tb.ScrollBars = ScrollBars.Vertical;
                        tb.Height = 80;
                    }
                    else
                    {
                        tb.Multiline = false;
                        tb.Height = 32;
                    }
                    input = tb;
                }

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

        // ---------- Validation (same regex logic as ThemNXB) ----------
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
            {
                if (!ValidateField(name)) { ok = false; break; }
            }
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

        // Same regex set used in ThemNXB
        private static bool TryGetRegex(string col, out Regex regex, out string message)
        {
            string n = col.Replace("_", "").ToLowerInvariant();

            if (n is "tennxb" or "ten" or "tennhaxuatban")
            {
                regex = new Regex(@"^[\p{L}\p{M}0-9 .,'&()/-]{2,100}$", RegexOptions.Compiled);
                message = "Tên không hợp lệ (2–100 ký tự; chữ, số và ,.'&()/-).";
                return true;
            }

            if (n is "email" or "mail")
            {
                regex = new Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
                message = "Email không hợp lệ (vd: ten@mien.com).";
                return true;
            }

            if (n is "sodienthoai" or "dienthoai" or "sdt" or "phone")
            {
                regex = new Regex(@"^(?:\+?84|0)\d{9,10}$", RegexOptions.Compiled);
                message = "Số điện thoại VN hợp lệ (bắt đầu 0 hoặc +84, 10–11 số).";
                return true;
            }

            if (n is "masothue" or "mst")
            {
                regex = new Regex(@"^\d{10}(\d{3})?$", RegexOptions.Compiled);
                message = "Mã số thuế phải có 10 hoặc 13 chữ số.";
                return true;
            }

            if (n is "website" or "web")
            {
                regex = new Regex(@"^(https?://)?([A-Za-z0-9-]+\.)+[A-Za-z]{2,}(/.*)?$", RegexOptions.Compiled);
                message = "URL không hợp lệ (vd: https://tenmien.com).";
                return true;
            }

            if (n is "diachi" or "address")
            {
                regex = new Regex(@"^[\p{L}\p{M}0-9\s,./\-#()]{5,200}$", RegexOptions.Compiled);
                message = "Địa chỉ tối thiểu 5 ký tự (chỉ dùng chữ, số và ,./-#()).";
                return true;
            }

            regex = null!;
            message = "";
            return false;
        }

        // ---------- Save (UPDATE) ----------
        private void DoUpdate()
        {
            try
            {
                if (_tableName == null || _idColumnName == null)
                    throw new InvalidOperationException("Chưa xác định bảng/ID.");

                // ensure valid
                UpdateSaveEnabled();
                if (!btnSave.Enabled) return;

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

                var sql = $"UPDATE {_tableName} SET {string.Join(", ", sets)} WHERE {_idColumnName} = @id";
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

        // ---------- Helpers ----------
        private static bool IsNumeric(string dt)
        {
            dt = dt.ToLowerInvariant();
            return dt is "int" or "bigint" or "smallint" or "tinyint"
                   or "decimal" or "numeric" or "money" or "smallmoney"
                   or "float" or "real";
        }

        private static bool IsMultiline(ColumnMeta c)
        {
            var dt = c.DataType.ToLowerInvariant();
            return dt is "ntext" or "text" || (dt is "nvarchar" or "varchar" && c.MaxLength < 0);
        }

        private static bool IsDateType(string dt) =>
            dt.Equals("date", StringComparison.OrdinalIgnoreCase) ||
            dt.Equals("datetime", StringComparison.OrdinalIgnoreCase) ||
            dt.Equals("datetime2", StringComparison.OrdinalIgnoreCase) ||
            dt.Equals("smalldatetime", StringComparison.OrdinalIgnoreCase);

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
            "tennxb" or "ten" or "tennhaxuatban" => "Tên NXB",
            "diachi"        => "Địa chỉ",
            "email" or "mail" => "Email",
            "sodienthoai" or "dienthoai" or "sdt" or "phone" => "Số điện thoại",
            "masothue" or "mst" => "Mã số thuế",
            "website" or "web" => "Website",
            "ghichu"        => "Ghi chú",
            "ngaythanhlap"  => "Ngày thành lập",
            "trangthai"     => "Trạng thái",
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
    }
}
