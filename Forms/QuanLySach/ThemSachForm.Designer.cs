using System.Drawing;
using System.Windows.Forms;

namespace LibraryManagement.Forms.QuanLySach
{
    partial class ThemSachForm
    {
        private TableLayoutPanel root;   // header, form grid, actions
        private Label lblTitle;
        private TableLayoutPanel grid;   // 2 columns: (label+*) | input
        private TableLayoutPanel actions;
        private Button btnAdd;
        private Button btnCancel;

        private void InitializeComponent()
        {
            root = new TableLayoutPanel();
            lblTitle = new Label();
            grid = new TableLayoutPanel();
            actions = new TableLayoutPanel();
            btnAdd = new Button();
            btnCancel = new Button();

            SuspendLayout();

            // Form
            AutoScaleMode = AutoScaleMode.Dpi;
            Text = "Thêm sách";
            StartPosition = FormStartPosition.CenterParent;
            MinimumSize = new Size(760, 560);

            // Root (% rows)
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
            lblTitle.Text = "Thêm sách";
            lblTitle.Dock = DockStyle.Fill;
            lblTitle.TextAlign = ContentAlignment.MiddleLeft;
            lblTitle.Font = new Font("Segoe UI", 16F, FontStyle.Bold, GraphicsUnit.Point);
            lblTitle.Padding = new Padding(8, 6, 8, 6);
            root.Controls.Add(lblTitle, 0, 0);

            // Grid: 2 columns (label+* | input). Rows are added at runtime.
            grid.Dock = DockStyle.Fill;
            grid.ColumnCount = 2;
            grid.RowCount = 0;
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35F)); // label + star
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 65F)); // input
            root.Controls.Add(grid, 0, 1);

            // Actions (Thêm / Hủy)
            actions.Dock = DockStyle.Fill;
            actions.ColumnCount = 5;
            actions.RowCount = 1;
            actions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F)); // spacer
            actions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20F)); // Thêm
            actions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 2F));  // gap
            actions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20F)); // Hủy
            actions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 8F));  // spacer

            btnAdd = new Button
            {
                Text = "Thêm",
                Dock = DockStyle.Fill,
                Enabled = false,
                Font = new Font("Segoe UI", 11F, FontStyle.Bold, GraphicsUnit.Point)
            };

            btnCancel = new Button
            {
                Text = "Hủy",
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 11F, FontStyle.Regular, GraphicsUnit.Point)
            };

            actions.Controls.Add(btnAdd, 1, 0);
            actions.Controls.Add(btnCancel, 3, 0);
            root.Controls.Add(actions, 0, 2);

            ResumeLayout(false);
        }
    }
}
