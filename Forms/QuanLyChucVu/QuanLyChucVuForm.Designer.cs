namespace LibraryManagement.Forms.QuanLyChucVu
{
    partial class QuanLyChucVuForm
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
        private DataGridView dgvCV;
        private Label lblEmpty;

        private void InitializeComponent()
        {
            this.root = new System.Windows.Forms.TableLayoutPanel();
            this.lblTitle = new System.Windows.Forms.Label();

            this.toolbar = new System.Windows.Forms.TableLayoutPanel();
            this.btnThem = new System.Windows.Forms.Button();
            this.btnXoaNhieu = new System.Windows.Forms.Button();
            this.spacer = new System.Windows.Forms.Panel();
            this.btnTimKiem = new System.Windows.Forms.Button();
            this.txtSearch = new System.Windows.Forms.TextBox();

            this.contentPanel = new System.Windows.Forms.Panel();
            this.dgvCV = new System.Windows.Forms.DataGridView();
            this.lblEmpty = new System.Windows.Forms.Label();

            this.SuspendLayout();

            this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.Text = "Quản lý chức vụ";
            this.MinimumSize = new System.Drawing.Size(1000, 600);
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;

            this.root.Dock = System.Windows.Forms.DockStyle.Fill;
            this.root.Padding = new System.Windows.Forms.Padding(12);
            this.root.ColumnCount = 1;
            this.root.RowCount = 3;
            this.root.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.root.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 12F)); // header
            this.root.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 10F)); // toolbar
            this.root.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 78F)); // content
            this.Controls.Add(this.root);

            this.lblTitle.Text = "Quản lý chức vụ";
            this.lblTitle.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblTitle.TextAlign = System.Drawing.ContentAlignment.TopLeft;
            this.lblTitle.Font = new System.Drawing.Font("Segoe UI", 16F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            this.lblTitle.Padding = new System.Windows.Forms.Padding(8, 6, 8, 6);
            this.root.Controls.Add(this.lblTitle, 0, 0);

            this.toolbar.Dock = System.Windows.Forms.DockStyle.Fill;
            this.toolbar.Padding = new System.Windows.Forms.Padding(0, 2, 0, 2);
            this.toolbar.ColumnCount = 5;
            this.toolbar.RowCount = 1;
            this.toolbar.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.toolbar.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 12F)); // 0
            this.toolbar.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 12F)); // 1
            this.toolbar.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 36F)); // 2
            this.toolbar.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 12F)); // 3
            this.toolbar.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 28F)); // 4

            System.Drawing.Font btnFont = new System.Drawing.Font("Segoe UI", 10.5F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            System.Windows.Forms.Padding btnPad = new System.Windows.Forms.Padding(2, 1, 2, 1);

            this.btnThem.Text = "Thêm chức vụ";
            this.btnThem.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnThem.Font = btnFont;
            this.btnThem.Margin = btnPad;

            this.btnXoaNhieu.Text = "Xóa nhiều";
            this.btnXoaNhieu.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnXoaNhieu.Font = btnFont;
            this.btnXoaNhieu.Margin = btnPad;
            this.btnXoaNhieu.Enabled = false;

            this.spacer.Dock = System.Windows.Forms.DockStyle.Fill;

            this.btnTimKiem.Text = "Tìm kiếm";
            this.btnTimKiem.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnTimKiem.Font = btnFont;
            this.btnTimKiem.Margin = btnPad;

            this.txtSearch.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtSearch.Margin = new System.Windows.Forms.Padding(6, 1, 0, 1);
            this.txtSearch.PlaceholderText = "Nhập tên chức vụ";
            this.txtSearch.Font = new System.Drawing.Font("Segoe UI", 10.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);

            this.toolbar.Controls.Add(this.btnThem,     0, 0);
            this.toolbar.Controls.Add(this.btnXoaNhieu, 1, 0);
            this.toolbar.Controls.Add(this.spacer,      2, 0);
            this.toolbar.Controls.Add(this.btnTimKiem,  3, 0);
            this.toolbar.Controls.Add(this.txtSearch,   4, 0);
            this.root.Controls.Add(this.toolbar, 0, 1);

            this.contentPanel.Dock = System.Windows.Forms.DockStyle.Fill;

            this.dgvCV.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgvCV.AllowUserToAddRows = false;
            this.dgvCV.AllowUserToDeleteRows = false;
            this.dgvCV.ReadOnly = false;
            this.dgvCV.MultiSelect = false;
            this.dgvCV.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dgvCV.RowHeadersVisible = false;
            this.dgvCV.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;

            this.lblEmpty.Text = "Không tìm thấy dữ liệu";
            this.lblEmpty.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblEmpty.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.lblEmpty.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Italic, System.Drawing.GraphicsUnit.Point);
            this.lblEmpty.Visible = false;

            this.contentPanel.Controls.Add(this.dgvCV);
            this.contentPanel.Controls.Add(this.lblEmpty);
            this.root.Controls.Add(this.contentPanel, 0, 2);

            this.ResumeLayout(false);
        }
    }
}
