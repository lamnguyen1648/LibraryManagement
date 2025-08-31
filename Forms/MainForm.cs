using System.Windows.Forms;
using LibraryManagement.Forms.QuanLySach;

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

            // Prevent duplicate subscriptions
            btnQuanLySach.Click -= btnQuanLySach_Click;
            btnQuanLySach.Click += btnQuanLySach_Click;

            btnQuanLyNhanVien.Click -= btnQuanLyNhanVien_Click;
            btnQuanLyNhanVien.Click += btnQuanLyNhanVien_Click;

            btnQuanLyPhieuMuon.Click -= btnQuanLyPhieuMuon_Click;
            btnQuanLyPhieuMuon.Click += btnQuanLyPhieuMuon_Click;

            btnQuanLyDocGia.Click -= btnQuanLyDocGia_Click;
            btnQuanLyDocGia.Click += btnQuanLyDocGia_Click;
        }

        private void btnQuanLySach_Click(object? sender, System.EventArgs e)
        {
            // Guard against double-clicks opening multiple forms
            btnQuanLySach.Enabled = false;
            try
            {
                Hide();
                using (var f = new QuanLySachForm())
                {
                    f.StartPosition = FormStartPosition.CenterParent;
                    f.ShowDialog(this); // modal; returns when closed
                }
                Show();
            }
            finally
            {
                btnQuanLySach.Enabled = true;
            }
        }

        private void btnQuanLyNhanVien_Click(object? s, System.EventArgs e) =>
            MessageBox.Show("TODO: Mở form Quản lý nhân viên", "Placeholder");

        private void btnQuanLyPhieuMuon_Click(object? s, System.EventArgs e) =>
            MessageBox.Show("TODO: Mở form Quản lý phiếu mượn", "Placeholder");

        private void btnQuanLyDocGia_Click(object? s, System.EventArgs e) =>
            MessageBox.Show("TODO: Mở form Quản lý độc giả", "Placeholder");
    }
}
