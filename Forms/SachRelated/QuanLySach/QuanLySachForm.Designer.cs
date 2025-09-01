using System.Drawing;
using System.Windows.Forms;

namespace LibraryManagement.Forms.SachRelated.QuanLySach
{
    public partial class QuanLySachForm
    {
        private TableLayoutPanel root;
        private Label lblTitle;

        private TableLayoutPanel toolbar;
        private Button btnQuanLyNXB;
        private Button btnQuanLyTheLoai;
        private Button btnQuanLyTacGia;
        private Button btnLichSuCapNhat; // NEW
        private Button btnThemSach;
        private Button btnXoaNhieu;
        private Button btnTimKiem;
        private TextBox txtSearch;

        private Panel contentPanel;
        private DataGridView dgvSach;
        private Label lblEmpty;

        private void InitializeComponent()
        {
            root = new TableLayoutPanel();
            lblTitle = new Label();

            toolbar = new TableLayoutPanel();
            btnQuanLyNXB = new Button();
            btnQuanLyTheLoai = new Button();
            btnQuanLyTacGia = new Button();
            btnLichSuCapNhat = new Button();
            btnThemSach = new Button();
            btnXoaNhieu = new Button();
            btnTimKiem = new Button();
            txtSearch = new TextBox();

            contentPanel = new Panel();
            dgvSach = new DataGridView();
            lblEmpty = new Label();

            SuspendLayout();

            AutoScaleMode = AutoScaleMode.Dpi;
            Text = "Quản lý sách";
            MinimumSize = new Size(1000, 600);
            StartPosition = FormStartPosition.CenterParent;

            // ===== Root layout
            root.Dock = DockStyle.Fill;
            root.Padding = new Padding(12);
            root.ColumnCount = 1;
            root.RowCount = 3;
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 12F)); // header
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 12F)); // toolbar (taller)
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 76F)); // content
            Controls.Add(root);

            // ===== Header
            lblTitle.Text = "Quản lý sách";
            lblTitle.Dock = DockStyle.Fill;
            lblTitle.TextAlign = ContentAlignment.TopLeft;
            lblTitle.Font = new Font("Segoe UI", 16F, FontStyle.Bold, GraphicsUnit.Point);
            lblTitle.Padding = new Padding(8, 6, 8, 6);
            root.Controls.Add(lblTitle, 0, 0);

            // ===== Toolbar layout
            toolbar.Dock = DockStyle.Fill;
            toolbar.Padding = new Padding(0, 4, 0, 4); // small vertical padding for breathing room
            toolbar.ColumnCount = 8;
            toolbar.RowCount = 1;
            toolbar.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            // [NXB 11][Thể loại 11][Tác giả 11][Lịch sử 11][Thêm 12][Xóa nhiều 12][Tìm 10][Search 22]
            toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 11F));
            toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 11F));
            toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 11F));
            toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 11F));
            toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 12F));
            toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 12F));
            toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 10F));
            toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 22F));
            root.Controls.Add(toolbar, 0, 1);

            var btnFont = new Font("Segoe UI", 10.5F, FontStyle.Bold, GraphicsUnit.Point);
            var btnPad  = new Padding(2, 2, 2, 2); // symmetric padding; height comes from taller row

            btnQuanLyNXB.Text = "Quản lý NXB";
            btnQuanLyNXB.Dock = DockStyle.Fill;
            btnQuanLyNXB.Font = btnFont;
            btnQuanLyNXB.Margin = btnPad;
            btnQuanLyNXB.TextAlign = ContentAlignment.MiddleCenter;

            btnQuanLyTheLoai.Text = "Quản lý thể loại";
            btnQuanLyTheLoai.Dock = DockStyle.Fill;
            btnQuanLyTheLoai.Font = btnFont;
            btnQuanLyTheLoai.Margin = btnPad;
            btnQuanLyTheLoai.TextAlign = ContentAlignment.MiddleCenter;

            btnQuanLyTacGia.Text = "Quản lý tác giả";
            btnQuanLyTacGia.Dock = DockStyle.Fill;
            btnQuanLyTacGia.Font = btnFont;
            btnQuanLyTacGia.Margin = btnPad;
            btnQuanLyTacGia.TextAlign = ContentAlignment.MiddleCenter;

            btnLichSuCapNhat.Text = "Lịch sử cập nhật";
            btnLichSuCapNhat.Dock = DockStyle.Fill;
            btnLichSuCapNhat.Font = btnFont;
            btnLichSuCapNhat.Margin = btnPad;
            btnLichSuCapNhat.TextAlign = ContentAlignment.MiddleCenter;

            btnThemSach.Text = "Thêm sách";
            btnThemSach.Dock = DockStyle.Fill;
            btnThemSach.Font = btnFont;
            btnThemSach.Margin = btnPad;
            btnThemSach.TextAlign = ContentAlignment.MiddleCenter;

            btnXoaNhieu.Text = "Xóa nhiều sách";
            btnXoaNhieu.Dock = DockStyle.Fill;
            btnXoaNhieu.Font = btnFont;
            btnXoaNhieu.Margin = btnPad;
            btnXoaNhieu.Enabled = false;
            btnXoaNhieu.TextAlign = ContentAlignment.MiddleCenter;

            btnTimKiem.Text = "Tìm kiếm";
            btnTimKiem.Dock = DockStyle.Fill;
            btnTimKiem.Font = btnFont;
            btnTimKiem.Margin = btnPad;
            btnTimKiem.TextAlign = ContentAlignment.MiddleCenter;

            txtSearch.Dock = DockStyle.Fill;
            txtSearch.Margin = new Padding(6, 2, 0, 2);
            txtSearch.PlaceholderText = "Nhập tên sách";
            txtSearch.Font = new Font("Segoe UI", 10.5F, FontStyle.Regular, GraphicsUnit.Point);

            toolbar.Controls.Add(btnQuanLyNXB,     0, 0);
            toolbar.Controls.Add(btnQuanLyTheLoai, 1, 0);
            toolbar.Controls.Add(btnQuanLyTacGia,  2, 0);
            toolbar.Controls.Add(btnLichSuCapNhat, 3, 0);
            toolbar.Controls.Add(btnThemSach,      4, 0);
            toolbar.Controls.Add(btnXoaNhieu,      5, 0);
            toolbar.Controls.Add(btnTimKiem,       6, 0);
            toolbar.Controls.Add(txtSearch,        7, 0);

            // ===== Content area
            contentPanel.Dock = DockStyle.Fill;
            root.Controls.Add(contentPanel, 0, 2);

            dgvSach.Dock = DockStyle.Fill;
            dgvSach.AllowUserToAddRows = false;
            dgvSach.AllowUserToDeleteRows = false;
            dgvSach.ReadOnly = false; // checkboxes editable
            dgvSach.MultiSelect = false;
            dgvSach.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvSach.RowHeadersVisible = false;
            dgvSach.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

            lblEmpty.Text = "Không tìm thấy sách";
            lblEmpty.Dock = DockStyle.Fill;
            lblEmpty.TextAlign = ContentAlignment.MiddleCenter;
            lblEmpty.Font = new Font("Segoe UI", 12F, FontStyle.Italic, GraphicsUnit.Point);
            lblEmpty.Visible = false;

            contentPanel.Controls.Add(dgvSach);
            contentPanel.Controls.Add(lblEmpty);

            ResumeLayout(false);
        }
    }
}
