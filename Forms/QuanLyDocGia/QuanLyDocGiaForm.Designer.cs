namespace LibraryManagement.Forms.QuanLyDocGia
{
    public partial class QuanLyDocGiaForm
    {
        private TableLayoutPanel root;
        private Label lblTitle;

        private TableLayoutPanel toolbar;
        private Button btnThem;
        private Button btnXoaNhieu;
        private Panel spacer;
        private Button btnTimKiem;
        private TextBox txtSearch;

        private Panel contentPanel;
        private DataGridView dgvDG;
        private Label lblEmpty;

        // Renamed to avoid collision with another InitializeComponent()
        private void InitializeComponent_DocGia()
        {
            root = new TableLayoutPanel();
            lblTitle = new Label();

            toolbar = new TableLayoutPanel();
            btnThem = new Button();
            btnXoaNhieu = new Button();
            spacer = new Panel();
            btnTimKiem = new Button();
            txtSearch = new TextBox();

            contentPanel = new Panel();
            dgvDG = new DataGridView();
            lblEmpty = new Label();

            SuspendLayout();

            AutoScaleMode = AutoScaleMode.Dpi;
            Text = "Quản lý độc giả";
            MinimumSize = new Size(1000, 600);
            StartPosition = FormStartPosition.CenterParent;

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
            lblTitle.Text = "Quản lý độc giả";
            lblTitle.Dock = DockStyle.Fill;
            lblTitle.TextAlign = ContentAlignment.TopLeft;
            lblTitle.Font = new Font("Segoe UI", 16F, FontStyle.Bold, GraphicsUnit.Point);
            lblTitle.Padding = new Padding(8, 6, 8, 6);
            root.Controls.Add(lblTitle, 0, 0);

            // Toolbar: [Thêm 12%][Xóa nhiều 12%][Spacer 36%][Tìm kiếm 12%][Search 28%]
            toolbar.Dock = DockStyle.Fill;
            toolbar.Padding = new Padding(0, 2, 0, 2);
            toolbar.ColumnCount = 5;
            toolbar.RowCount = 1;
            toolbar.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 12F)); // 0 Thêm
            toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 12F)); // 1 Xóa nhiều
            toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 36F)); // 2 spacer
            toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 12F)); // 3 Tìm kiếm
            toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 28F)); // 4 Search box

            var btnFont = new Font("Segoe UI", 10.5F, FontStyle.Bold, GraphicsUnit.Point);
            var btnPad  = new Padding(2, 1, 2, 1);

            btnThem.Text = "Thêm độc giả";
            btnThem.Dock = DockStyle.Fill;
            btnThem.Font = btnFont;
            btnThem.Margin = btnPad;

            btnXoaNhieu.Text = "Xóa nhiều";
            btnXoaNhieu.Dock = DockStyle.Fill;
            btnXoaNhieu.Font = btnFont;
            btnXoaNhieu.Margin = btnPad;
            btnXoaNhieu.Enabled = false;

            spacer.Dock = DockStyle.Fill;

            btnTimKiem.Text = "Tìm kiếm";
            btnTimKiem.Dock = DockStyle.Fill;
            btnTimKiem.Font = btnFont;
            btnTimKiem.Margin = btnPad;

            txtSearch.Dock = DockStyle.Fill;
            txtSearch.Margin = new Padding(6, 1, 0, 1);
            txtSearch.PlaceholderText = "Nhập tên độc giả";
            txtSearch.Font = new Font("Segoe UI", 10.5F, FontStyle.Regular, GraphicsUnit.Point);

            toolbar.Controls.Add(btnThem,     0, 0);
            toolbar.Controls.Add(btnXoaNhieu, 1, 0);
            toolbar.Controls.Add(spacer,      2, 0);
            toolbar.Controls.Add(btnTimKiem,  3, 0);
            toolbar.Controls.Add(txtSearch,   4, 0);
            root.Controls.Add(toolbar, 0, 1);

            // Content
            contentPanel.Dock = DockStyle.Fill;

            dgvDG.Dock = DockStyle.Fill;
            dgvDG.AllowUserToAddRows = false;
            dgvDG.AllowUserToDeleteRows = false;
            dgvDG.ReadOnly = false; // checkbox editable
            dgvDG.MultiSelect = false;
            dgvDG.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvDG.RowHeadersVisible = false;
            dgvDG.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

            lblEmpty.Text = "Không tìm thấy độc giả";
            lblEmpty.Dock = DockStyle.Fill;
            lblEmpty.TextAlign = ContentAlignment.MiddleCenter;
            lblEmpty.Font = new Font("Segoe UI", 12F, FontStyle.Italic, GraphicsUnit.Point);
            lblEmpty.Visible = false;

            contentPanel.Controls.Add(dgvDG);
            contentPanel.Controls.Add(lblEmpty);
            root.Controls.Add(contentPanel, 0, 2);

            ResumeLayout(false);
        }
    }
}
