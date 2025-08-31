using System.Drawing;
using System.Windows.Forms;

namespace LibraryManagement.Forms.QuanLySach
{
    partial class QuanLySachForm
    {
        private TableLayoutPanel root;         // header, toolbar, content
        private Label lblTitle;

        private TableLayoutPanel toolbar;      // 1 row, 7 cols (6 buttons + search)
        private Button btnQuanLyNXB;
        private Button btnQuanLyTheLoai;
        private Button btnQuanLyTacGia;
        private Button btnThemSach;
        private Button btnXoaNhieu;
        private Button btnTimKiem;
        private TextBox txtSearch;

        private Panel contentPanel;
        private DataGridView dgvBooks;
        private Label lblEmpty;

        private void InitializeComponent()
        {
            root = new TableLayoutPanel();
            lblTitle = new Label();

            toolbar = new TableLayoutPanel();
            btnQuanLyNXB = new Button();
            btnQuanLyTheLoai = new Button();
            btnQuanLyTacGia = new Button();
            btnThemSach = new Button();
            btnXoaNhieu = new Button();
            btnTimKiem = new Button();
            txtSearch = new TextBox();

            contentPanel = new Panel();
            dgvBooks = new DataGridView();
            lblEmpty = new Label();

            SuspendLayout();

            // === Form ===
            AutoScaleMode = AutoScaleMode.Dpi;
            Text = "Quản lý sách";
            MinimumSize = new Size(1100, 640);
            StartPosition = FormStartPosition.CenterScreen;

            // === Root (header / toolbar / content) ===
            root.Dock = DockStyle.Fill;
            root.Padding = new Padding(12);
            root.ColumnCount = 1;
            root.RowCount = 3;
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 12F)); // header
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 10F)); // toolbar (slim height)
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 78F)); // content
            Controls.Add(root);

            // Header label (top-left)
            lblTitle.Text = "Quản lý sách";
            lblTitle.Dock = DockStyle.Fill;
            lblTitle.TextAlign = ContentAlignment.TopLeft;
            lblTitle.Font = new Font("Segoe UI", 16F, FontStyle.Bold, GraphicsUnit.Point);
            lblTitle.Padding = new Padding(8, 6, 8, 6);
            root.Controls.Add(lblTitle, 0, 0);

            // === Toolbar: 6 wider buttons + search ===
            toolbar.Dock = DockStyle.Fill;
            toolbar.Padding = new Padding(0, 2, 0, 2); // slim vertical padding
            toolbar.ColumnCount = 7;
            toolbar.RowCount = 1;
            toolbar.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            // Wider buttons: 6 x 12% = 72%; Search = 28%
            toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 12F)); // NXB
            toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 12F)); // TL
            toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 12F)); // TG
            toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 12F)); // Thêm
            toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 12F)); // Xóa nhiều
            toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 12F)); // Tìm
            toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 28F)); // Search

            var btnFont = new Font("Segoe UI", 10.5F, FontStyle.Bold, GraphicsUnit.Point);
            Padding btnPad = new Padding(2, 1, 2, 1);

            btnQuanLyNXB.Text = "Quản lý NXB";
            btnQuanLyNXB.Dock = DockStyle.Fill;
            btnQuanLyNXB.Font = btnFont;
            btnQuanLyNXB.Margin = btnPad;

            btnQuanLyTheLoai.Text = "Quản lý thể loại";
            btnQuanLyTheLoai.Dock = DockStyle.Fill;
            btnQuanLyTheLoai.Font = btnFont;
            btnQuanLyTheLoai.Margin = btnPad;

            btnQuanLyTacGia.Text = "Quản lý tác giả";
            btnQuanLyTacGia.Dock = DockStyle.Fill;
            btnQuanLyTacGia.Font = btnFont;
            btnQuanLyTacGia.Margin = btnPad;

            btnThemSach.Text = "Thêm sách";
            btnThemSach.Dock = DockStyle.Fill;
            btnThemSach.Font = btnFont;
            btnThemSach.Margin = btnPad;

            btnXoaNhieu.Text = "Xóa nhiều sách";
            btnXoaNhieu.Dock = DockStyle.Fill;
            btnXoaNhieu.Font = btnFont;
            btnXoaNhieu.Margin = btnPad;
            btnXoaNhieu.Enabled = false;

            btnTimKiem.Text = "Tìm kiếm";
            btnTimKiem.Dock = DockStyle.Fill;
            btnTimKiem.Font = btnFont;
            btnTimKiem.Margin = btnPad;

            txtSearch.Dock = DockStyle.Fill;
            txtSearch.Margin = new Padding(6, 1, 0, 1);
            txtSearch.PlaceholderText = "Nhập tên sách";
            txtSearch.Font = new Font("Segoe UI", 10.5F, FontStyle.Regular, GraphicsUnit.Point);

            toolbar.Controls.AddRange(new Control[]
            {
                btnQuanLyNXB, btnQuanLyTheLoai, btnQuanLyTacGia,
                btnThemSach, btnXoaNhieu, btnTimKiem, txtSearch
            });

            root.Controls.Add(toolbar, 0, 1);

            // === Content ===
            contentPanel.Dock = DockStyle.Fill;

            dgvBooks.Dock = DockStyle.Fill;
            dgvBooks.AllowUserToAddRows = false;
            dgvBooks.AllowUserToDeleteRows = false;
            dgvBooks.ReadOnly = false; // checkbox column editable
            dgvBooks.MultiSelect = false;
            dgvBooks.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvBooks.RowHeadersVisible = false;
            dgvBooks.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

            lblEmpty.Text = "Không tìm thấy sách";
            lblEmpty.Dock = DockStyle.Fill;
            lblEmpty.TextAlign = ContentAlignment.MiddleCenter;
            lblEmpty.Font = new Font("Segoe UI", 12F, FontStyle.Italic, GraphicsUnit.Point);
            lblEmpty.Visible = false;

            contentPanel.Controls.Add(dgvBooks);
            contentPanel.Controls.Add(lblEmpty);

            root.Controls.Add(contentPanel, 0, 2);

            ResumeLayout(false);
        }
    }
}
