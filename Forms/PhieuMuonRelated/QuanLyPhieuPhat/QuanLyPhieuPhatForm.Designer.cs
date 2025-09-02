namespace LibraryManagement.Forms.PhieuMuonRelated.QuanLyPhieuPhat
{
    partial class QuanLyPhieuPhatForm
    {
        private TableLayoutPanel root;
        private Label lblTitle;

        private TableLayoutPanel toolbar;
        private Button btnThem;
        private Button btnXoaNhieu;
        private Panel spacer2;   // NEW (match PhieuMuon layout)
        private Panel spacer3;   // NEW
        private Panel spacer4;   // NEW
        private Button btnTimKiem;
        private TextBox txtSearch;

        private Panel contentPanel;
        private DataGridView dgvPP;
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
            dgvPP = new DataGridView();
            lblEmpty = new Label();

            SuspendLayout();

            // Form
            this.AutoScaleMode = AutoScaleMode.Dpi;
            Text = "Quản lý phiếu phạt";
            MinimumSize = new Size(1100, 640);               // match PhieuMuon
            StartPosition = FormStartPosition.CenterScreen;

            // Root (header 12% | toolbar 12% | content 76%)
            root.Dock = DockStyle.Fill;
            root.Padding = new Padding(12);
            root.ColumnCount = 1;
            root.RowCount = 3;
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 12F));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 12F));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 76F));
            Controls.Add(root);

            // Header
            lblTitle.Text = "Quản lý phiếu phạt";
            lblTitle.Dock = DockStyle.Fill;
            lblTitle.TextAlign = ContentAlignment.TopLeft;
            lblTitle.Font = new Font("Segoe UI", 16F, FontStyle.Bold, GraphicsUnit.Point);
            lblTitle.Padding = new Padding(8, 6, 8, 6);
            root.Controls.Add(lblTitle, 0, 0);

            // Toolbar (exact same proportions as PhieuMuon):
            // 15% Thêm | 15% Xóa nhiều | 12% (unused, kept for symmetry) | 5% + 5% + 8% spacers | 10% Tìm kiếm | 30% Search box
            toolbar.Dock = DockStyle.Fill;
            toolbar.Padding = new Padding(0, 2, 0, 2);
            toolbar.ColumnCount = 8;
            toolbar.RowCount = 1;
            toolbar.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 15F)); // Thêm (wider)
            toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 15F)); // Xóa nhiều (wider)
            toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 12F)); // (kept empty to mirror layout)
            toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 5F));  // spacer
            toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 5F));  // spacer
            toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 8F));  // spacer
            toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 10F)); // Tìm kiếm
            toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30F)); // Search box

            var btnFont = new Font("Segoe UI", 11.25F, FontStyle.Bold, GraphicsUnit.Point);
            var btnPad  = new Padding(4, 2, 4, 2);

            btnThem.Text = "Thêm phiếu phạt";
            btnThem.Dock = DockStyle.Fill;
            btnThem.Font = btnFont;
            btnThem.Margin = btnPad;

            btnXoaNhieu.Text = "Xóa nhiều";
            btnXoaNhieu.Dock = DockStyle.Fill;
            btnXoaNhieu.Font = btnFont;
            btnXoaNhieu.Margin = btnPad;
            btnXoaNhieu.Enabled = false;

            spacer2.Dock = DockStyle.Fill; spacer3.Dock = DockStyle.Fill; spacer4.Dock = DockStyle.Fill;

            btnTimKiem.Text = "Tìm kiếm";
            btnTimKiem.Dock = DockStyle.Fill;
            btnTimKiem.Font = btnFont;
            btnTimKiem.Margin = btnPad;

            txtSearch.Dock = DockStyle.Fill;
            txtSearch.Margin = new Padding(8, 2, 0, 2);
            txtSearch.PlaceholderText = "Nhập từ khóa (VD: Lý do)";
            txtSearch.Font = new Font("Segoe UI", 11F, FontStyle.Regular, GraphicsUnit.Point);

            // add controls to toolbar
            toolbar.Controls.Add(btnThem,     0, 0);
            toolbar.Controls.Add(btnXoaNhieu, 1, 0);
            toolbar.Controls.Add(new Panel() { Dock = DockStyle.Fill }, 2, 0); // keep column for symmetry
            toolbar.Controls.Add(spacer2,     3, 0);
            toolbar.Controls.Add(spacer3,     4, 0);
            toolbar.Controls.Add(spacer4,     5, 0);
            toolbar.Controls.Add(btnTimKiem,  6, 0);
            toolbar.Controls.Add(txtSearch,   7, 0);
            root.Controls.Add(toolbar, 0, 1);

            // Content
            contentPanel.Dock = DockStyle.Fill;

            dgvPP.Dock = DockStyle.Fill;
            dgvPP.AllowUserToAddRows = false;
            dgvPP.AllowUserToDeleteRows = false;
            dgvPP.ReadOnly = false; // checkbox editable
            dgvPP.MultiSelect = false;
            dgvPP.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvPP.RowHeadersVisible = false;
            dgvPP.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

            lblEmpty.Text = "Không có phiếu phạt";
            lblEmpty.Dock = DockStyle.Fill;
            lblEmpty.TextAlign = ContentAlignment.MiddleCenter;
            lblEmpty.Font = new Font("Segoe UI", 12F, FontStyle.Italic, GraphicsUnit.Point);
            lblEmpty.Visible = false;

            contentPanel.Controls.Add(dgvPP);
            contentPanel.Controls.Add(lblEmpty);
            root.Controls.Add(contentPanel, 0, 2);

            ResumeLayout(false);
        }
    }
}
