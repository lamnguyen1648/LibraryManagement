using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Microsoft.Data.SqlClient;

namespace LibraryManagement.Forms.QuanLySach
{
    public partial class ThemSachForm : Form
    {
        private const string TableName = "Sach";

        // Exclude from the UI
        private static readonly HashSet<string> ExcludedColumns =
            new(StringComparer.OrdinalIgnoreCase) { "QR_Code" };

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
        private readonly Dictionary<string, Control> _controlsByColumn = new(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> _requiredColumns = new(StringComparer.OrdinalIgnoreCase);

        public ThemSachForm()
        {
            InitializeComponent();

            // Center and make buttons act as default/cancel
            StartPosition = FormStartPosition.CenterParent;
            AcceptButton = btnAdd;
            CancelButton = btnCancel;

            Load += (_, __) =>
            {
                try
                {
                    LoadSchema();
                    BuildDynamicFields();
                    LoadLookups();     // NXB_ID / TG_ID / TL_ID => ComboBox lists
                    WireValidation();
                    UpdateAddEnabled();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Không thể tải biểu mẫu: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    DialogResult = DialogResult.Cancel;
                    Close();
                }
            };

            btnAdd.Click += (_, __) => DoInsert();
            btnCancel.Click += (_, __) => DialogResult = DialogResult.Cancel;
        }

        // Read DB schema (required vs nullable; exclude identity/computed/QR_Code)
        private void LoadSchema()
        {
            using var conn = Db.Create();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
SELECT  c.name          AS ColumnName,
        t.name          AS DataType,
        c.max_length    AS MaxLength,
        c.is_nullable   AS IsNullable,
        c.is_identity   AS IsIdentity,
        c.column_id     AS Ordinal,
        c.is_computed   AS IsComputed
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

                if (meta.IsIdentity || meta.IsComputed || ExcludedColumns.Contains(meta.Name))
                    continue;

                _cols.Add(meta);
                if (!meta.IsNullable)
                    _requiredColumns.Add(meta.Name);
            }
        }

        // Build dynamic UI rows: (label + red *) | input
        private void BuildDynamicFields()
        {
            grid.RowStyles.Clear();
            grid.Controls.Clear();
            grid.RowCount = 0;
            _controlsByColumn.Clear();

            foreach (var col in _cols)
            {
                grid.RowCount += 1;
                grid.RowStyles.Add(new RowStyle(SizeType.Percent, 1f)); // equal % heights

                // Left cell: label + star
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

                // Right cell: input control
                Control input;
                if (IsLookupColumn(col.Name))
                {
                    input = new ComboBox
                    {
                        Dock = DockStyle.Fill,
                        DropDownStyle = ComboBoxStyle.DropDownList, // fixed list
                        AutoCompleteMode = AutoCompleteMode.None,   // must be None for DropDownList
                        AutoCompleteSource = AutoCompleteSource.ListItems,
                        Margin = new Padding(0, 0, 0, 12)
                    };
                }
                else if (IsDateType(col.DataType))
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
                        AutoSize = false,              // allow height adjustment
                        Margin = new Padding(0, 0, 0, 12)
                    };

                    if (IsMultiline(col))
                    {
                        tb.Multiline = true;
                        tb.ScrollBars = ScrollBars.Vertical;
                        tb.Height = 80;                // taller default for long text
                    }
                    else
                    {
                        tb.Multiline = false;
                        tb.Height = 32;                // consistent single-line height
                    }

                    input = tb;
                }

