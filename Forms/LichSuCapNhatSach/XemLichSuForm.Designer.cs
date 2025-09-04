// XemChiTietForm.Designer.cs
namespace LibraryManagement.Forms.LichSuCapNhatSach
{
    partial class XemChiTietForm
    {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.TableLayoutPanel root;
        private System.Windows.Forms.TableLayoutPanel grid;
        private System.Windows.Forms.TableLayoutPanel actions;

        private System.Windows.Forms.Label cap_NhanVien;
        private System.Windows.Forms.Label cap_TenSach;
        private System.Windows.Forms.Label cap_NgayCapNhat;
        private System.Windows.Forms.Label cap_HinhThuc;
        private System.Windows.Forms.Label cap_ChiTiet;

        private System.Windows.Forms.Label lblNhanVien;
        private System.Windows.Forms.Label lblTenSach;
        private System.Windows.Forms.Label lblNgayCapNhat;
        private System.Windows.Forms.Label lblHinhThuc;

        private System.Windows.Forms.Panel panelChiTietScroll;
        private System.Windows.Forms.Label lblChiTiet;
        private System.Windows.Forms.Panel spacerRow; // % spacer row

        /// <inheritdoc />
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();

            // ====== Form ======
            this.Text = "Xem chi tiết cập nhật";
            this.MinimumSize = new System.Drawing.Size(800, 520);
            this.Font = new System.Drawing.Font("Segoe UI", 10F);

            // ====== root (2 rows: grid 92%, actions 8%) ======
            root = new System.Windows.Forms.TableLayoutPanel();
            root.Dock = System.Windows.Forms.DockStyle.Fill;
            root.ColumnCount = 1;
            root.RowCount = 2;
            root.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            root.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 92F));
            root.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 8F));
            this.Controls.Add(root);

            // ====== grid (labels/values) ======
            grid = new System.Windows.Forms.TableLayoutPanel();
            grid.Dock = System.Windows.Forms.DockStyle.Fill;
            grid.ColumnCount = 2;
            grid.RowCount = 6; // 0..3 fields, 4 spacer, 5 details
            grid.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 30F)); // label
            grid.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 70F)); // value
            // uniform rows + a % spacer + big details
            grid.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 11F)); // 0 Nhân viên
            grid.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 11F)); // 1 Tên sách
            grid.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 11F)); // 2 Ngày cập nhật
            grid.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 11F)); // 3 Hình thức
            grid.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 4F));  // 4 spacer
            grid.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 52F)); // 5 Chi tiết cập nhật
            root.Controls.Add(grid, 0, 0);

            // ====== captions ======
            cap_NhanVien    = MakeCaption("Nhân viên");
            cap_TenSach     = MakeCaption("Tên sách");
            cap_NgayCapNhat = MakeCaption("Ngày cập nhật");
            cap_HinhThuc    = MakeCaption("Hình thức");
            cap_ChiTiet     = MakeCaption("Chi tiết cập nhật");
            cap_ChiTiet.TextAlign = System.Drawing.ContentAlignment.TopLeft;

            // ====== value labels (consistent margins) ======
            lblNhanVien    = MakeValueLabel();
            lblTenSach     = MakeValueLabel();
            lblNgayCapNhat = MakeValueLabel();
            lblHinhThuc    = MakeValueLabel();

            // ====== % spacer row between Hình thức and Chi tiết ======
            spacerRow = new System.Windows.Forms.Panel();
            spacerRow.Dock = System.Windows.Forms.DockStyle.Fill;
            spacerRow.Margin = new System.Windows.Forms.Padding(0); // no extra px margins
            // span both columns to look like a true row gap
            grid.Controls.Add(spacerRow, 0, 4);
            grid.SetColumnSpan(spacerRow, 2);

            // ====== long detail container ======
            panelChiTietScroll = new System.Windows.Forms.Panel();
            panelChiTietScroll.Dock = System.Windows.Forms.DockStyle.Fill;
            panelChiTietScroll.AutoScroll = true;
            panelChiTietScroll.Margin = new System.Windows.Forms.Padding(0, 6, 6, 6); // same top margin as other value cells

            lblChiTiet = new System.Windows.Forms.Label();
            lblChiTiet.AutoSize = true;
            lblChiTiet.Dock = System.Windows.Forms.DockStyle.Top; // start from very top of the detail cell
            lblChiTiet.MaximumSize = new System.Drawing.Size(0, 0);
            lblChiTiet.Margin = new System.Windows.Forms.Padding(0);
            lblChiTiet.Padding = new System.Windows.Forms.Padding(0);
            lblChiTiet.TextAlign = System.Drawing.ContentAlignment.TopLeft;

            panelChiTietScroll.Controls.Add(lblChiTiet);

            // ====== add to grid ======
            AddRow(0, cap_NhanVien,    lblNhanVien);
            AddRow(1, cap_TenSach,     lblTenSach);
            AddRow(2, cap_NgayCapNhat, lblNgayCapNhat);
            AddRow(3, cap_HinhThuc,    lblHinhThuc);
            // row 5: caption + scroll panel
            grid.Controls.Add(cap_ChiTiet,        0, 5);
            grid.Controls.Add(panelChiTietScroll, 1, 5);

            // ====== actions (right-aligned Close) ======
            actions = new System.Windows.Forms.TableLayoutPanel();
            actions.Dock = System.Windows.Forms.DockStyle.Fill;
            actions.ColumnCount = 2;
            actions.RowCount = 1;
            actions.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 70F));
            actions.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 30F));
            actions.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            root.Controls.Add(actions, 0, 1);

            var actionRight = new System.Windows.Forms.TableLayoutPanel();
            actionRight.Dock = System.Windows.Forms.DockStyle.Fill;
            actionRight.ColumnCount = 3;
            actionRight.RowCount = 1;
            actionRight.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 60F));
            actionRight.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 20F));
            actionRight.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 20F));
            actionRight.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));

            actions.Controls.Add(new System.Windows.Forms.Panel(), 0, 0);
            actions.Controls.Add(actionRight, 1, 0);
            actionRight.Controls.Add(new System.Windows.Forms.Panel(), 0, 0);
            actionRight.Controls.Add(new System.Windows.Forms.Panel(), 1, 0);
        }

        // Helpers — unified margins so every row looks the same
        private System.Windows.Forms.Label MakeCaption(string text)
        {
            var lbl = new System.Windows.Forms.Label();
            lbl.Text = text;
            lbl.Dock = System.Windows.Forms.DockStyle.Fill;
            lbl.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            lbl.Margin = new System.Windows.Forms.Padding(6, 6, 6, 6);
            return lbl;
        }

        private System.Windows.Forms.Label MakeValueLabel()
        {
            var lbl = new System.Windows.Forms.Label();
            lbl.Dock = System.Windows.Forms.DockStyle.Fill;
            lbl.AutoEllipsis = true;
            lbl.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            lbl.Margin = new System.Windows.Forms.Padding(0, 6, 6, 6);
            return lbl;
        }

        private void AddRow(int rowIndex, System.Windows.Forms.Control cap, System.Windows.Forms.Control val)
        {
            grid.Controls.Add(cap, 0, rowIndex);
            grid.Controls.Add(val, 1, rowIndex);
        }

        #endregion
    }
}
