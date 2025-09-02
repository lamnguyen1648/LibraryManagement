using System.Drawing;
using System.Windows.Forms;

namespace LibraryManagement.Forms.QuanLyPhieuMuon
{
    partial class QuanLyPhieuMuonForm
    {
        private TableLayoutPanel root;
        private Label lblTitle;

        private TableLayoutPanel toolbar;
        private Button btnQuanLyPhieuPhat; // wider
        private Button btnThem;            // wider
        private Button btnXoaNhieu;        // wider
        private Panel spacer2;
        private Panel spacer3;
        private Panel spacer4;
        private Button btnTimKiem;
        private TextBox txtSearch;

        private Panel contentPanel;
        private DataGridView dgvPhieuMuon;
        private Label lblEmpty;

        private void InitializeComponent()
        {
            root = new TableLayoutPanel();
            lblTitle = new Label();

            toolbar = new TableLayoutPanel();
            btnQuanLyPhieuPhat = new Button();
            btnThem = new Button();
            btnXoaNhieu = new Button();
            spacer2 = new Panel();
            spacer3 = new Panel();
            spacer4 = new Panel();
            btnTimKiem = new Button();
            txtSearch = new TextBox();

            contentPanel = new Panel();
            dgvPhieuMuon = new DataGridView();
            lblEmpty = new Label();

            SuspendLayout();

            // Form
            AutoScaleMode = AutoScaleMode.Dpi;
            Text = "Quản lý phiếu mượn";
            MinimumSize = new Size(1100, 640);
            StartPosition = FormStartPosition.CenterScreen;

            // Root (header 12% | toolbar 12% | content 76%)
            root.Dock = DockStyle.Fill;
            root.Padding = new Padding(12);
            root.ColumnCount = 1;
            root.RowCount = 3;
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 12F));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 12F)); // ↑ taller toolbar
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 76F));
            Controls.Add(root);

            // Header
            lblTitle.Text = "Quản lý phiếu mượn";
            lblTitle.Dock = DockStyle.Fill;
            lblTitle.TextAlign = ContentAlignment.TopLeft;
            lblTitle.Font = new Font("Segoe UI", 16F, FontStyle.Bold, GraphicsUnit.Point);
            lblTitle.Padding = new Padding(8, 6, 8, 6);
            root.Controls.Add(lblTitle, 0, 0);

            // Toolbar layout (100% by percent; buttons wider)
            toolbar.Dock = DockStyle.Fill;
            toolbar.Padding = new Padding(0, 2, 0, 2);
            toolbar.ColumnCount = 8;
            toolbar.RowCount = 1;
            toolbar.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            // 15% + 15% + 12% buttons, 5% + 5% + 8% spacers, 10% search btn, 30% search box = 100%
            toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 15F)); // Quản lý phiếu phạt (wider)
            toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 15F)); // Thêm phiếu (wider)
            toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 12F)); // Xóa nhiều (wider)
            toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 5F));  // spacer
            toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 5F));  // spacer
            toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 8F));  // spacer
            toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 10F)); // Tìm kiếm
            toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30F)); // Search box

            var btnFont = new Font("Segoe UI", 11.25F, FontStyle.Bold, GraphicsUnit.Point); // slightly larger
            var btnPad  = new Padding(4, 2, 4, 2);

            btnQuanLyPhieuPhat.Text = "Quản lý phiếu phạt";
            btnQuanLyPhieuPhat.Dock = DockStyle.Fill;
            btnQuanLyPhieuPhat.Font = btnFont;
            btnQuanLyPhieuPhat.Margin = btnPad;

            btnThem.Text = "Thêm phiếu";
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
            txtSearch.PlaceholderText = "Nhập từ khóa (VD: Tình trạng)";
            txtSearch.Font = new Font("Segoe UI", 11F, FontStyle.Regular, GraphicsUnit.Point);

            toolbar.Controls.Add(btnQuanLyPhieuPhat, 0, 0);
            toolbar.Controls.Add(btnThem,              1, 0);
            toolbar.Controls.Add(btnXoaNhieu,         2, 0);
            toolbar.Controls.Add(spacer2,             3, 0);
            toolbar.Controls.Add(spacer3,             4, 0);
            toolbar.Controls.Add(spacer4,             5, 0);
            toolbar.Controls.Add(btnTimKiem,          6, 0);
            toolbar.Controls.Add(txtSearch,           7, 0);
            root.Controls.Add(toolbar, 0, 1);

            // Content
            contentPanel.Dock = DockStyle.Fill;

            dgvPhieuMuon.Dock = DockStyle.Fill;
            dgvPhieuMuon.AllowUserToAddRows = false;
            dgvPhieuMuon.AllowUserToDeleteRows = false;
            dgvPhieuMuon.ReadOnly = false;
            dgvPhieuMuon.MultiSelect = false;
            dgvPhieuMuon.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvPhieuMuon.RowHeadersVisible = false;
            dgvPhieuMuon.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

            lblEmpty.Text = "Không có phiếu mượn nào";
            lblEmpty.Dock = DockStyle.Fill;
            lblEmpty.TextAlign = ContentAlignment.MiddleCenter;
            lblEmpty.Font = new Font("Segoe UI", 12F, FontStyle.Italic, GraphicsUnit.Point);
            lblEmpty.Visible = false;

            contentPanel.Controls.Add(dgvPhieuMuon);
            contentPanel.Controls.Add(lblEmpty);
            root.Controls.Add(contentPanel, 0, 2);

            ResumeLayout(false);
        }
    }
}
