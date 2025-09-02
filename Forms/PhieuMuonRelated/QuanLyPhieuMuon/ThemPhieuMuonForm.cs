using System.ComponentModel;
using System.Data;
using Microsoft.Data.SqlClient;

namespace LibraryManagement.Forms.PhieuMuonRelated.QuanLyPhieuMuon
{
    public partial class ThemPhieuMuonForm : Form
    {
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public int? InsertedId { get; private set; }

        public ThemPhieuMuonForm()
        {
            InitializeComponent();

            StartPosition = FormStartPosition.CenterParent;
            AcceptButton  = btnLuu;
            CancelButton  = btnHuy;

            // events (de-dupe)
            btnLuu.Click -= BtnLuu_Click; btnLuu.Click += BtnLuu_Click;
            btnHuy.Click -= BtnHuy_Click; btnHuy.Click += BtnHuy_Click;

            Load += (_, __) =>
            {
                LoadDocGia();
                LoadTinhTrangOptions();
                LoadSachList();                // NEW: load books (multi-select)
                dtpNgayMuon.Value = DateTime.Today;
                dtpNgayTra.Checked = false;    // null by default
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
            cbTinhTrang.SelectedIndex = 0;
        }

        private void LoadDocGia()
        {
            try
            {
                using var conn = Db.Create(); conn.Open();

                // pick a nice display column from DocGia
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

                // pick a display column from Sach
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
            // required: DG_ID, NgayMuon, at least one Sách
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
                // Insert PhieuMuon
                using var cmd = conn.CreateCommand();
                cmd.Transaction = tx;
                cmd.CommandText = @"
insert into PhieuMuon (DG_ID, NgayMuon, NgayTra, TinhTrang)
values (@dg, @ngaymuon, @ngaytra, @tinhtrang);
select cast(scope_identity() as int);";

                cmd.Parameters.Add(new SqlParameter("@dg", SqlDbType.Int)           { Value = (int)cbDocGia.SelectedValue });
                cmd.Parameters.Add(new SqlParameter("@ngaymuon", SqlDbType.Date)    { Value = dtpNgayMuon.Value.Date });
                cmd.Parameters.Add(new SqlParameter("@ngaytra",  SqlDbType.Date)    { Value = dtpNgayTra.Checked ? dtpNgayTra.Value.Date : DBNull.Value });
                cmd.Parameters.Add(new SqlParameter("@tinhtrang",SqlDbType.NVarChar,30) { Value = cbTinhTrang.SelectedItem?.ToString() ?? (object)DBNull.Value });

                InsertedId = (int?)cmd.ExecuteScalar();

                // Insert ChiTietPhieuMuon (many books)
                if (InsertedId.HasValue)
                {
                    using var ins = conn.CreateCommand();
                    ins.Transaction = tx;
                    ins.CommandText = "insert into ChiTietPhieuMuon (PM_ID, Sach_ID) values (@pm, @sach)";
                    var pPm   = ins.Parameters.Add("@pm",   SqlDbType.Int);
                    var pSach = ins.Parameters.Add("@sach", SqlDbType.Int);

                    foreach (var sid in sachIds.Distinct())
                    {
                        pPm.Value = InsertedId.Value;
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
                MessageBox.Show("Lưu thất bại:\r\n" + ex.Message, "Lỗi",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
