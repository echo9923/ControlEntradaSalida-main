
namespace ControlEntradaSalida
{
    partial class CapturaEntradaSalida
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
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.listViewEventos = new System.Windows.Forms.ListView();
            this.colSequence = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.colEmployeeNumber = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.colEmployeeName = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.colDeviceNumber = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.colDeviceName = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.colEventType = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.colEventTime = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.colRemoteHost = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.groupBox1.SuspendLayout();
            this.SuspendLayout();
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.listViewEventos);
            this.groupBox1.Location = new System.Drawing.Point(16, 14);
            this.groupBox1.Margin = new System.Windows.Forms.Padding(4);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Padding = new System.Windows.Forms.Padding(4);
            this.groupBox1.Size = new System.Drawing.Size(1200, 354);
            this.groupBox1.TabIndex = 0;
            this.groupBox1.TabStop = false;
            // 
            // listViewEventos
            // 
            this.listViewEventos.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.colSequence,
            this.colEmployeeNumber,
            this.colEmployeeName,
            this.colDeviceNumber,
            this.colDeviceName,
            this.colEventType,
            this.colEventTime,
            this.colRemoteHost});
            this.listViewEventos.FullRowSelect = true;
            this.listViewEventos.HideSelection = false;
            this.listViewEventos.Location = new System.Drawing.Point(8, 22);
            this.listViewEventos.Margin = new System.Windows.Forms.Padding(4);
            this.listViewEventos.Name = "listViewEventos";
            this.listViewEventos.Size = new System.Drawing.Size(1182, 312);
            this.listViewEventos.TabIndex = 0;
            this.listViewEventos.UseCompatibleStateImageBehavior = false;
            this.listViewEventos.View = System.Windows.Forms.View.Details;
            this.listViewEventos.SelectedIndexChanged += new System.EventHandler(this.listViewEventos_SelectedIndexChanged);
            // 
            // colSequence
            // 
            this.colSequence.Text = "\u5e8f\u53f7";
            this.colSequence.Width = 80;
            // 
            // colEmployeeNumber
            // 
            this.colEmployeeNumber.Text = "\u5de5\u53f7";
            this.colEmployeeNumber.Width = 100;
            // 
            // colEmployeeName
            // 
            this.colEmployeeName.Text = "\u59d3\u540d";
            this.colEmployeeName.Width = 140;
            // 
            // colDeviceNumber
            // 
            this.colDeviceNumber.Text = "\u8bbe\u5907\u7f16\u53f7";
            this.colDeviceNumber.Width = 110;
            // 
            // colDeviceName
            // 
            this.colDeviceName.Text = "\u8bbe\u5907\u540d\u79f0";
            this.colDeviceName.Width = 140;
            // 
            // colEventType
            // 
            this.colEventType.Text = "\u4e8b\u4ef6\u7c7b\u578b";
            this.colEventType.Width = 170;
            // 
            // colEventTime
            // 
            this.colEventTime.Text = "\u65f6\u95f4";
            this.colEventTime.Width = 180;
            // 
            // colRemoteHost
            // 
            this.colRemoteHost.Text = "\u8fdc\u7a0b\u4e3b\u673a\u5730\u5740";
            this.colRemoteHost.Width = 160;
            // 
            // CapturaEntradaSalida
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1232, 384);
            this.Controls.Add(this.groupBox1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.Margin = new System.Windows.Forms.Padding(4);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "CapturaEntradaSalida";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "\u8fdb\u51fa\u4e8b\u4ef6";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.GestionEventos_FormClosing);
            this.Load += new System.EventHandler(this.GestionEventos_Load);
            this.groupBox1.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.ListView listViewEventos;
        private System.Windows.Forms.ColumnHeader colSequence;
        private System.Windows.Forms.ColumnHeader colEmployeeNumber;
        private System.Windows.Forms.ColumnHeader colEmployeeName;
        private System.Windows.Forms.ColumnHeader colDeviceNumber;
        private System.Windows.Forms.ColumnHeader colDeviceName;
        private System.Windows.Forms.ColumnHeader colEventType;
        private System.Windows.Forms.ColumnHeader colEventTime;
        private System.Windows.Forms.ColumnHeader colRemoteHost;
    }
}