                _controlsByColumn[col.Name] = input;
                grid.Controls.Add(input, 1, grid.RowCount - 1);
            }
        }

        // Lookup columns
        private static bool IsLookupColumn(string name) =>
            name.Equals("NXB_ID", StringComparison.OrdinalIgnoreCase) ||
            name.Equals("TG_ID",  StringComparison.OrdinalIgnoreCase) ||
            name.Equals("TL_ID",  StringComparison.OrdinalIgnoreCase);

        private static bool IsDateType(string dt) =>
            dt.Equals("date", StringComparison.OrdinalIgnoreCase) ||
            dt.Equals("datetime", StringComparison.OrdinalIgnoreCase) ||
            dt.Equals("datetime2", StringComparison.OrdinalIgnoreCase) ||
            dt.Equals("smalldatetime", StringComparison.OrdinalIgnoreCase);

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
            return dt is "ntext" or "text" || (dt is "nvarchar" or "varchar" && c.MaxLength < 0); // MAX
        }

        // Wire validation to enable/disable Add button
        private void WireValidation()
        {
            foreach (var (name, control) in _controlsByColumn)
            {
                if (control is TextBox tb)
                    tb.TextChanged += (_, __) => UpdateAddEnabled();
                else if (control is ComboBox cb)
                {
                    cb.SelectedIndexChanged += (_, __) => UpdateAddEnabled();
                    cb.TextChanged +=        (_, __) => UpdateAddEnabled(); // fallback editable
                }
                else if (control is DateTimePicker dtp)
                    dtp.ValueChanged += (_, __) => UpdateAddEnabled();
            }
        }

        private void UpdateAddEnabled()
        {
            bool allOk = true;

            foreach (var req in _requiredColumns)
            {
                if (!_controlsByColumn.TryGetValue(req, out var c)) continue;

                if (c is TextBox tb)
                    allOk &= !string.IsNullOrWhiteSpace(tb.Text);
                else if (c is ComboBox cb)
                {
                    // If bound (DropDownList), ensure there is a selection; otherwise, allow non-empty text.
                    if (cb.DropDownStyle == ComboBoxStyle.DropDownList)
                        allOk &= cb.SelectedItem != null;
                    else
                        allOk &= !string.IsNullOrWhiteSpace(cb.Text);
                }
                else if (c is DateTimePicker)
                    allOk &= true;

                if (!allOk) break;
            }

            btnAdd.Enabled = allOk;
        }

        // Load lookup data for NXB / Tác giả / Thể loại
        private void LoadLookups()
        {
            TryBindLookup("NXB_ID", new[] { "NXB", "NhaXuatBan" }, new[] { "NXB_ID", "ID" }, new[] { "TenNXB", "Ten", "TenNhaXuatBan" });
            TryBindLookup("TG_ID",  new[] { "TacGia" },            new[] { "TG_ID", "ID" },      new[] { "TenTG", "HoTen", "TenTacGia" });
            TryBindLookup("TL_ID",  new[] { "TheLoai" },           new[] { "TL_ID", "ID" },      new[] { "TenTL", "TenTheLoai", "Ten" });
        }

        private void TryBindLookup(string columnName, string[] tableCandidates, string[] idCandidates, string[] nameCandidates)
        {
            if (!_controlsByColumn.TryGetValue(columnName, out var ctrl) || ctrl is not ComboBox cb)
                return;

            using var conn = Db.Create();
            conn.Open();

            foreach (var table in tableCandidates)
            foreach (var id in idCandidates)
            foreach (var name in nameCandidates)
            {
                try
                {
                    using var cmd = new SqlCommand($"SELECT {id}, {name} FROM {table} ORDER BY {name}", conn);
                    using var da = new SqlDataAdapter(cmd);
                    var dt = new DataTable();
                    da.Fill(dt);
                    if (dt.Rows.Count == 0) continue;

                    cb.DisplayMember = name;
                    cb.ValueMember   = id;
                    cb.DataSource    = dt;

                    // Valid combo settings for DropDownList:
                    cb.DropDownStyle     = ComboBoxStyle.DropDownList;
                    cb.AutoCompleteMode  = AutoCompleteMode.None;          // must be None for DropDownList
                    cb.AutoCompleteSource = AutoCompleteSource.ListItems;
                    return;
                }
                catch
                {
                    // Try next candidate combination
                }
            }

            // Fallback: allow free typing if we couldn't bind a list
            cb.DataSource = null;
            cb.Items.Clear();
            cb.DropDownStyle     = ComboBoxStyle.DropDown;
            cb.AutoCompleteMode  = AutoCompleteMode.None;
            cb.AutoCompleteSource = AutoCompleteSource.None;
        }

        // Insert row from current control values
        private void DoInsert()
        {
            try
            {
                var cols = new List<string>();
                var prms = new List<SqlParameter>();

                foreach (var col in _cols)
                {
                    if (!_controlsByColumn.TryGetValue(col.Name, out var c)) continue;

                    object? value = GetValueFor(col, c, out var dbType);
                    if (value is null || (value is string s && string.IsNullOrWhiteSpace(s)))
                    {
                        if (col.IsNullable) value = DBNull.Value;
                        else
                        {
                            MessageBox.Show($"Vui lòng nhập: {ToVietnameseLabel(col.Name)}", "Thiếu dữ liệu");
                            return;
                        }
                    }

                    cols.Add(col.Name);
                    var p = new SqlParameter("@" + col.Name, dbType) { Value = value ?? DBNull.Value };
                    prms.Add(p);
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

                DialogResult = DialogResult.OK; // signal parent to refresh
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Không thể thêm sách: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private object? GetValueFor(ColumnMeta col, Control c, out SqlDbType dbType)
        {
            dbType = MapSqlDbType(col.DataType);

            if (c is ComboBox cb)
                return cb.SelectedValue ?? (object?)cb.Text;

            if (c is DateTimePicker dtp)
                return dtp.Value;

            var txt = (c as TextBox)?.Text?.Trim();
            if (string.IsNullOrWhiteSpace(txt)) return null;

            if (IsNumeric(col.DataType))
            {
                if (col.DataType.Equals("int", StringComparison.OrdinalIgnoreCase) ||
                    col.DataType.Equals("bigint", StringComparison.OrdinalIgnoreCase) ||
                    col.DataType.Equals("smallint", StringComparison.OrdinalIgnoreCase) ||
                    col.DataType.Equals("tinyint", StringComparison.OrdinalIgnoreCase))
                {
                    if (long.TryParse(txt, out var l)) return l;
                    throw new Exception($"Giá trị không hợp lệ cho {ToVietnameseLabel(col.Name)}");
                }

                if (col.DataType.Equals("float", StringComparison.OrdinalIgnoreCase) ||
                    col.DataType.Equals("real", StringComparison.OrdinalIgnoreCase) ||
                    col.DataType.Equals("decimal", StringComparison.OrdinalIgnoreCase) ||
                    col.DataType.Equals("numeric", StringComparison.OrdinalIgnoreCase) ||
                    col.DataType.Equals("money", StringComparison.OrdinalIgnoreCase) ||
                    col.DataType.Equals("smallmoney", StringComparison.OrdinalIgnoreCase))
                {
                    if (decimal.TryParse(txt, out var d)) return d;
                    throw new Exception($"Giá trị không hợp lệ cho {ToVietnameseLabel(col.Name)}");
                }
            }

            return txt; // NVARCHAR-ish default
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

        // Map DB column names -> Vietnamese labels
        private static string ToVietnameseLabel(string col) => col.ToLowerInvariant() switch
        {
            "tensach"       => "Tên sách",
            "namxuatban"    => "Năm xuất bản",
            "tinhtrangsach" => "Tình trạng sách",
            "nxb_id"        => "Nhà Xuất Bản",
            "tg_id"         => "Tác Giả",
            "tl_id"         => "Thể Loại",
            "soluong"       => "Số lượng",
            "gia"           => "Giá",
            "mota"          => "Mô tả",
            "ngaynhap"      => "Ngày nhập",
            "vitri"         => "Vị trí",
            "masach"        => "Mã sách",
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
