namespace LibraryManagement.Forms.PhieuMuonRelated.QuanLyPhieuMuon
{
    partial class ChiTietPhieuMuonForm
    {
        private TableLayoutPanel root;
        private Label lblTitle;

        private TableLayoutPanel grid;

        private Label lblDocGia;
        private Label lblDocGiaValue;

        private Label lblNgayMuon;
        private Label lblNgayMuonValue;

        private Label lblNgayTra;
        private Label lblNgayTraValue;

        private Label lblTinhTrang;
        private Label lblTinhTrangValue;

        private Label lblSach;
        private ListBox lbSach;
        private Label lblTongSach;

        private void InitializeComponent()
        {
            root = new TableLayoutPanel();
            lblTitle = new Label();
            grid = new TableLayoutPanel();

            lblDocGia = new Label();
            lblDocGiaValue = new Label();

            lblNgayMuon = new Label();
            lblNgayMuonValue = new Label();

            lblNgayTra = new Label();
            lblNgayTraValue = new Label();

            lblTinhTrang = new Label();
            lblTinhTrangValue = new Label();

            lblSach = new Label();
            lbSach = new ListBox();
            lblTongSach = new Label();

            SuspendLayout();

            // Form
            AutoScaleMode = AutoScaleMode.Dpi;
            Text = "Chi tiết phiếu mượn";
            MinimumSize = new Size(820, 560);

            // Root: ONLY header + grid (no footer)
            root.Dock = DockStyle.Fill;
            root.Padding = new Padding(12);
            root.ColumnCount = 1; root.RowCount = 2;
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 12F));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 88F));
            Controls.Add(root);

            // Header
            lblTitle.Text = "Chi tiết phiếu mượn";
            lblTitle.Dock = DockStyle.Fill;
            lblTitle.TextAlign = ContentAlignment.MiddleLeft;
            lblTitle.Font = new Font("Segoe UI", 16F, FontStyle.Bold);
            lblTitle.Padding = new Padding(8, 6, 8, 6);
            root.Controls.Add(lblTitle, 0, 0);

            // Grid (35% label | 65% value)
            grid.Dock = DockStyle.Fill;
            grid.ColumnCount = 2;
            grid.RowCount = 5; // no Mã phiếu row
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35F));
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 65F));
            grid.RowStyles.Add(new RowStyle(SizeType.Percent, 17F));
            grid.RowStyles.Add(new RowStyle(SizeType.Percent, 17F));
            grid.RowStyles.Add(new RowStyle(SizeType.Percent, 17F));
            grid.RowStyles.Add(new RowStyle(SizeType.Percent, 17F));
            grid.RowStyles.Add(new RowStyle(SizeType.Percent, 32F)); // books taller
            root.Controls.Add(grid, 0, 1);

            var labelFont = new Font("Segoe UI", 10F);
            var valueFont = new Font("Segoe UI", 10F);

            void StyleLabel(Label l)
            {
                l.Dock = DockStyle.Fill;
                l.TextAlign = ContentAlignment.MiddleLeft;
                l.Font = labelFont;
            }
            void StyleValue(Label l)
            {
                l.Dock = DockStyle.Fill;
                l.TextAlign = ContentAlignment.MiddleLeft;
                l.Font = valueFont;
                l.AutoEllipsis = true;
                l.BorderStyle = BorderStyle.None; // ← no border
                l.Padding = new Padding(6, 0, 0, 0);
            }

            // Độc giả
            lblDocGia.Text = "Độc giả"; StyleLabel(lblDocGia);
            StyleValue(lblDocGiaValue);
            grid.Controls.Add(lblDocGia, 0, 0);
            grid.Controls.Add(lblDocGiaValue, 1, 0);

            // Ngày mượn
            lblNgayMuon.Text = "Ngày mượn"; StyleLabel(lblNgayMuon);
            StyleValue(lblNgayMuonValue);
            grid.Controls.Add(lblNgayMuon, 0, 1);
            grid.Controls.Add(lblNgayMuonValue, 1, 1);

            // Ngày trả
            lblNgayTra.Text = "Ngày trả"; StyleLabel(lblNgayTra);
            StyleValue(lblNgayTraValue);
            grid.Controls.Add(lblNgayTra, 0, 2);
            grid.Controls.Add(lblNgayTraValue, 1, 2);

            // Tình trạng
            lblTinhTrang.Text = "Tình trạng"; StyleLabel(lblTinhTrang);
            StyleValue(lblTinhTrangValue);
            grid.Controls.Add(lblTinhTrang, 0, 3);
            grid.Controls.Add(lblTinhTrangValue, 1, 3);

            // Sách
            lblSach.Text = "Sách mượn"; StyleLabel(lblSach);

            lbSach.Dock = DockStyle.Fill;
            lbSach.Font = valueFont;
            lbSach.SelectionMode = SelectionMode.None;
            lbSach.IntegralHeight = false;
            // Panel contains list + total
            var booksPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
            };
            booksPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 88F));
            booksPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 12F));

            lblTongSach.Text = "Tổng số sách: 0";
            lblTongSach.Dock = DockStyle.Fill;
            lblTongSach.TextAlign = ContentAlignment.MiddleRight;
            lblTongSach.Font = valueFont;

            booksPanel.Controls.Add(lbSach, 0, 0);
            booksPanel.Controls.Add(lblTongSach, 0, 1);

            grid.Controls.Add(lblSach, 0, 4);
            grid.Controls.Add(booksPanel, 1, 4);

            ResumeLayout(false);
        }
    }
}
