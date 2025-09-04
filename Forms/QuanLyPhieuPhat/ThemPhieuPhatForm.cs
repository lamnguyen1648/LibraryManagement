using System.Data;
using System.Globalization;
using System.Reflection;
using Microsoft.Data.SqlClient;

namespace LibraryManagement.Forms.QuanLyPhieuPhat
{
    public partial class ThemPhieuPhatForm : Form
    {
        private int? _nvId;      // from UserSession
        private string _nvName;  // display only

        private readonly CultureInfo _vi = new("vi-VN");
        private bool _formattingMoney; // guard re-entrancy for TextChanged

        public ThemPhieuPhatForm()
        {
            InitializeComponent();

            StartPosition = FormStartPosition.CenterParent;
            AcceptButton  = btnLuu;
            CancelButton  = btnHuy;

            // wire once
            btnLuu.Click -= BtnLuu_Click; btnLuu.Click += BtnLuu_Click;
            btnHuy.Click -= BtnHuy_Click; btnHuy.Click += BtnHuy_Click;

            cbPhieuMuon.SelectedIndexChanged -= AnyInputChanged; cbPhieuMuon.SelectedIndexChanged += AnyInputChanged;
            txtLyDo.TextChanged              -= AnyInputChanged; txtLyDo.TextChanged              += AnyInputChanged;

            // MONEY: numbers-only + Vietnamese thousands formatting + min 1.000
            txtSoTien.KeyPress    -= TxtSoTien_KeyPress;    txtSoTien.KeyPress    += TxtSoTien_KeyPress;
            txtSoTien.TextChanged -= TxtSoTien_TextChanged; txtSoTien.TextChanged += TxtSoTien_TextChanged;

            Load += (_, __) =>
            {
                LoadCurrentNhanVienFromSession();
                LoadPhieuMuon();
                UpdateSaveButtonState();
            };
        }

        private void BtnHuy_Click(object? sender, EventArgs e) => DialogResult = DialogResult.Cancel;

        // --- UserSession fetch (safe via reflection to avoid hard dependency) ---
        private void LoadCurrentNhanVienFromSession()
        {
            _nvId = null; _nvName = "";

            try
            {
                var candidateTypes = new[]
                {
                    "LibraryManagement.UserSession",
                    "LibraryManagement.Auth.UserSession",
                    "UserSession"
                };

                foreach (var typeName in candidateTypes)
                {
                    var t = Type.GetType(typeName);
                    if (t == null) continue;

                    // try common property names
                    var pId = t.GetProperty("NV_ID", BindingFlags.Public | BindingFlags.Static)
                              ?? t.GetProperty("NhanVienId", BindingFlags.Public | BindingFlags.Static)
                              ?? t.GetProperty("UserId", BindingFlags.Public | BindingFlags.Static)
                              ?? t.GetProperty("NVId", BindingFlags.Public | BindingFlags.Static);

                    var pName = t.GetProperty("TenNV", BindingFlags.Public | BindingFlags.Static)
                              ?? t.GetProperty("HoTen", BindingFlags.Public | BindingFlags.Static)
                              ?? t.GetProperty("FullName", BindingFlags.Public | BindingFlags.Static)
                              ?? t.GetProperty("Name", BindingFlags.Public | BindingFlags.Static);

                    if (pId == null) continue;

                    var idObj = pId.GetValue(null);
                    if (idObj == null) continue;

                    _nvId = Convert.ToInt32(idObj);
                    _nvName = pName?.GetValue(null)?.ToString() ?? "";
                    break;
                }
            }
            catch { /* ignore; keep null */ }

            // If we got an ID but no name, try to resolve the name from DB.
            if (_nvId.HasValue && string.IsNullOrWhiteSpace(_nvName))
            {
                try
                {
                    using var conn = Db.Create(); conn.Open();

                    // Detect a nice display column.
                    var probe = new DataTable();
                    using (var da = new SqlDataAdapter("select top(0) * from NhanVien", conn)) da.Fill(probe);
                    string displayCol =
                        new[] { "TenNV", "HoTen", "Ten", "HoVaTen" }.FirstOrDefault(probe.Columns.Contains)
                        ?? probe.Columns.Cast<DataColumn>().FirstOrDefault(c => c.DataType == typeof(string))?.ColumnName
                        ?? "NV_ID";

                    using var cmd = new SqlCommand($"select [{displayCol}] from NhanVien where NV_ID=@id", conn);
                    cmd.Parameters.Add(new SqlParameter("@id", SqlDbType.Int) { Value = _nvId.Value });
                    var name = cmd.ExecuteScalar();
                    _nvName = name?.ToString() ?? $"NV {_nvId.Value}";
                }
                catch { /* ignore */ }
            }

            txtNhanVien.Text = _nvId.HasValue
                ? (!string.IsNullOrWhiteSpace(_nvName) ? _nvName : $"NV {_nvId.Value}")
                : "Không xác định";
        }

