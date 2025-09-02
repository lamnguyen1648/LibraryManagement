using System.Data;
using Microsoft.Data.SqlClient;

namespace LibraryManagement.Forms.PhieuMuonRelated.QuanLyPhieuMuon
{
    public partial class SuaPhieuMuonForm : Form
    {
        private readonly int _pmId;

        public SuaPhieuMuonForm(int pmId)
        {
            _pmId = pmId;
            InitializeComponent();

            StartPosition = FormStartPosition.CenterParent;
            AcceptButton  = btnLuu;
            CancelButton  = btnHuy;

            btnLuu.Click -= BtnLuu_Click; btnLuu.Click += BtnLuu_Click;
            btnHuy.Click -= BtnHuy_Click; btnHuy.Click += BtnHuy_Click;

            Load += (_, __) =>
            {
                LoadDocGia();
                LoadTinhTrangOptions();
                LoadSachList();
                LoadPhieuMuon();
            };
        }

        private void BtnHuy_Click(object? sender, EventArgs e) => DialogResult = DialogResult.Cancel;

        private void LoadTinhTrangOptions()
        {
            cbTinhTrang.Items.Clear();
            cbTinhTrang.Items.AddRange(new object[]
            {
                "Đang mượn",
                "Đang nợ/thanh toán",
                "Quá hạn",
                "Đã trả"
            });
        }

