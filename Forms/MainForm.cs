using LibraryManagement.Forms.QuanLyDocGia;
using LibraryManagement.Forms.QuanLyNhanVien;
using LibraryManagement.Forms.QuanLyPhieuMuon;
using LibraryManagement.Forms.QuanLySach;

// 👇 Add this using to reach the new form

namespace LibraryManagement.Forms
{
    public partial class MainForm : Form
    {
        private readonly string _username;

        public MainForm(string username)
        {
            _username = username;
            InitializeComponent();

            lblWelcome.Text = $"Xin chào, {_username}!\r\nChúc một ngày làm việc hiệu quả.";

            // De-dupe and wire clicks
            btnQuanLySach.Click      -= btnQuanLySach_Click;
            btnQuanLySach.Click      += btnQuanLySach_Click;

            btnQuanLyNhanVien.Click  -= btnQuanLyNhanVien_Click;
            btnQuanLyNhanVien.Click  += btnQuanLyNhanVien_Click;

            btnQuanLyPhieuMuon.Click -= btnQuanLyPhieuMuon_Click;
            btnQuanLyPhieuMuon.Click += btnQuanLyPhieuMuon_Click;

            btnQuanLyDocGia.Click    -= btnQuanLyDocGia_Click;
            btnQuanLyDocGia.Click    += btnQuanLyDocGia_Click;
        }

        private void btnQuanLySach_Click(object? sender, EventArgs e)
        {
            btnQuanLySach.Enabled = false;
            try
            {
                Hide();
                using (var f = new QuanLySachForm())
                {
                    f.StartPosition = FormStartPosition.CenterParent;
                    f.ShowDialog(this);
                }
                Show();
                Activate();
            }
            finally
            {
                btnQuanLySach.Enabled = true;
            }
        }

        private void btnQuanLyNhanVien_Click(object? sender, EventArgs e)
        {
            btnQuanLyNhanVien.Enabled = false;
            try
            {
                Hide();
                using (var f = new QuanLyNhanVienForm())
                {
                    f.StartPosition = FormStartPosition.CenterParent;
                    f.ShowDialog(this);
                }
                Show();
                Activate();
            }
            finally
            {
                btnQuanLyNhanVien.Enabled = true;
            }
        }

        // ✅ Now opens QuanLyPhieuMuonForm
        private void btnQuanLyPhieuMuon_Click(object? sender, EventArgs e)
        {
            btnQuanLyPhieuMuon.Enabled = false;
            try
            {
                Hide();
                using (var f = new QuanLyPhieuMuonForm())
                {
                    f.StartPosition = FormStartPosition.CenterParent;
                    f.ShowDialog(this);
                }
                Show();
                Activate();
            }
            finally
            {
                btnQuanLyPhieuMuon.Enabled = true;
            }
        }

        private void btnQuanLyDocGia_Click(object? sender, EventArgs e)
        {
            btnQuanLyDocGia.Enabled = false;
            try
            {
                Hide();
                using (var f = new QuanLyDocGiaForm())
                {
                    f.StartPosition = FormStartPosition.CenterParent;
                    f.ShowDialog(this);
                }
                Show();
                Activate();
            }
            finally
            {
                btnQuanLyDocGia.Enabled = true;
            }
        }
    }
}
