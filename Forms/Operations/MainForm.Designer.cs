namespace LibraryManagement.Forms.Operations  // ← match your actual project namespace
{
    partial class MainForm
    {
        private TableLayoutPanel root;
        private Label lblWelcome;

        private TableLayoutPanel rightPanel;
        private TableLayoutPanel menuGrid;

        private Button btnQuanLySach;
        private Button btnQuanLyNhanVien;
        private Button btnQuanLyPhieuMuon;
        private Button btnQuanLyDocGia;

        private void InitializeComponent()
        {
            root = new TableLayoutPanel();
            lblWelcome = new Label();

            rightPanel = new TableLayoutPanel();
            menuGrid = new TableLayoutPanel();

            btnQuanLySach = new Button();
            btnQuanLyNhanVien = new Button();
            btnQuanLyPhieuMuon = new Button();
            btnQuanLyDocGia = new Button();

            SuspendLayout();

            // === Form ===
            AutoScaleMode = AutoScaleMode.Dpi;
            MinimumSize = new Size(900, 520);
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Hệ thống quản lý thư viện";

            // === Root (2 columns: left welcome, right menu) ===
            root.Dock = DockStyle.Fill;
            root.Padding = new Padding(12);
            root.ColumnCount = 2;
            root.RowCount = 1;
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 65F)); // left 65%
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35F)); // right 35%
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            // --- Left: Welcome label (top-left with inner padding/margins) ---
            lblWelcome.Dock = DockStyle.Fill;
            lblWelcome.Text = "Xin chào!";
            lblWelcome.TextAlign = ContentAlignment.TopLeft;          // <-- top left
            lblWelcome.Font = new Font("Segoe UI", 18F, FontStyle.Bold, GraphicsUnit.Point);
            lblWelcome.Padding = new Padding(16, 16, 24, 8);           // <-- inner margins
            lblWelcome.Margin = new Padding(6, 6, 12, 6);              // outer spacing

            // --- Right: container for menu grid only (removed 'Chọn chức năng') ---
            rightPanel.Dock = DockStyle.Fill;
            rightPanel.ColumnCount = 1;
            rightPanel.RowCount = 1;                                   // <-- single row
            rightPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            rightPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            // Menu grid: 4 equal rows for 4 buttons (top-down)
            menuGrid.Dock = DockStyle.Fill;
            menuGrid.ColumnCount = 1;
            menuGrid.RowCount = 4;
            menuGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            menuGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 25F));
            menuGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 25F));
            menuGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 25F));
            menuGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 25F));
            menuGrid.Padding = new Padding(0, 6, 0, 0);

            // --- Buttons: more "vibrant" text (bigger & bold); full-width; nice spacing ---
            // You can adjust ForeColor if desired; Font change keeps system theme friendly.
            var buttonFont = new Font("Segoe UI", 12F, FontStyle.Bold, GraphicsUnit.Point);

            btnQuanLySach.Text = "Quản lý sách";
            btnQuanLySach.Dock = DockStyle.Fill;
            btnQuanLySach.Font = buttonFont;
            btnQuanLySach.Margin = new Padding(0, 6, 0, 6);
            btnQuanLySach.Click += btnQuanLySach_Click;

            btnQuanLyNhanVien.Text = "Quản lý nhân viên";
            btnQuanLyNhanVien.Dock = DockStyle.Fill;
            btnQuanLyNhanVien.Font = buttonFont;
            btnQuanLyNhanVien.Margin = new Padding(0, 6, 0, 6);
            btnQuanLyNhanVien.Click += btnQuanLyNhanVien_Click;

            btnQuanLyPhieuMuon.Text = "Quản lý phiếu mượn";
            btnQuanLyPhieuMuon.Dock = DockStyle.Fill;
            btnQuanLyPhieuMuon.Font = buttonFont;
            btnQuanLyPhieuMuon.Margin = new Padding(0, 6, 0, 6);
            btnQuanLyPhieuMuon.Click += btnQuanLyPhieuMuon_Click;

            btnQuanLyDocGia.Text = "Quản lý độc giả";
            btnQuanLyDocGia.Dock = DockStyle.Fill;
            btnQuanLyDocGia.Font = buttonFont;
            btnQuanLyDocGia.Margin = new Padding(0, 6, 0, 6);
            btnQuanLyDocGia.Click += btnQuanLyDocGia_Click;

            // Add buttons to menu grid
            menuGrid.Controls.Add(btnQuanLySach, 0, 0);
            menuGrid.Controls.Add(btnQuanLyNhanVien, 0, 1);
            menuGrid.Controls.Add(btnQuanLyPhieuMuon, 0, 2);
            menuGrid.Controls.Add(btnQuanLyDocGia, 0, 3);

            // Assemble right panel
            rightPanel.Controls.Add(menuGrid, 0, 0);

            // Add to root
            root.Controls.Add(lblWelcome, 0, 0);
            root.Controls.Add(rightPanel, 1, 0);

            // Add root to form
            Controls.Add(root);

            ResumeLayout(false);
        }
    }
}
