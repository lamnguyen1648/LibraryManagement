namespace LibraryManagement.Forms.QuanLyNhanVien
{
    partial class SuaNhanVienForm
    {
        private TableLayoutPanel root;   // header, grid, actions
        private Label lblTitle;
        private TableLayoutPanel grid;
        private TableLayoutPanel actions;
        private Button btnSave;
        private Button btnCancel;
        private ErrorProvider errorProvider1;

        private void InitializeComponent()
        {
            root = new TableLayoutPanel();
            lblTitle = new Label();
            grid = new TableLayoutPanel();
            actions = new TableLayoutPanel();
            btnSave = new Button();
            btnCancel = new Button();
            errorProvider1 = new ErrorProvider();

            SuspendLayout();

            AutoScaleMode = AutoScaleMode.Dpi;
            Text = "Sửa nhân viên";
            StartPosition = FormStartPosition.CenterParent;
            MinimumSize = new Size(620, 500);

            errorProvider1.BlinkStyle = ErrorBlinkStyle.NeverBlink;
            errorProvider1.ContainerControl = this;

            // Root (percent layout)
            root.Dock = DockStyle.Fill;
            root.Padding = new Padding(12);
            root.ColumnCount = 1;
            root.RowCount = 3;
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 12F)); // header
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 73F)); // dynamic grid
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 15F)); // actions
            Controls.Add(root);

            // Header
            lblTitle.Text = "Sửa nhân viên";
            lblTitle.Dock = DockStyle.Fill;
            lblTitle.TextAlign = ContentAlignment.MiddleLeft;
            lblTitle.Font = new Font("Segoe UI", 16F, FontStyle.Bold, GraphicsUnit.Point);
            lblTitle.Padding = new Padding(8, 6, 8, 6);
            root.Controls.Add(lblTitle, 0, 0);

            // Grid
            grid.Dock = DockStyle.Fill;
            grid.ColumnCount = 2;
            grid.RowCount = 0; // rows added at runtime
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35F)); // label + *
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 65F)); // input
            root.Controls.Add(grid, 0, 1);

            // Actions
            actions.Dock = DockStyle.Fill;
            actions.ColumnCount = 5;
            actions.RowCount = 1;
            actions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F)); // spacer
            actions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20F)); // Save
            actions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 2F));  // gap
            actions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20F)); // Cancel
            actions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 8F));  // spacer

            btnSave.Text = "Lưu";
            btnSave.Dock = DockStyle.Fill;
            btnSave.Enabled = false;
            btnSave.Font = new Font("Segoe UI", 11F, FontStyle.Bold, GraphicsUnit.Point);

            btnCancel.Text = "Hủy";
            btnCancel.Dock = DockStyle.Fill;
            btnCancel.Font = new Font("Segoe UI", 11F, FontStyle.Regular, GraphicsUnit.Point);

            actions.Controls.Add(btnSave, 1, 0);
            actions.Controls.Add(btnCancel, 3, 0);
            root.Controls.Add(actions, 0, 2);

            ResumeLayout(false);
        }
    }
}
