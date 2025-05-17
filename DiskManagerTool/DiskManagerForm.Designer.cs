namespace DiskManagerTool
{
    partial class DiskManagerForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            dgvDiskler = new DataGridView();
            statusStrip1 = new StatusStrip();
            lblDiskSayisi = new ToolStripStatusLabel();
            lblNotification = new ToolStripStatusLabel();
            timerNotification = new System.Windows.Forms.Timer(components);
            toolStrip1 = new ToolStrip();
            btnDisklerExport = new ToolStripButton();
            ((System.ComponentModel.ISupportInitialize)dgvDiskler).BeginInit();
            statusStrip1.SuspendLayout();
            toolStrip1.SuspendLayout();
            SuspendLayout();
            // 
            // dgvDiskler
            // 
            dgvDiskler.AllowUserToAddRows = false;
            dgvDiskler.AllowUserToDeleteRows = false;
            dgvDiskler.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            dgvDiskler.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgvDiskler.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgvDiskler.Location = new Point(14, 32);
            dgvDiskler.Margin = new Padding(4, 3, 4, 3);
            dgvDiskler.Name = "dgvDiskler";
            dgvDiskler.ReadOnly = true;
            dgvDiskler.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvDiskler.Size = new Size(905, 447);
            dgvDiskler.TabIndex = 0;
            dgvDiskler.CellContentClick += DgvDiskler_CellContentClick;
            
            // 
            // statusStrip1
            // 
            statusStrip1.Items.AddRange(new ToolStripItem[] { lblDiskSayisi, lblNotification });
            statusStrip1.Location = new Point(0, 497);
            statusStrip1.Name = "statusStrip1";
            statusStrip1.Padding = new Padding(1, 0, 16, 0);
            statusStrip1.Size = new Size(933, 22);
            statusStrip1.TabIndex = 1;
            statusStrip1.Text = "statusStrip1";
            // 
            // lblDiskSayisi
            // 
            lblDiskSayisi.Name = "lblDiskSayisi";
            lblDiskSayisi.Size = new Size(108, 17);
            lblDiskSayisi.Text = "Toplam 0 disk bağlı";
            // 
            // lblNotification
            // 
            lblNotification.ForeColor = Color.Green;
            lblNotification.Name = "lblNotification";
            lblNotification.Size = new Size(808, 17);
            lblNotification.Spring = true;
            lblNotification.TextAlign = ContentAlignment.MiddleRight;
            // 
            // timerNotification
            // 
            timerNotification.Interval = 3000;
            timerNotification.Tick += timerNotification_Tick;
            // 
            // toolStrip1
            // 
            toolStrip1.Items.AddRange(new ToolStripItem[] { btnDisklerExport });
            toolStrip1.Location = new Point(0, 0);
            toolStrip1.Name = "toolStrip1";
            toolStrip1.Size = new Size(933, 25);
            toolStrip1.TabIndex = 2;
            toolStrip1.Text = "toolStrip1";
            // 
            // btnDisklerExport
            // 
            btnDisklerExport.DisplayStyle = ToolStripItemDisplayStyle.Text;
            btnDisklerExport.ImageTransparentColor = Color.Magenta;
            btnDisklerExport.Name = "btnDisklerExport";
            btnDisklerExport.Size = new Size(149, 22);
            btnDisklerExport.Text = "Diskleri CSV Olarak Kaydet";
            btnDisklerExport.Click += btnDisklerExport_Click;
            // 
            // DiskManagerForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(933, 519);
            Controls.Add(toolStrip1);
            Controls.Add(dgvDiskler);
            Controls.Add(statusStrip1);
            Margin = new Padding(4, 3, 4, 3);
            Name = "DiskManagerForm";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Disk Yönetim Sistemi";
            ((System.ComponentModel.ISupportInitialize)dgvDiskler).EndInit();
            statusStrip1.ResumeLayout(false);
            statusStrip1.PerformLayout();
            toolStrip1.ResumeLayout(false);
            toolStrip1.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion
        private System.Windows.Forms.DataGridView dgvDiskler;
        private System.Windows.Forms.StatusStrip statusStrip1;
        private System.Windows.Forms.ToolStripStatusLabel lblDiskSayisi;
        private System.Windows.Forms.ToolStripStatusLabel lblNotification;
        private System.Windows.Forms.Timer timerNotification;
        private System.Windows.Forms.ToolStrip toolStrip1;
        private System.Windows.Forms.ToolStripButton btnDisklerExport;
    }
}