        private void LoadDocGia()
        {
            try
            {
                using var conn = Db.Create(); conn.Open();

                var peek = new DataTable();
                using (var da = new SqlDataAdapter("select top(0) * from DocGia", conn)) da.Fill(peek);

                string displayCol =
                    new[] { "TenDocGia", "HoTen", "Ten", "HoVaTen", "TenDG", "Ten_DocGia" }
                    .FirstOrDefault(c => peek.Columns.Contains(c))
                    ?? peek.Columns.Cast<DataColumn>().FirstOrDefault(c => c.DataType == typeof(string))?.ColumnName
                    ?? "DG_ID";

                string sql = $"select DG_ID, [{displayCol}] as TenHienThi from DocGia order by TenHienThi";
                using var da2 = new SqlDataAdapter(sql, conn);
                var list = new DataTable(); da2.Fill(list);

                cbDocGia.DisplayMember = "TenHienThi";
                cbDocGia.ValueMember   = "DG_ID";
                cbDocGia.DataSource    = list;
            }
            catch
            {
                cbDocGia.DataSource = null;
                cbDocGia.Items.Clear();
                MessageBox.Show("Không tải được danh sách độc giả. Hãy kiểm tra bảng DocGia.", "Cảnh báo",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void LoadSachList()
        {
            try
            {
                using var conn = Db.Create(); conn.Open();

                var peek = new DataTable();
                using (var da = new SqlDataAdapter("select top(0) * from Sach", conn)) da.Fill(peek);

                string displayCol =
                    new[] { "TenSach", "Ten", "TieuDe", "NhanDe" }
                    .FirstOrDefault(c => peek.Columns.Contains(c))
                    ?? peek.Columns.Cast<DataColumn>().FirstOrDefault(c => c.DataType == typeof(string))?.ColumnName
                    ?? "Sach_ID";

                string sql = $"select Sach_ID, [{displayCol}] as TenHienThi from Sach order by TenHienThi";
                using var da2 = new SqlDataAdapter(sql, conn);
                var list = new DataTable(); da2.Fill(list);

                clbSach.DataSource    = list;
                clbSach.DisplayMember = "TenHienThi";
                clbSach.ValueMember   = "Sach_ID";
            }
            catch
            {
                clbSach.DataSource = null;
                clbSach.Items.Clear();
                MessageBox.Show("Không tải được danh sách sách. Hãy kiểm tra bảng Sach.", "Cảnh báo",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void LoadPhieuMuon()
        {
            try
            {
                using var conn = Db.Create(); conn.Open();

                // Load main row
                using var da = new SqlDataAdapter("select * from PhieuMuon where PM_ID=@id", conn);
                da.SelectCommand!.Parameters.Add(new SqlParameter("@id", SqlDbType.Int) { Value = _pmId });
                var dt = new DataTable(); da.Fill(dt);
                if (dt.Rows.Count == 0)
                {
                    MessageBox.Show("Không tìm thấy phiếu mượn.", "Lỗi");
                    DialogResult = DialogResult.Cancel; return;
                }
                var r = dt.Rows[0];

                if (cbDocGia.DataSource != null)
                    cbDocGia.SelectedValue = Convert.ToInt32(r["DG_ID"]);

                dtpNgayMuon.Value = r["NgayMuon"] == DBNull.Value ? DateTime.Today : Convert.ToDateTime(r["NgayMuon"]);

                if (r["NgayTra"] == DBNull.Value)
                {
                    dtpNgayTra.Value = DateTime.Today;
                    dtpNgayTra.Checked = false;
                }
                else
                {
                    dtpNgayTra.Value = Convert.ToDateTime(r["NgayTra"]);
                    dtpNgayTra.Checked = true;
                }

                var tt = r["TinhTrang"] == DBNull.Value ? "" : r["TinhTrang"].ToString();
                if (!string.IsNullOrWhiteSpace(tt))
                {
                    if (!cbTinhTrang.Items.Cast<object>().Any(x => string.Equals(x.ToString(), tt, StringComparison.OrdinalIgnoreCase)))
                        cbTinhTrang.Items.Add(tt);
                    cbTinhTrang.SelectedItem = tt;
                }
                else if (cbTinhTrang.Items.Count > 0) cbTinhTrang.SelectedIndex = 0;

                // Load existing book links
                var existed = new HashSet<int>();
                using (var daC = new SqlDataAdapter("select Sach_ID from ChiTietPhieuMuon where PM_ID=@id", conn))
                {
                    daC.SelectCommand!.Parameters.Add(new SqlParameter("@id", SqlDbType.Int) { Value = _pmId });
                    var dtC = new DataTable(); daC.Fill(dtC);
                    foreach (DataRow rr in dtC.Rows) existed.Add(Convert.ToInt32(rr["Sach_ID"]));
                }

                for (int i = 0; i < clbSach.Items.Count; i++)
                {
                    if (clbSach.Items[i] is DataRowView drv && drv.Row.Table.Columns.Contains("Sach_ID"))
                    {
                        var sid = Convert.ToInt32(drv["Sach_ID"]);
                        clbSach.SetItemChecked(i, existed.Contains(sid));
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Không tải được dữ liệu:\r\n" + ex.Message, "Lỗi",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                DialogResult = DialogResult.Cancel;
            }
        }

        private List<int> GetCheckedSachIds()
        {
            var ids = new List<int>();
            for (int i = 0; i < clbSach.Items.Count; i++)
            {
                if (!clbSach.GetItemChecked(i)) continue;
                if (clbSach.Items[i] is DataRowView drv && drv.Row.Table.Columns.Contains("Sach_ID"))
                {
                    ids.Add(Convert.ToInt32(drv["Sach_ID"]));
                }
            }
            return ids;
        }

        private void BtnLuu_Click(object? sender, EventArgs e)
        {
            if (cbDocGia.SelectedValue == null)
            {
                MessageBox.Show("Vui lòng chọn độc giả.", "Thiếu thông tin",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                cbDocGia.DroppedDown = true; return;
            }
            if (dtpNgayTra.Checked && dtpNgayTra.Value.Date < dtpNgayMuon.Value.Date)
            {
                MessageBox.Show("Ngày trả không được nhỏ hơn ngày mượn.", "Giá trị không hợp lệ",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            var sachIds = GetCheckedSachIds();
            if (sachIds.Count == 0)
            {
                MessageBox.Show("Vui lòng chọn ít nhất một sách.", "Thiếu thông tin",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            using var conn = Db.Create(); conn.Open();
            using var tx = conn.BeginTransaction();

            try
            {
                // Update PhieuMuon
                using (var cmd = conn.CreateCommand())
                {
                    cmd.Transaction = tx;
                    cmd.CommandText = @"
update PhieuMuon
   set DG_ID     = @dg,
       NgayMuon  = @ngaymuon,
       NgayTra   = @ngaytra,
       TinhTrang = @tinhtrang
 where PM_ID     = @id;";
                    cmd.Parameters.Add(new SqlParameter("@dg", SqlDbType.Int)           { Value = (int)cbDocGia.SelectedValue });
                    cmd.Parameters.Add(new SqlParameter("@ngaymuon", SqlDbType.Date)    { Value = dtpNgayMuon.Value.Date });
                    cmd.Parameters.Add(new SqlParameter("@ngaytra",  SqlDbType.Date)    { Value = dtpNgayTra.Checked ? dtpNgayTra.Value.Date : DBNull.Value });
                    cmd.Parameters.Add(new SqlParameter("@tinhtrang",SqlDbType.NVarChar,30) { Value = cbTinhTrang.SelectedItem?.ToString() ?? (object)DBNull.Value });
                    cmd.Parameters.Add(new SqlParameter("@id", SqlDbType.Int)           { Value = _pmId });
                    cmd.ExecuteNonQuery();
                }

                // Resync ChiTietPhieuMuon
                using (var del = conn.CreateCommand())
                {
                    del.Transaction = tx;
                    del.CommandText = "delete from ChiTietPhieuMuon where PM_ID = @id";
                    del.Parameters.Add(new SqlParameter("@id", SqlDbType.Int) { Value = _pmId });
                    del.ExecuteNonQuery();
                }

                using (var ins = conn.CreateCommand())
                {
                    ins.Transaction = tx;
                    ins.CommandText = "insert into ChiTietPhieuMuon (PM_ID, Sach_ID) values (@pm, @sach)";
                    var pPm   = ins.Parameters.Add("@pm",   SqlDbType.Int);
                    var pSach = ins.Parameters.Add("@sach", SqlDbType.Int);
                    foreach (var sid in sachIds.Distinct())
                    {
                        pPm.Value = _pmId;
                        pSach.Value = sid;
                        ins.ExecuteNonQuery();
                    }
                }

                tx.Commit();
                DialogResult = DialogResult.OK;
            }
            catch (Exception ex)
            {
                try { tx.Rollback(); } catch { /* ignore */ }
                MessageBox.Show("Cập nhật thất bại:\r\n" + ex.Message, "Lỗi",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
