using System.Drawing;
using System.Windows.Forms;

namespace LibraryManagement.Forms.QuanLyPhieuPhat
{
    partial class QuanLyPhieuPhatForm
    {
        private TableLayoutPanel root;
        private Label lblTitle;

        private TableLayoutPanel toolbar;
        private Button btnThem;
        private Button btnXoaNhieu;

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

            contentPanel = new Panel();
            dgvPP = new DataGridView();
            lblEmpty = new Label();

            SuspendLayout();

            // Form
            AutoScaleMode = AutoScaleMode.Dpi;
            Text = "Quản lý phiếu phạt";
            MinimumSize = new Size(1000, 600);
            StartPosition = FormStartPosition.CenterScreen;

            // Root (12% header | 10% toolbar | 78% content)
            root.Dock = DockStyle.Fill;
            root.Padding = new Padding(12);
            root.ColumnCount = 1;
            root.RowCount = 3;
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 12F));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 10F));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 78F));
            Controls.Add(root);

            // Header
            lblTitle.Text = "Quản lý phiếu phạt";
            lblTitle.Dock = DockStyle.Fill;
            lblTitle.TextAlign = ContentAlignment.TopLeft;
            lblTitle.Font = new Font("Segoe UI", 16F, FontStyle.Bold);
            lblTitle.Padding = new Padding(8, 6, 8, 6);
            root.Controls.Add(lblTitle, 0, 0);

            // Toolbar (2 buttons)
            toolbar.Dock = DockStyle.Fill;
            toolbar.Padding = new Padding(0, 2, 0, 2);
            toolbar.RowCount = 1;
            toolbar.ColumnCount = 6;
            toolbar.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20F)); // Thêm
            toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20F)); // Xóa nhiều
            toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60F));
            toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 0F));
            toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 0F));
            toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 0F));

            var btnFont = new Font("Segoe UI", 11F, FontStyle.Bold);
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

            toolbar.Controls.Add(btnThem,     0, 0);
            toolbar.Controls.Add(btnXoaNhieu, 1, 0);
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
            lblEmpty.Font = new Font("Segoe UI", 12F, FontStyle.Italic);
            lblEmpty.Visible = false;

            contentPanel.Controls.Add(dgvPP);
            contentPanel.Controls.Add(lblEmpty);
            root.Controls.Add(contentPanel, 0, 2);

            ResumeLayout(false);
        }
    }
}
