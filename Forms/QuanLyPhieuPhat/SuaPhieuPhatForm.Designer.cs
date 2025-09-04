namespace LibraryManagement.Forms.QuanLyPhieuPhat
{
    partial class SuaPhieuPhatForm
    {
        private TableLayoutPanel root;
        private Label lblTitle;

        private TableLayoutPanel grid;
        private FlowLayoutPanel flNV;
        private TextBox txtNhanVien;     // disabled display only

        private FlowLayoutPanel flPM;
        private ComboBox cbPhieuMuon;

        private FlowLayoutPanel flLyDo;
        private TextBox txtLyDo;

        private FlowLayoutPanel flTien;
        private TextBox txtSoTien;

        private TableLayoutPanel actions;
        private Button btnLuu;
        private Button btnHuy;

        private void InitializeComponent()
        {
            root = new TableLayoutPanel();
            lblTitle = new Label();
            grid = new TableLayoutPanel();

            flNV = new FlowLayoutPanel();
            txtNhanVien = new TextBox();

            flPM = new FlowLayoutPanel();
            cbPhieuMuon = new ComboBox();

            flLyDo = new FlowLayoutPanel();
            txtLyDo = new TextBox();

            flTien = new FlowLayoutPanel();
            txtSoTien = new TextBox();

            actions = new TableLayoutPanel();
            btnLuu = new Button();
            btnHuy = new Button();

            SuspendLayout();

            // Form
            AutoScaleMode = AutoScaleMode.Dpi;
            Text = "Sửa phiếu phạt";
            MinimumSize = new Size(820, 560);

            // Root
            root.Dock = DockStyle.Fill;
            root.Padding = new Padding(12);
            root.ColumnCount = 1; root.RowCount = 3;
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 12F));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 73F));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 15F));
            Controls.Add(root);

            // Header
            lblTitle.Text = "Sửa phiếu phạt";
            lblTitle.Dock = DockStyle.Fill;
            lblTitle.TextAlign = ContentAlignment.MiddleLeft;
            lblTitle.Font = new Font("Segoe UI", 16F, FontStyle.Bold);
            lblTitle.Padding = new Padding(8, 6, 8, 6);
            root.Controls.Add(lblTitle, 0, 0);

            // Grid
            grid.Dock = DockStyle.Fill;
            grid.ColumnCount = 2;
            grid.RowCount = 4;
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35F));
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 65F));
            grid.RowStyles.Add(new RowStyle(SizeType.Percent, 20F));
            grid.RowStyles.Add(new RowStyle(SizeType.Percent, 20F));
            grid.RowStyles.Add(new RowStyle(SizeType.Percent, 40F));
            grid.RowStyles.Add(new RowStyle(SizeType.Percent, 20F));
            root.Controls.Add(grid, 0, 1);

            var labelFont = new Font("Segoe UI", 10F);
            var inputFont = new Font("Segoe UI", 10F);

            FlowLayoutPanel MakeLabelStar(string text, bool required)
            {
                var pnl = new FlowLayoutPanel
                {
                    Dock = DockStyle.Fill, WrapContents = false, AutoSize = false,
                    Padding = new Padding(6, 0, 0, 0), Margin = new Padding(0)
                };
                var lbl  = new Label { AutoSize = true, Text = text, Font = labelFont, Margin = new Padding(0, 6, 2, 0) };
                var star = new Label { AutoSize = true, Text = required ? "*" : "", ForeColor = Color.Firebrick, Font = new Font("Segoe UI", 9.5F, FontStyle.Bold), Margin = new Padding(0, 6, 0, 0) };
                pnl.Controls.Add(lbl); pnl.Controls.Add(star);
                return pnl;
            }

            // Nhân viên * (disabled text)
            flNV = MakeLabelStar("Nhân viên", true);
            txtNhanVien.Dock = DockStyle.Top; txtNhanVien.Font = inputFont;
            txtNhanVien.ReadOnly = true; txtNhanVien.Enabled = false;
            grid.Controls.Add(flNV, 0, 0);
            grid.Controls.Add(txtNhanVien, 1, 0);

            // Phiếu mượn *
            flPM = MakeLabelStar("Phiếu mượn", true);
            cbPhieuMuon.Dock = DockStyle.Top; cbPhieuMuon.DropDownStyle = ComboBoxStyle.DropDownList; cbPhieuMuon.Font = inputFont;
            grid.Controls.Add(flPM, 0, 1);
            grid.Controls.Add(cbPhieuMuon, 1, 1);

            // Lý do
            flLyDo = MakeLabelStar("Lý do", false);
            txtLyDo.Dock = DockStyle.Fill; txtLyDo.Font = inputFont; txtLyDo.Multiline = true; txtLyDo.ScrollBars = ScrollBars.Vertical;
            grid.Controls.Add(flLyDo, 0, 2);
            grid.Controls.Add(txtLyDo, 1, 2);

            // Số tiền phạt (VND) *
            flTien = MakeLabelStar("Số tiền phạt (VND)", true);
            txtSoTien.Dock = DockStyle.Top; txtSoTien.Font = inputFont;
            grid.Controls.Add(flTien, 0, 3);
            grid.Controls.Add(txtSoTien, 1, 3);

            // Actions
            actions.Dock = DockStyle.Fill;
            actions.ColumnCount = 5; actions.RowCount = 1;
            actions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            actions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20F));
            actions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 2F));
            actions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20F));
            actions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 8F));
            root.Controls.Add(actions, 0, 2);

            btnLuu.Text = "Lưu"; btnLuu.Dock = DockStyle.Fill; btnLuu.Font = new Font("Segoe UI", 10F, FontStyle.Bold); btnLuu.Enabled = false;
            btnHuy.Text = "Hủy"; btnHuy.Dock = DockStyle.Fill; btnHuy.Font = new Font("Segoe UI", 10F);
            actions.Controls.Add(btnLuu, 1, 0);
            actions.Controls.Add(btnHuy,  3, 0);

            ResumeLayout(false);
        }
    }
}
