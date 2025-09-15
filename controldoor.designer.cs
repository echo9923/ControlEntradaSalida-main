
namespace ControlEntradaSalida
{
    partial class controldoor
    {
        /// <summary>
        ///必需的设计器变量。
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// 清理所有正在使用的资源。
        /// </summary>
        /// <param name="disposing">如果应释放托管资源，为 true；否则为 false。</param>

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
            this.btnOpen = new System.Windows.Forms.Button();
            this.btnClose = new System.Windows.Forms.Button();
            this.btnStayOpen = new System.Windows.Forms.Button();
            this.btnStayClose = new System.Windows.Forms.Button();
            this.lblDevice = new System.Windows.Forms.Label();
            this.cmbDevices = new System.Windows.Forms.ComboBox();
            this.SuspendLayout();
            // 
            // btnOpen
            // 
            this.btnOpen.Location = new System.Drawing.Point(197, 64);
            this.btnOpen.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.btnOpen.Name = "btnOpen";
            this.btnOpen.Size = new System.Drawing.Size(137, 30);
            this.btnOpen.TabIndex = 61;
            this.btnOpen.Text = "开门";
            this.btnOpen.UseVisualStyleBackColor = true;
            this.btnOpen.Click += new System.EventHandler(this.btnOpen_Click);
            // 
            // btnClose
            // 
            this.btnClose.Location = new System.Drawing.Point(197, 114);
            this.btnClose.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.btnClose.Name = "btnClose";
            this.btnClose.Size = new System.Drawing.Size(137, 30);
            this.btnClose.TabIndex = 63;
            this.btnClose.Text = "关门";
            this.btnClose.UseVisualStyleBackColor = true;
            this.btnClose.Click += new System.EventHandler(this.btnClose_Click);
            // 
            // btnStayOpen
            // 
            this.btnStayOpen.Location = new System.Drawing.Point(197, 164);
            this.btnStayOpen.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.btnStayOpen.Name = "btnStayOpen";
            this.btnStayOpen.Size = new System.Drawing.Size(137, 30);
            this.btnStayOpen.TabIndex = 65;
            this.btnStayOpen.Text = "常开";
            this.btnStayOpen.UseVisualStyleBackColor = true;
            this.btnStayOpen.Click += new System.EventHandler(this.btnStayOpen_Click);
            // 
            // btnStayClose
            // 
            this.btnStayClose.Location = new System.Drawing.Point(197, 215);
            this.btnStayClose.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.btnStayClose.Name = "btnStayClose";
            this.btnStayClose.Size = new System.Drawing.Size(137, 30);
            this.btnStayClose.TabIndex = 67;
            this.btnStayClose.Text = "常闭";
            this.btnStayClose.UseVisualStyleBackColor = true;
            this.btnStayClose.Click += new System.EventHandler(this.btnStayClose_Click);
            // 
            // lblDevice
            // 
            this.lblDevice.AutoSize = true;
            this.lblDevice.Location = new System.Drawing.Point(40, 19);
            this.lblDevice.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lblDevice.Name = "lblDevice";
            this.lblDevice.Size = new System.Drawing.Size(75, 15);
            this.lblDevice.TabIndex = 68;
            this.lblDevice.Text = "选择设备:";
            // 
            // cmbDevices
            // 
            this.cmbDevices.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbDevices.FormattingEnabled = true;
            this.cmbDevices.Location = new System.Drawing.Point(135, 15);
            this.cmbDevices.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.cmbDevices.Name = "cmbDevices";
            this.cmbDevices.Size = new System.Drawing.Size(199, 23);
            this.cmbDevices.TabIndex = 69;
            this.cmbDevices.SelectedIndexChanged += new System.EventHandler(this.cmbDevices_SelectedIndexChanged);
            // 
            // controldoor
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(577, 324);
            this.Controls.Add(this.btnStayClose);
            this.Controls.Add(this.btnStayOpen);
            this.Controls.Add(this.btnClose);
            this.Controls.Add(this.btnOpen);
            this.Controls.Add(this.cmbDevices);
            this.Controls.Add(this.lblDevice);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "controldoor";
            this.ShowIcon = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "远程控门";
            this.Load += new System.EventHandler(this.controldoor_Load_1);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button btnOpen;
        private System.Windows.Forms.Button btnClose;
        private System.Windows.Forms.Button btnStayOpen;
        private System.Windows.Forms.Button btnStayClose;
        private System.Windows.Forms.Label lblDevice;
        private System.Windows.Forms.ComboBox cmbDevices;
    }
}