        private void LoadPhieuMuon()
        {
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

                // No "PM" prefix; show "PM_ID - Tên độc giả"
                string sql = $@"
select pm.PM_ID,
       cast(pm.PM_ID as nvarchar(20)) +
       case when dg.{dgNameCol} is not null then ' - ' + cast(dg.{dgNameCol} as nvarchar(255)) else '' end as TenHienThi
from PhieuMuon pm
left join DocGia dg on dg.DG_ID = pm.DG_ID
order by pm.PM_ID";

                using var da = new SqlDataAdapter(sql, conn);
                var list = new DataTable(); da.Fill(list);

                cbPhieuMuon.DisplayMember = "TenHienThi";
                cbPhieuMuon.ValueMember   = "PM_ID";
                cbPhieuMuon.DataSource    = list;
            }
            catch
            {
                cbPhieuMuon.DataSource = null;
                cbPhieuMuon.Items.Clear();
                MessageBox.Show("Không tải được danh sách phiếu mượn.", "Cảnh báo",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        // ===== Money helpers (numbers-only, vi-VN thousands, min 1.000) =====
        private void TxtSoTien_KeyPress(object? sender, KeyPressEventArgs e)
        {
            // allow digits and control keys (Backspace, Delete, arrows, Ctrl+C/V, etc.)
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar))
                e.Handled = true;
        }

        private void TxtSoTien_TextChanged(object? sender, EventArgs e)
        {
            if (_formattingMoney) return;

            // Keep only digits from pasted text
            var digits = new string(txtSoTien.Text.Where(char.IsDigit).ToArray());

            // Format as vi-VN thousands (no decimals)
            string formatted = string.IsNullOrEmpty(digits)
                ? ""
                : string.Format(_vi, "{0:N0}", long.Parse(digits));

            _formattingMoney = true;
            int selEnd = txtSoTien.SelectionStart;
            txtSoTien.Text = formatted;
            // Put caret at end (simplest reliable UX for formatted inputs)
            txtSoTien.SelectionStart = txtSoTien.Text.Length;
            _formattingMoney = false;

            UpdateSaveButtonState();
        }

        private bool TryGetMoneyValue(out decimal value)
        {
            var digits = new string(txtSoTien.Text.Where(char.IsDigit).ToArray());
            if (string.IsNullOrEmpty(digits))
            {
                value = 0; return false;
            }
            if (!decimal.TryParse(digits, NumberStyles.None, CultureInfo.InvariantCulture, out value))
            {
                value = 0; return false;
            }
            return value >= 0m;
        }

        private void AnyInputChanged(object? sender, EventArgs e) => UpdateSaveButtonState();

        private void UpdateSaveButtonState()
        {
            bool ok = _nvId.HasValue
                      && cbPhieuMuon.SelectedValue != null
                      && TryGetMoneyValue(out _);
            btnLuu.Enabled = ok;
        }

        private void BtnLuu_Click(object? sender, EventArgs e)
        {
            if (!btnLuu.Enabled) return;

            if (!TryGetMoneyValue(out var money))
            {
                MessageBox.Show("Số tiền phạt tối thiểu là 1.000 VND.", "Giá trị không hợp lệ");
                txtSoTien.Focus(); return;
            }

            try
            {
                using var conn = Db.Create(); conn.Open();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = @"
insert into PhieuPhat (NV_ID, PM_ID, LyDo, SoTienPhat)
values (@nv, @pm, @lydo, @tien);";
                cmd.Parameters.Add(new SqlParameter("@nv",   SqlDbType.Int)         { Value = _nvId!.Value });
                cmd.Parameters.Add(new SqlParameter("@pm",   SqlDbType.Int)         { Value = (int)cbPhieuMuon.SelectedValue });
                cmd.Parameters.Add(new SqlParameter("@lydo", SqlDbType.NVarChar, -1){ Value = (object?)txtLyDo.Text?.Trim() ?? DBNull.Value });
                cmd.Parameters.Add(new SqlParameter("@tien", SqlDbType.Decimal)     { Value = money });

                cmd.ExecuteNonQuery();
                DialogResult = DialogResult.OK;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lưu thất bại:\r\n" + ex.Message, "Lỗi",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
