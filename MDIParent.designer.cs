
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
            this.gestiónDeDispositivosToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.controldoor = new System.Windows.Forms.ToolStripMenuItem();
            this.gestionDeEmpleadosToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.CapturarEntradaSalidaToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem2 = new System.Windows.Forms.ToolStripSeparator();
            this.consultarDatosDispositivoToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem3 = new System.Windows.Forms.ToolStripSeparator();
            this.salirToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.informesToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.entradasYSalidasToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.eventosToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.Plantemplate = new System.Windows.Forms.ToolStripMenuItem();
            this.menuStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // menuStrip1
            // 
            this.menuStrip1.ImageScalingSize = new System.Drawing.Size(20, 20);
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.generalToolStripMenuItem,
            this.informesToolStripMenuItem});
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Padding = new System.Windows.Forms.Padding(4, 2, 0, 2);
            this.menuStrip1.Size = new System.Drawing.Size(800, 25);
            this.menuStrip1.TabIndex = 1;
            this.menuStrip1.Text = "menuStrip1";
            this.menuStrip1.ItemClicked += new System.Windows.Forms.ToolStripItemClickedEventHandler(this.menuStrip1_ItemClicked);
            // 
            // generalToolStripMenuItem
            // 
            this.generalToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.gestiónDeDispositivosToolStripMenuItem,
            this.controldoor,
            this.Plantemplate,
            this.gestionDeEmpleadosToolStripMenuItem,
            this.CapturarEntradaSalidaToolStripMenuItem,
            this.toolStripMenuItem2,
            this.consultarDatosDispositivoToolStripMenuItem,
            this.toolStripMenuItem3,
            this.salirToolStripMenuItem});
            this.generalToolStripMenuItem.Name = "generalToolStripMenuItem";
            this.generalToolStripMenuItem.Size = new System.Drawing.Size(44, 21);
            this.generalToolStripMenuItem.Text = "管理";
            // 
            // gestiónDeDispositivosToolStripMenuItem
            // 
            this.gestiónDeDispositivosToolStripMenuItem.Name = "gestiónDeDispositivosToolStripMenuItem";
            this.gestiónDeDispositivosToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
            this.gestiónDeDispositivosToolStripMenuItem.Text = "设备管理";
            this.gestiónDeDispositivosToolStripMenuItem.Click += new System.EventHandler(this.gestiónDeDispositivosToolStripMenuItem_Click);
            // 
            // controldoor
            // 
            this.controldoor.Name = "controldoor";
            this.controldoor.Size = new System.Drawing.Size(180, 22);
            this.controldoor.Text = "远程控门";
            this.controldoor.Click += new System.EventHandler(this.controldoor_Click);
            // 
            // gestionDeEmpleadosToolStripMenuItem
            // 
            this.gestionDeEmpleadosToolStripMenuItem.Name = "gestionDeEmpleadosToolStripMenuItem";
            this.gestionDeEmpleadosToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
            this.gestionDeEmpleadosToolStripMenuItem.Text = "员工管理";
            this.gestionDeEmpleadosToolStripMenuItem.Click += new System.EventHandler(this.gestionDeEmpleadosToolStripMenuItem_Click);
            // 
            // CapturarEntradaSalidaToolStripMenuItem
            // 
            this.CapturarEntradaSalidaToolStripMenuItem.Name = "CapturarEntradaSalidaToolStripMenuItem";
            this.CapturarEntradaSalidaToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
            this.CapturarEntradaSalidaToolStripMenuItem.Text = "进出事件";
            this.CapturarEntradaSalidaToolStripMenuItem.Click += new System.EventHandler(this.CapturarEntradaSalidaToolStripMenuItem_Click);
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
            this.consultarDatosDispositivoToolStripMenuItem.Text = "设备用户权限管理";
            this.consultarDatosDispositivoToolStripMenuItem.Click += new System.EventHandler(this.consultarDatosDispositivoToolStripMenuItem_Click);
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
            this.salirToolStripMenuItem.Text = "退出";
            this.salirToolStripMenuItem.Click += new System.EventHandler(this.salirToolStripMenuItem_Click);
            // 
            // informesToolStripMenuItem
            // 
            this.informesToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.entradasYSalidasToolStripMenuItem,
            this.eventosToolStripMenuItem});
            this.informesToolStripMenuItem.Name = "informesToolStripMenuItem";
            this.informesToolStripMenuItem.Size = new System.Drawing.Size(44, 21);
            this.informesToolStripMenuItem.Text = "日志";
            // 
            // entradasYSalidasToolStripMenuItem
            // 
            this.entradasYSalidasToolStripMenuItem.Name = "entradasYSalidasToolStripMenuItem";
            this.entradasYSalidasToolStripMenuItem.Size = new System.Drawing.Size(124, 22);
            this.entradasYSalidasToolStripMenuItem.Text = "进出事件";
            this.entradasYSalidasToolStripMenuItem.Click += new System.EventHandler(this.entradasYSalidasToolStripMenuItem_Click);
            // 
            // eventosToolStripMenuItem
            // 
            this.eventosToolStripMenuItem.Name = "eventosToolStripMenuItem";
            this.eventosToolStripMenuItem.Size = new System.Drawing.Size(124, 22);
            this.eventosToolStripMenuItem.Text = "事件日志";
            this.eventosToolStripMenuItem.Click += new System.EventHandler(this.eventosToolStripMenuItem_Click);
            // 
            // Plantemplate
            // 
            this.Plantemplate.Name = "Plantemplate";
            this.Plantemplate.Size = new System.Drawing.Size(180, 22);
            this.Plantemplate.Text = "计划管理";
            this.Plantemplate.Click += new System.EventHandler(this.Plantemplate_Click);
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
            this.Text = "门禁系统";
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
        private System.Windows.Forms.ToolStripMenuItem CapturarEntradaSalidaToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem salirToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripMenuItem2;
        private System.Windows.Forms.ToolStripMenuItem informesToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem eventosToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem consultarDatosDispositivoToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripMenuItem3;
        private System.Windows.Forms.ToolStripMenuItem entradasYSalidasToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem controldoor;
        private System.Windows.Forms.ToolStripMenuItem Plantemplate;
    }
}

