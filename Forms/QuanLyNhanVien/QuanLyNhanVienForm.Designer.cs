namespace LibraryManagement.Forms.QuanLyNhanVien
{
    partial class QuanLyNhanVienForm
    {
        private TableLayoutPanel root;
        private Label lblTitle;

        private TableLayoutPanel toolbar;
        private Button btnQuanLyChucVu;
        private Button btnThem;
        private Button btnXoaNhieu;
        private Panel spacer3;
        private Panel spacer4;
        private Button btnTimKiem;
        private TextBox txtSearch;

        private Panel contentPanel;
        private DataGridView dgvNV;
        private Label lblEmpty;

        private void InitializeComponent()
        {
            root = new TableLayoutPanel();
            lblTitle = new Label();

            toolbar = new TableLayoutPanel();
            btnQuanLyChucVu = new Button();
            btnThem = new Button();
            btnXoaNhieu = new Button();
            spacer3 = new Panel();
            spacer4 = new Panel();
            btnTimKiem = new Button();
            txtSearch = new TextBox();

            contentPanel = new Panel();
            dgvNV = new DataGridView();
            lblEmpty = new Label();

            SuspendLayout();

            AutoScaleMode = AutoScaleMode.Dpi;
            Text = "Quản lý nhân viên";
            MinimumSize = new Size(1100, 640);
            StartPosition = FormStartPosition.CenterScreen;

            // Root (all %)
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
            lblTitle.Text = "Quản lý nhân viên";
            lblTitle.Dock = DockStyle.Fill;
            lblTitle.TextAlign = ContentAlignment.TopLeft;
            lblTitle.Font = new Font("Segoe UI", 16F, FontStyle.Bold, GraphicsUnit.Point);
            lblTitle.Padding = new Padding(8, 6, 8, 6);
            root.Controls.Add(lblTitle, 0, 0);

            // Toolbar
            toolbar.Dock = DockStyle.Fill;
            toolbar.Padding = new Padding(0, 2, 0, 2);
            toolbar.ColumnCount = 7;
            toolbar.RowCount = 1;
            toolbar.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            // Percent widths tuned so both left buttons fit on one line.
            // Keep search button (12%) and search box (28%) consistent with other forms.
            toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 14F)); // 0: Quản lý Chức vụ (wider)
            toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 14F)); // 1: Thêm nhân viên (wider)
            toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 10F)); // 2: Xóa nhiều
            toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 11F)); // 3: spacer
            toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 11F)); // 4: spacer
            toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 12F)); // 5: Tìm kiếm
            toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 28F)); // 6: Search box

            var btnFont = new Font("Segoe UI", 10.5F, FontStyle.Bold, GraphicsUnit.Point);
            var btnPad  = new Padding(2, 1, 2, 1);

            btnQuanLyChucVu.Text = "Quản lý Chức vụ";
            btnQuanLyChucVu.Dock = DockStyle.Fill;
            btnQuanLyChucVu.Font = btnFont;
            btnQuanLyChucVu.Margin = btnPad;
            btnQuanLyChucVu.TextAlign = ContentAlignment.MiddleCenter;

            btnThem.Text = "Thêm nhân viên";
            btnThem.Dock = DockStyle.Fill;
            btnThem.Font = btnFont;
            btnThem.Margin = btnPad;
            btnThem.TextAlign = ContentAlignment.MiddleCenter;

            btnXoaNhieu.Text = "Xóa nhiều";
            btnXoaNhieu.Dock = DockStyle.Fill;
            btnXoaNhieu.Font = btnFont;
            btnXoaNhieu.Margin = btnPad;
            btnXoaNhieu.Enabled = false;

            spacer3.Dock = DockStyle.Fill;
            spacer4.Dock = DockStyle.Fill;

            btnTimKiem.Text = "Tìm kiếm";
            btnTimKiem.Dock = DockStyle.Fill;
            btnTimKiem.Font = btnFont;
            btnTimKiem.Margin = btnPad;

            txtSearch.Dock = DockStyle.Fill;
            txtSearch.Margin = new Padding(6, 1, 0, 1);
            txtSearch.PlaceholderText = "Nhập tên nhân viên";
            txtSearch.Font = new Font("Segoe UI", 10.5F, FontStyle.Regular, GraphicsUnit.Point);

            toolbar.Controls.Add(btnQuanLyChucVu, 0, 0);
            toolbar.Controls.Add(btnThem,          1, 0);
            toolbar.Controls.Add(btnXoaNhieu,      2, 0);
            toolbar.Controls.Add(spacer3,          3, 0);
            toolbar.Controls.Add(spacer4,          4, 0);
            toolbar.Controls.Add(btnTimKiem,       5, 0);
            toolbar.Controls.Add(txtSearch,        6, 0);
            root.Controls.Add(toolbar, 0, 1);

            // Content
            contentPanel.Dock = DockStyle.Fill;

            dgvNV.Dock = DockStyle.Fill;
            dgvNV.AllowUserToAddRows = false;
            dgvNV.AllowUserToDeleteRows = false;
            dgvNV.ReadOnly = false; // checkbox cells editable
            dgvNV.MultiSelect = false;
            dgvNV.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvNV.RowHeadersVisible = false;
            dgvNV.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

            lblEmpty.Text = "Không có nhân viên nào";
            lblEmpty.Dock = DockStyle.Fill;
            lblEmpty.TextAlign = ContentAlignment.MiddleCenter;
            lblEmpty.Font = new Font("Segoe UI", 12F, FontStyle.Italic, GraphicsUnit.Point);
            lblEmpty.Visible = false;

            contentPanel.Controls.Add(dgvNV);
            contentPanel.Controls.Add(lblEmpty);
            root.Controls.Add(contentPanel, 0, 2);

            ResumeLayout(false);
        }
    }
}
