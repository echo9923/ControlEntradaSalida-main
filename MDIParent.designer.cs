
namespace ControlEntradaSalida
{
    partial class MDIParent
    {
        /// <summary>
        /// Variable del diseñador necesaria.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Limpiar los recursos que se estén usando.
        /// </summary>
        /// <param name="disposing">true si los recursos administrados se deben desechar; false en caso contrario.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Código generado por el Diseñador de Windows Forms

        /// <summary>
        /// Método necesario para admitir el Diseñador. No se puede modificar
        /// el contenido de este método con el editor de código.
        /// </summary>
        private void InitializeComponent()
        {
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.generalToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.gesti\u00f3nDeDispositivosToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.gestionDeEmpleadosToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem2 = new System.Windows.Forms.ToolStripSeparator();
            this.consultarDatosDispositivoToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.refreshPermissionsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem3 = new System.Windows.Forms.ToolStripSeparator();
            this.salirToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.SuspendLayout();
            // 
            // menuStrip1
            // 
            this.menuStrip1.ImageScalingSize = new System.Drawing.Size(20, 20);
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.generalToolStripMenuItem});
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Padding = new System.Windows.Forms.Padding(4, 2, 0, 2);
            this.menuStrip1.Size = new System.Drawing.Size(800, 25);
            this.menuStrip1.TabIndex = 1;
            this.menuStrip1.Text = "\u4e3b\u83dc\u5355";
            this.menuStrip1.ItemClicked += new System.Windows.Forms.ToolStripItemClickedEventHandler(this.menuStrip1_ItemClicked);
            // 
            // generalToolStripMenuItem
            // 
            this.generalToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.gesti\u00f3nDeDispositivosToolStripMenuItem,
            this.gestionDeEmpleadosToolStripMenuItem,
            this.toolStripMenuItem2,
            this.consultarDatosDispositivoToolStripMenuItem,
            this.refreshPermissionsToolStripMenuItem,
            this.toolStripMenuItem3,
            this.salirToolStripMenuItem});
            this.generalToolStripMenuItem.Name = "generalToolStripMenuItem";
            this.generalToolStripMenuItem.Size = new System.Drawing.Size(44, 21);
            this.generalToolStripMenuItem.Text = "\u7cfb\u7edf\u7ba1\u7406";
            // 
            // gesti\u00f3nDeDispositivosToolStripMenuItem
            // 
            this.gesti\u00f3nDeDispositivosToolStripMenuItem.Name = "gesti\u00f3nDeDispositivosToolStripMenuItem";
            this.gesti\u00f3nDeDispositivosToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
            this.gesti\u00f3nDeDispositivosToolStripMenuItem.Text = "\u8bbe\u5907\u7ba1\u7406";
            this.gesti\u00f3nDeDispositivosToolStripMenuItem.Click += new System.EventHandler(this.gesti\u00f3nDeDispositivosToolStripMenuItem_Click);
            // 
            // 
            // gestionDeEmpleadosToolStripMenuItem
            // 
            this.gestionDeEmpleadosToolStripMenuItem.Name = "gestionDeEmpleadosToolStripMenuItem";
            this.gestionDeEmpleadosToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
            this.gestionDeEmpleadosToolStripMenuItem.Text = "\u5458\u5de5\u7ba1\u7406";
            this.gestionDeEmpleadosToolStripMenuItem.Click += new System.EventHandler(this.gestionDeEmpleadosToolStripMenuItem_Click);
            // 
            // toolStripMenuItem2
            // 
            this.toolStripMenuItem2.Name = "toolStripMenuItem2";
            this.toolStripMenuItem2.Size = new System.Drawing.Size(177, 6);
            // 
            // consultarDatosDispositivoToolStripMenuItem
            // 
            this.consultarDatosDispositivoToolStripMenuItem.Name = "consultarDatosDispositivoToolStripMenuItem";
            this.consultarDatosDispositivoToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
            this.consultarDatosDispositivoToolStripMenuItem.Text = "\u6743\u9650\u7ba1\u7406";
            this.consultarDatosDispositivoToolStripMenuItem.Click += new System.EventHandler(this.consultarDatosDispositivoToolStripMenuItem_Click);
            // 
            // refreshPermissionsToolStripMenuItem
            // 
            this.refreshPermissionsToolStripMenuItem.Name = "refreshPermissionsToolStripMenuItem";
            this.refreshPermissionsToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
            this.refreshPermissionsToolStripMenuItem.Text = "\u5237\u65b0\u7528\u6237\u6743\u9650";
            this.refreshPermissionsToolStripMenuItem.Click += new System.EventHandler(this.refreshPermissionsToolStripMenuItem_Click);
            // 
            // toolStripMenuItem3
            // 
            this.toolStripMenuItem3.Name = "toolStripMenuItem3";
            this.toolStripMenuItem3.Size = new System.Drawing.Size(177, 6);
            // 
            // salirToolStripMenuItem
            // 
            this.salirToolStripMenuItem.Name = "salirToolStripMenuItem";
            this.salirToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
            this.salirToolStripMenuItem.Text = "\u9000\u51fa";
            this.salirToolStripMenuItem.Click += new System.EventHandler(this.salirToolStripMenuItem_Click);
            // 
            // MDIParent
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackgroundImageLayout = System.Windows.Forms.ImageLayout.None;
            this.ClientSize = new System.Drawing.Size(800, 415);
            this.Controls.Add(this.menuStrip1);
            this.IsMdiContainer = true;
            this.MainMenuStrip = this.menuStrip1;
            this.Name = "MDIParent";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "\u95e8\u7981\u7cfb\u7edf";
            this.WindowState = System.Windows.Forms.FormWindowState.Maximized;
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.MDIParent_FormClosing);
            this.Load += new System.EventHandler(this.MDIParent_Load);
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.ToolStripMenuItem generalToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem gestiónDeDispositivosToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem gestionDeEmpleadosToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripMenuItem2;
        private System.Windows.Forms.ToolStripMenuItem consultarDatosDispositivoToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem refreshPermissionsToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripMenuItem3;
        private System.Windows.Forms.ToolStripMenuItem salirToolStripMenuItem;
    }
}

