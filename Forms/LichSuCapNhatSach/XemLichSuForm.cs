// XemChiTietForm.cs

using System.Data;

namespace LibraryManagement.Forms.LichSuCapNhatSach
{
    public partial class XemChiTietForm : Form
    {
        private readonly DataRow _row;

        public XemChiTietForm(DataRow row)
        {
            _row = row ?? throw new ArgumentNullException(nameof(row));
            InitializeComponent();

            StartPosition = FormStartPosition.CenterParent;

            Load += (_, __) => BindRow();
        }

        private void BindRow()
        {
            string Get(string col)
            {
                try
                {
                    if (!_row.Table.Columns.Contains(col)) return "";
                    var v = _row[col];
                    return v == DBNull.Value ? "" : v.ToString();
                }
                catch { return ""; }
            }

            lblNhanVien.Text = Get("NhanVien");
            lblTenSach.Text  = Get("TenSach");

            var ngay = Get("NgayCapNhat");
            if (DateTime.TryParse(ngay, out var dt))
                lblNgayCapNhat.Text = dt.ToString("dd/MM/yyyy HH:mm:ss");
            else
                lblNgayCapNhat.Text = ngay;

            lblHinhThuc.Text = Get("HinhThucCapNhat");

            lblChiTiet.MaximumSize = new Size(0, 0);
            lblChiTiet.Text        = Get("ChiTietCapNhat");
        }
    }
}