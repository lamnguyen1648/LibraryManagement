using System;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;
using Microsoft.Data.SqlClient;

namespace LibraryManagement.Forms.QuanLyPhieuPhat
{
    public partial class SuaPhieuPhatForm : Form
    {
        private readonly int _ppId;
        private int _nvIdFromRow;

        private readonly CultureInfo _vi = new("vi-VN");
        private bool _formattingMoney;

        public SuaPhieuPhatForm(int ppId)
        {
            _ppId = ppId;
            InitializeComponent();

            StartPosition = FormStartPosition.CenterParent;
            AcceptButton  = btnLuu;
            CancelButton  = btnHuy;

            btnLuu.Click -= BtnLuu_Click; btnLuu.Click += BtnLuu_Click;
            btnHuy.Click -= BtnHuy_Click; btnHuy.Click += BtnHuy_Click;

            cbPhieuMuon.SelectedIndexChanged -= AnyInputChanged; cbPhieuMuon.SelectedIndexChanged += AnyInputChanged;
            txtLyDo.TextChanged              -= AnyInputChanged; txtLyDo.TextChanged              += AnyInputChanged;

            // MONEY rules
            txtSoTien.KeyPress    -= TxtSoTien_KeyPress;    txtSoTien.KeyPress    += TxtSoTien_KeyPress;
            txtSoTien.TextChanged -= TxtSoTien_TextChanged; txtSoTien.TextChanged += TxtSoTien_TextChanged;

            Load += (_, __) =>
            {
                LoadPhieuMuon();
                LoadRow();
                UpdateSaveButtonState();
            };
        }

        private void BtnHuy_Click(object? sender, EventArgs e) => DialogResult = DialogResult.Cancel;

        private void LoadPhieuMuon()
        {
            try
            {
                using var conn = Db.Create(); conn.Open();

                var probeDG = new DataTable();
                using (var da0 = new SqlDataAdapter("select top(0) * from DocGia", conn)) da0.Fill(probeDG);
                string dgNameCol =
                    new[] { "TenDocGia", "HoTen", "Ten", "TenDG", "HoVaTen" }
                    .FirstOrDefault(probeDG.Columns.Contains)
                    ?? probeDG.Columns.Cast<DataColumn>().FirstOrDefault(c => c.DataType == typeof(string))?.ColumnName
                    ?? "DG_ID";

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
            }
        }

        private void LoadRow()
        {
            try
            {
                using var conn = Db.Create(); conn.Open();
                using var da = new SqlDataAdapter("select * from PhieuPhat where PP_ID=@id", conn);
                da.SelectCommand!.Parameters.Add(new SqlParameter("@id", SqlDbType.Int) { Value = _ppId });
                var dt = new DataTable(); da.Fill(dt);
                if (dt.Rows.Count == 0)
                {
                    MessageBox.Show("Không tìm thấy phiếu phạt.", "Lỗi");
                    DialogResult = DialogResult.Cancel; return;
                }
                var r = dt.Rows[0];

                _nvIdFromRow = Convert.ToInt32(r["NV_ID"]);
                txtNhanVien.Text = ResolveNhanVienName(_nvIdFromRow);

                if (cbPhieuMuon.DataSource != null)
                    cbPhieuMuon.SelectedValue = Convert.ToInt32(r["PM_ID"]);

                // format money as vi-VN thousands
                if (r["SoTienPhat"] != DBNull.Value)
                {
                    var money = Convert.ToDecimal(r["SoTienPhat"]);
                    _formattingMoney = true;
                    txtSoTien.Text = string.Format(_vi, "{0:N0}", money);
                    _formattingMoney = false;
                }
                else txtSoTien.Text = "";

                txtLyDo.Text = r["LyDo"] == DBNull.Value ? "" : r["LyDo"].ToString();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Không tải được dữ liệu:\r\n" + ex.Message, "Lỗi",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                DialogResult = DialogResult.Cancel;
            }
        }

        private string ResolveNhanVienName(int nvId)
        {
            try
            {
                using var conn = Db.Create(); conn.Open();
                var probe = new DataTable();
                using (var da = new SqlDataAdapter("select top(0) * from NhanVien", conn)) da.Fill(probe);
                string displayCol =
                    new[] { "TenNV", "HoTen", "Ten", "HoVaTen" }.FirstOrDefault(probe.Columns.Contains)
                    ?? probe.Columns.Cast<DataColumn>().FirstOrDefault(c => c.DataType == typeof(string))?.ColumnName
                    ?? "NV_ID";

                using var cmd = new SqlCommand($"select [{displayCol}] from NhanVien where NV_ID=@id", conn);
                cmd.Parameters.Add(new SqlParameter("@id", SqlDbType.Int) { Value = nvId });
                var name = cmd.ExecuteScalar();
                return name?.ToString() ?? $"NV {nvId}";
            }
            catch { return $"NV {nvId}"; }
        }

        // ===== Money helpers =====
        private void TxtSoTien_KeyPress(object? sender, KeyPressEventArgs e)
        {
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar))
                e.Handled = true;
        }

        private void TxtSoTien_TextChanged(object? sender, EventArgs e)
        {
            if (_formattingMoney) return;

            var digits = new string(txtSoTien.Text.Where(char.IsDigit).ToArray());
            string formatted = string.IsNullOrEmpty(digits)
                ? ""
                : string.Format(_vi, "{0:N0}", long.Parse(digits));

            _formattingMoney = true;
            txtSoTien.Text = formatted;
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
            return value >= 1000m;
        }

        private void AnyInputChanged(object? sender, EventArgs e) => UpdateSaveButtonState();

        private void UpdateSaveButtonState()
        {
            bool ok = _nvIdFromRow > 0
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
update PhieuPhat
   set NV_ID=@nv, PM_ID=@pm, LyDo=@lydo, SoTienPhat=@tien
 where PP_ID=@id;";
                cmd.Parameters.Add(new SqlParameter("@nv",   SqlDbType.Int)         { Value = _nvIdFromRow });
                cmd.Parameters.Add(new SqlParameter("@pm",   SqlDbType.Int)         { Value = (int)cbPhieuMuon.SelectedValue });
                cmd.Parameters.Add(new SqlParameter("@lydo", SqlDbType.NVarChar, -1){ Value = (object?)txtLyDo.Text?.Trim() ?? DBNull.Value });
                cmd.Parameters.Add(new SqlParameter("@tien", SqlDbType.Decimal)     { Value = money });
                cmd.Parameters.Add(new SqlParameter("@id",   SqlDbType.Int)         { Value = _ppId });

                cmd.ExecuteNonQuery();
                DialogResult = DialogResult.OK;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Cập nhật thất bại:\r\n" + ex.Message, "Lỗi",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
