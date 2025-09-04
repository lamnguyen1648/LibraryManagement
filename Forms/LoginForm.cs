namespace LibraryManagement.Forms
{
    public partial class LoginForm : Form
    {
        private readonly IAuthService _auth;

        public LoginForm()
        {
            InitializeComponent();

            _auth = new SqlAuthService();

            btnLogin.Click += btnLogin_Click;
            AcceptButton = btnLogin;
        }

        private void btnLogin_Click(object? sender, EventArgs e)
        {
            lblStatus.Text = string.Empty;

            var u = txtUser.Text.Trim();
            var p = txtPass.Text;

            if (string.IsNullOrWhiteSpace(u) || string.IsNullOrWhiteSpace(p))
            {
                lblStatus.Text = "Vui lòng nhập đầy đủ tài khoản và mật khẩu.";
                return;
            }

            try
            {
                if (_auth.ValidateCredentials(u, p))
                {
                    Hide();
                    using (var main = new MainForm(u))
                    {
                        main.ShowDialog();
                    }
                    Close();
                }
                else
                {
                    lblStatus.Text = "Sai thông tin đăng nhập.";
                }
            }
            catch (Exception ex)
            {
                lblStatus.Text = "Không thể đăng nhập: " + ex.Message;
            }
        }
    }
}