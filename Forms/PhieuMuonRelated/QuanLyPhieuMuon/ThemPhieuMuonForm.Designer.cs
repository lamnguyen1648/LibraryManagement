using System.Drawing;
using System.Windows.Forms;

namespace LibraryManagement.Forms.QuanLyPhieuMuon
{
    partial class ThemPhieuMuonForm
    {
        private TableLayoutPanel root;
        private Label lblTitle;

        private TableLayoutPanel grid;
        private FlowLayoutPanel flDocGia;
        private ComboBox cbDocGia;

        private FlowLayoutPanel flNgayMuon;
        private DateTimePicker dtpNgayMuon;

        private FlowLayoutPanel flNgayTra;
        private DateTimePicker dtpNgayTra;

        private FlowLayoutPanel flTinhTrang;
        private ComboBox cbTinhTrang;

        private FlowLayoutPanel flSach;
        private CheckedListBox clbSach;

        private TableLayoutPanel actions;
        private Button btnLuu;
        private Button btnHuy;

        private void InitializeComponent()
        {
            root = new TableLayoutPanel();
            lblTitle = new Label();
            grid = new TableLayoutPanel();

            flDocGia = new FlowLayoutPanel();
            cbDocGia = new ComboBox();

            flNgayMuon = new FlowLayoutPanel();
            dtpNgayMuon = new DateTimePicker();

            flNgayTra = new FlowLayoutPanel();
            dtpNgayTra = new DateTimePicker();

            flTinhTrang = new FlowLayoutPanel();
            cbTinhTrang = new ComboBox();

            flSach = new FlowLayoutPanel();
            clbSach = new CheckedListBox();

            actions = new TableLayoutPanel();
            btnLuu = new Button();
            btnHuy = new Button();

            SuspendLayout();

            // Form
            AutoScaleMode = AutoScaleMode.Dpi;
            Text = "Thêm phiếu mượn";
            MinimumSize = new Size(820, 620);

            // Root (12% header, 73% grid, 15% actions)
            root.Dock = DockStyle.Fill;
            root.Padding = new Padding(12);
            root.ColumnCount = 1; root.RowCount = 3;
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 12F));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 73F));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 15F));
            Controls.Add(root);

            // Header (keep big font)
            lblTitle.Text = "Thêm phiếu mượn";
            lblTitle.Dock = DockStyle.Fill;
            lblTitle.TextAlign = ContentAlignment.MiddleLeft;
            lblTitle.Font = new Font("Segoe UI", 16F, FontStyle.Bold);
            lblTitle.Padding = new Padding(8, 6, 8, 6);
            root.Controls.Add(lblTitle, 0, 0);

            // Grid (35% label | 65% input)
            grid.Dock = DockStyle.Fill;
            grid.ColumnCount = 2;
            grid.RowCount = 5; // + Sách (multi-select)
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35F));
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 65F));
            grid.RowStyles.Add(new RowStyle(SizeType.Percent, 16F));
            grid.RowStyles.Add(new RowStyle(SizeType.Percent, 16F));
            grid.RowStyles.Add(new RowStyle(SizeType.Percent, 16F));
            grid.RowStyles.Add(new RowStyle(SizeType.Percent, 16F));
            grid.RowStyles.Add(new RowStyle(SizeType.Percent, 36F)); // books list taller
            root.Controls.Add(grid, 0, 1);

            // Common fonts (10F for everything except heading)
            var labelFont = new Font("Segoe UI", 10F);
            var inputFont = new Font("Segoe UI", 10F);

            // Helper to make "Label + *"
            FlowLayoutPanel MakeLabelStar(string text, bool required)
            {
                var pnl = new FlowLayoutPanel
                {
                    Dock = DockStyle.Fill, WrapContents = false, AutoSize = false,
                    Padding = new Padding(6, 0, 0, 0), Margin = new Padding(0)
                };
                var lbl = new Label { AutoSize = true, Text = text, Font = labelFont };
                var star = new Label { AutoSize = true, Text = required ? "*" : "", ForeColor = Color.Firebrick, Font = new Font("Segoe UI", 9.5F, FontStyle.Bold) };
                lbl.Margin = new Padding(0, 6, 2, 0);
                star.Margin = new Padding(0, 6, 0, 0);
                pnl.Controls.Add(lbl); pnl.Controls.Add(star);
                return pnl;
            }

            // Độc giả *
            flDocGia = MakeLabelStar("Độc giả", true);
            cbDocGia.Dock = DockStyle.Top; cbDocGia.DropDownStyle = ComboBoxStyle.DropDownList; cbDocGia.Font = inputFont;
            grid.Controls.Add(flDocGia, 0, 0);
            grid.Controls.Add(cbDocGia, 1, 0);

            // Ngày mượn *
            flNgayMuon = MakeLabelStar("Ngày mượn", true);
            dtpNgayMuon.Dock = DockStyle.Top; dtpNgayMuon.Font = inputFont;
            dtpNgayMuon.Format = DateTimePickerFormat.Custom; dtpNgayMuon.CustomFormat = "dd/MM/yyyy";
            grid.Controls.Add(flNgayMuon, 0, 1);
            grid.Controls.Add(dtpNgayMuon, 1, 1);

            // Ngày trả (optional)
            flNgayTra = MakeLabelStar("Ngày trả", false);
            dtpNgayTra.Dock = DockStyle.Top; dtpNgayTra.Font = inputFont;
            dtpNgayTra.ShowCheckBox = true; dtpNgayTra.Format = DateTimePickerFormat.Custom; dtpNgayTra.CustomFormat = "dd/MM/yyyy";
            grid.Controls.Add(flNgayTra, 0, 2);
            grid.Controls.Add(dtpNgayTra, 1, 2);

            // Tình trạng (dropdown)
            flTinhTrang = MakeLabelStar("Tình trạng", false);
            cbTinhTrang.Dock = DockStyle.Top; cbTinhTrang.DropDownStyle = ComboBoxStyle.DropDownList; cbTinhTrang.Font = inputFont;
            grid.Controls.Add(flTinhTrang, 0, 3);
            grid.Controls.Add(cbTinhTrang, 1, 3);

            // Sách (multi-select, required)
            flSach = MakeLabelStar("Sách mượn (chọn nhiều)", true);
            clbSach.Dock = DockStyle.Fill;
            clbSach.CheckOnClick = true;
            clbSach.IntegralHeight = false;
            clbSach.Font = inputFont;
            grid.Controls.Add(flSach, 0, 4);
            grid.Controls.Add(clbSach, 1, 4);

            // Actions (centered, 10F)
            actions.Dock = DockStyle.Fill;
            actions.ColumnCount = 5; actions.RowCount = 1;
            actions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            actions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20F));
            actions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 2F));
            actions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20F));
            actions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 8F));
            root.Controls.Add(actions, 0, 2);

            btnLuu.Text = "Lưu"; btnLuu.Dock = DockStyle.Fill; btnLuu.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            btnHuy.Text = "Hủy"; btnHuy.Dock = DockStyle.Fill; btnHuy.Font = new Font("Segoe UI", 10F);
            actions.Controls.Add(btnLuu, 1, 0);
            actions.Controls.Add(btnHuy,  3, 0);

            ResumeLayout(false);
        }
    }
}
