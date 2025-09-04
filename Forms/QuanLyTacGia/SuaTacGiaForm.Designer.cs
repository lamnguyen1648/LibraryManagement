namespace LibraryManagement.Forms.QuanLyTacGia
{
    partial class SuaTacGiaForm
    {
        private TableLayoutPanel root;
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
            Text = "Sửa tác giả";
            StartPosition = FormStartPosition.CenterParent;
            MinimumSize = new Size(600, 460);

            errorProvider1.BlinkStyle = ErrorBlinkStyle.NeverBlink;
            errorProvider1.ContainerControl = this;

            root.Dock = DockStyle.Fill; root.Padding = new Padding(12);
            root.ColumnCount = 1; root.RowCount = 3;
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 12F));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 73F));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 15F));
            Controls.Add(root);

            lblTitle.Text = "Sửa tác giả";
            lblTitle.Dock = DockStyle.Fill;
            lblTitle.TextAlign = ContentAlignment.MiddleLeft;
            lblTitle.Font = new Font("Segoe UI", 16F, FontStyle.Bold, GraphicsUnit.Point);
            lblTitle.Padding = new Padding(8, 6, 8, 6);
            root.Controls.Add(lblTitle, 0, 0);

            grid.Dock = DockStyle.Fill; grid.ColumnCount = 2; grid.RowCount = 0;
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35F));
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 65F));
            root.Controls.Add(grid, 0, 1);

            actions.Dock = DockStyle.Fill; actions.ColumnCount = 5; actions.RowCount = 1;
            actions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            actions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20F));
            actions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 2F));
            actions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20F));
            actions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 8F));

            btnSave.Text = "Lưu"; btnSave.Dock = DockStyle.Fill; btnSave.Enabled = false;
            btnSave.Font = new Font("Segoe UI", 11F, FontStyle.Bold, GraphicsUnit.Point);
            btnCancel.Text = "Hủy"; btnCancel.Dock = DockStyle.Fill;
            btnCancel.Font = new Font("Segoe UI", 11F, FontStyle.Regular, GraphicsUnit.Point);

            actions.Controls.Add(btnSave, 1, 0);
            actions.Controls.Add(btnCancel, 3, 0);
            root.Controls.Add(actions, 0, 2);

            ResumeLayout(false);
        }
    }
}
