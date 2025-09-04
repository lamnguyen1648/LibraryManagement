namespace LibraryManagement.Forms.QuanLyTheLoai
{
    partial class QuanLyTheLoaiForm
    {
        private TableLayoutPanel root;     // header, toolbar, content
        private Label lblTitle;

        // Toolbar (same as NXB: 6×12% + 28%)
        private TableLayoutPanel toolbar;
        private Button btnThem;            // 0
        private Button btnXoaNhieu;        // 1
        private Panel spacer2;             // 2
        private Panel spacer3;             // 3
        private Panel spacer4;             // 4
        private Button btnTimKiem;         // 5
        private TextBox txtSearch;         // 6

        private Panel contentPanel;
        private DataGridView dgvTheLoai;
        private Label lblEmpty;

        private void InitializeComponent()
        {
            root = new TableLayoutPanel();
            lblTitle = new Label();

            toolbar = new TableLayoutPanel();
            btnThem = new Button();
            btnXoaNhieu = new Button();
            spacer2 = new Panel();
            spacer3 = new Panel();
            spacer4 = new Panel();
            btnTimKiem = new Button();
            txtSearch = new TextBox();

            contentPanel = new Panel();
            dgvTheLoai = new DataGridView();
            lblEmpty = new Label();

            SuspendLayout();

            // Form
            AutoScaleMode = AutoScaleMode.Dpi;
            Text = "Quản lý thể loại";
            MinimumSize = new Size(1100, 640);
            StartPosition = FormStartPosition.CenterScreen;

            // Root
            root.Dock = DockStyle.Fill;
            root.Padding = new Padding(12);
            root.ColumnCount = 1;
            root.RowCount = 3;
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 12F)); // header
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 10F)); // toolbar
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 78F)); // content
            Controls.Add(root);

            // Header
            lblTitle.Text = "Quản lý thể loại";
            lblTitle.Dock = DockStyle.Fill;
            lblTitle.TextAlign = ContentAlignment.TopLeft;
            lblTitle.Font = new Font("Segoe UI", 16F, FontStyle.Bold, GraphicsUnit.Point);
            lblTitle.Padding = new Padding(8, 6, 8, 6);
            root.Controls.Add(lblTitle, 0, 0);

            // Toolbar (6×12% + 28%)
            toolbar.Dock = DockStyle.Fill;
            toolbar.Padding = new Padding(0, 2, 0, 2);
            toolbar.ColumnCount = 7;
            toolbar.RowCount = 1;
            toolbar.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 12F)); // Thêm
            toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 12F)); // Xóa nhiều
            toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 12F)); // spacer
            toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 12F)); // spacer
            toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 12F)); // spacer
            toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 12F)); // Tìm
            toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 28F)); // Search box

            var btnFont = new Font("Segoe UI", 10.5F, FontStyle.Bold, GraphicsUnit.Point);
            var btnPad  = new Padding(2, 1, 2, 1);

            btnThem.Text = "Thêm thể loại";
            btnThem.Dock = DockStyle.Fill;
            btnThem.Font = btnFont;
            btnThem.Margin = btnPad;

            btnXoaNhieu.Text = "Xóa nhiều";
            btnXoaNhieu.Dock = DockStyle.Fill;
            btnXoaNhieu.Font = btnFont;
            btnXoaNhieu.Margin = btnPad;
            btnXoaNhieu.Enabled = false;

            spacer2.Dock = DockStyle.Fill;
            spacer3.Dock = DockStyle.Fill;
            spacer4.Dock = DockStyle.Fill;

            btnTimKiem.Text = "Tìm kiếm";
            btnTimKiem.Dock = DockStyle.Fill;
            btnTimKiem.Font = btnFont;
            btnTimKiem.Margin = btnPad;

            txtSearch.Dock = DockStyle.Fill;
            txtSearch.Margin = new Padding(6, 1, 0, 1);
            txtSearch.PlaceholderText = "Nhập tên thể loại";
            txtSearch.Font = new Font("Segoe UI", 10.5F, FontStyle.Regular, GraphicsUnit.Point);

            toolbar.Controls.Add(btnThem,     0, 0);
            toolbar.Controls.Add(btnXoaNhieu, 1, 0);
            toolbar.Controls.Add(spacer2,     2, 0);
            toolbar.Controls.Add(spacer3,     3, 0);
            toolbar.Controls.Add(spacer4,     4, 0);
            toolbar.Controls.Add(btnTimKiem,  5, 0);
            toolbar.Controls.Add(txtSearch,   6, 0);

            root.Controls.Add(toolbar, 0, 1);

            // Content
            contentPanel.Dock = DockStyle.Fill;

            dgvTheLoai.Dock = DockStyle.Fill;
            dgvTheLoai.AllowUserToAddRows = false;
            dgvTheLoai.AllowUserToDeleteRows = false;
            dgvTheLoai.ReadOnly = false; // checkbox column editable
            dgvTheLoai.MultiSelect = false;
            dgvTheLoai.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvTheLoai.RowHeadersVisible = false;
            dgvTheLoai.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

            lblEmpty.Text = "Không có thể loại nào";
            lblEmpty.Dock = DockStyle.Fill;
            lblEmpty.TextAlign = ContentAlignment.MiddleCenter;
            lblEmpty.Font = new Font("Segoe UI", 12F, FontStyle.Italic, GraphicsUnit.Point);
            lblEmpty.Visible = false;

            contentPanel.Controls.Add(dgvTheLoai);
            contentPanel.Controls.Add(lblEmpty);
            root.Controls.Add(contentPanel, 0, 2);

            ResumeLayout(false);
        }
    }
}
