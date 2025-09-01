namespace LibraryManagement.Forms.Operations
{
    partial class LoginForm
    {
        private TableLayoutPanel outer;
        private TableLayoutPanel grid;

        private Label lblTitle;
        private Label lblUser;
        private Label lblPass;
        private Label lblStatus;

        private TextBox txtUser;
        private TextBox txtPass;
        private Button btnLogin;

        private void InitializeComponent()
        {
            outer = new TableLayoutPanel();
            grid = new TableLayoutPanel();
            lblTitle = new Label();
            lblUser = new Label();
            lblPass = new Label();
            lblStatus = new Label();
            txtUser = new TextBox();
            txtPass = new TextBox();
            btnLogin = new Button();

            SuspendLayout();

            // Form
            AutoScaleMode = AutoScaleMode.Dpi;
            StartPosition = FormStartPosition.CenterScreen;
            MinimumSize = new Size(520, 320);
            Text = "Đăng nhập";

            // Outer layout: 100% width/height with % rows
            outer.Dock = DockStyle.Fill;
            outer.Padding = new Padding(12);
            outer.ColumnCount = 1;
            outer.RowCount = 3;
            outer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            outer.RowStyles.Add(new RowStyle(SizeType.Percent, 20F)); // top: title
            outer.RowStyles.Add(new RowStyle(SizeType.Percent, 60F)); // middle: form
            outer.RowStyles.Add(new RowStyle(SizeType.Percent, 20F)); // bottom: spacing

            // Title
            lblTitle.Text = "Đăng nhập";
            lblTitle.Dock = DockStyle.Fill;
            lblTitle.TextAlign = ContentAlignment.MiddleCenter;
            lblTitle.Font = new Font("Segoe UI", 14F, FontStyle.Bold, GraphicsUnit.Point);

            // Grid for inputs/buttons/status using % everywhere
            grid.Dock = DockStyle.Fill;
            grid.ColumnCount = 2;
            grid.RowCount = 4;
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30F)); // labels
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 70F)); // inputs
            grid.RowStyles.Add(new RowStyle(SizeType.Percent, 25F)); // username
            grid.RowStyles.Add(new RowStyle(SizeType.Percent, 25F)); // password
            grid.RowStyles.Add(new RowStyle(SizeType.Percent, 25F)); // button
            grid.RowStyles.Add(new RowStyle(SizeType.Percent, 25F)); // status

            // Username
            lblUser.Text = "Tên nhân viên";
            lblUser.Dock = DockStyle.Fill;
            lblUser.TextAlign = ContentAlignment.MiddleLeft;

            txtUser.Dock = DockStyle.Fill;
            txtUser.PlaceholderText = "Nhập tên nhân viên";

            // Password
            lblPass.Text = "Mật khẩu";
            lblPass.Dock = DockStyle.Fill;
            lblPass.TextAlign = ContentAlignment.MiddleLeft;

            txtPass.Dock = DockStyle.Fill;
            txtPass.UseSystemPasswordChar = true;
            txtPass.PlaceholderText = "Nhập mật khẩu";

            btnLogin.Text = "Đăng nhập";
            btnLogin.Dock = DockStyle.Fill;

            lblStatus.Text = "";
            lblStatus.Dock = DockStyle.Fill;
            lblStatus.TextAlign = ContentAlignment.MiddleCenter;
            lblStatus.ForeColor = Color.Firebrick;

            grid.Controls.Add(lblUser, 0, 0);
            grid.Controls.Add(txtUser, 1, 0);
            grid.Controls.Add(lblPass, 0, 1);
            grid.Controls.Add(txtPass, 1, 1);

            grid.Controls.Add(btnLogin, 0, 2);
            grid.SetColumnSpan(btnLogin, 2);

            grid.Controls.Add(lblStatus, 0, 3);
            grid.SetColumnSpan(lblStatus, 2);

            outer.Controls.Add(lblTitle, 0, 0);
            outer.Controls.Add(grid, 0, 1);

            Controls.Add(outer);

            ResumeLayout(false);
        }
    }
}
