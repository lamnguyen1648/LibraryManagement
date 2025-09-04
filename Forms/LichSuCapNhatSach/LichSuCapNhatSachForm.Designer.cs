namespace LibraryManagement.Forms.LichSuCapNhatSach
{
    public partial class LichSuCapNhatSachForm
    {
        private TableLayoutPanel root;
        private Label lblTitle;

        private TableLayoutPanel toolbar;
        private Panel spacer;
        private Button btnTimKiem;
        private TextBox txtSearch;

        private Panel contentPanel;
        private DataGridView dgvLog;
        private Label lblEmpty;

        // Unique name to avoid clashes with other InitializeComponent() methods.
        private void InitializeComponent_LichSuCapNhatSach()
        {
            root = new TableLayoutPanel();
            lblTitle = new Label();

            toolbar = new TableLayoutPanel();
            spacer = new Panel();
            btnTimKiem = new Button();
            txtSearch = new TextBox();

            contentPanel = new Panel();
            dgvLog = new DataGridView();
            lblEmpty = new Label();

            SuspendLayout();

            AutoScaleMode = AutoScaleMode.Dpi;
            Text = "Lịch sử cập nhật sách";
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
            lblTitle.Text = "Lịch sử cập nhật sách";
            lblTitle.Dock = DockStyle.Fill;
            lblTitle.TextAlign = ContentAlignment.TopLeft;
            lblTitle.Font = new Font("Segoe UI", 16F, FontStyle.Bold, GraphicsUnit.Point);
            lblTitle.Padding = new Padding(8, 6, 8, 6);
            root.Controls.Add(lblTitle, 0, 0);

            // Toolbar: [Spacer 60%][Tìm kiếm 12%][Search 28%]
            toolbar.Dock = DockStyle.Fill;
            toolbar.Padding = new Padding(0, 2, 0, 2);
            toolbar.ColumnCount = 3;
            toolbar.RowCount = 1;
            toolbar.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60F)); // spacer
            toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 12F)); // search button
            toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 28F)); // search box

            spacer.Dock = DockStyle.Fill;

            btnTimKiem.Text = "Tìm kiếm";
            btnTimKiem.Dock = DockStyle.Fill;
            btnTimKiem.Font = new Font("Segoe UI", 10.5F, FontStyle.Bold, GraphicsUnit.Point);
            btnTimKiem.Margin = new Padding(2, 1, 2, 1);

            txtSearch.Dock = DockStyle.Fill;
            txtSearch.PlaceholderText = "Nhập tên sách / nhân viên / chi tiết";
            txtSearch.Font = new Font("Segoe UI", 10.5F, FontStyle.Regular, GraphicsUnit.Point);
            txtSearch.Margin = new Padding(6, 1, 0, 1);

            toolbar.Controls.Add(spacer,    0, 0);
            toolbar.Controls.Add(btnTimKiem,1, 0);
            toolbar.Controls.Add(txtSearch, 2, 0);
            root.Controls.Add(toolbar, 0, 1);

            // Content
            contentPanel.Dock = DockStyle.Fill;

            dgvLog.Dock = DockStyle.Fill;
            dgvLog.ReadOnly = true;       // no editing here
            dgvLog.RowHeadersVisible = false;
            dgvLog.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvLog.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

            lblEmpty.Text = "Không có dữ liệu lịch sử";
            lblEmpty.Dock = DockStyle.Fill;
            lblEmpty.TextAlign = ContentAlignment.MiddleCenter;
            lblEmpty.Font = new Font("Segoe UI", 12F, FontStyle.Italic, GraphicsUnit.Point);
            lblEmpty.Visible = false;

            contentPanel.Controls.Add(dgvLog);
            contentPanel.Controls.Add(lblEmpty);
            root.Controls.Add(contentPanel, 0, 2);

            ResumeLayout(false);
        }
    }
}
