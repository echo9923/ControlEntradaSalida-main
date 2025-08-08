
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
            this.SuspendLayout();
            // 
            // btnOpen
            // 
            this.btnOpen.Location = new System.Drawing.Point(148, 31);
            this.btnOpen.Name = "btnOpen";
            this.btnOpen.Size = new System.Drawing.Size(103, 24);
            this.btnOpen.TabIndex = 61;
            this.btnOpen.Text = "开门";
            this.btnOpen.UseVisualStyleBackColor = true;
            this.btnOpen.Click += new System.EventHandler(this.btnOpen_Click);
            // 
            // btnClose
            // 
            this.btnClose.Location = new System.Drawing.Point(148, 71);
            this.btnClose.Name = "btnClose";
            this.btnClose.Size = new System.Drawing.Size(103, 24);
            this.btnClose.TabIndex = 63;
            this.btnClose.Text = "关门";
            this.btnClose.UseVisualStyleBackColor = true;
            this.btnClose.Click += new System.EventHandler(this.btnClose_Click);
            // 
            // btnStayOpen
            // 
            this.btnStayOpen.Location = new System.Drawing.Point(148, 111);
            this.btnStayOpen.Name = "btnStayOpen";
            this.btnStayOpen.Size = new System.Drawing.Size(103, 24);
            this.btnStayOpen.TabIndex = 65;
            this.btnStayOpen.Text = "常开";
            this.btnStayOpen.UseVisualStyleBackColor = true;
            this.btnStayOpen.Click += new System.EventHandler(this.btnStayOpen_Click);
            // 
            // btnStayClose
            // 
            this.btnStayClose.Location = new System.Drawing.Point(148, 152);
            this.btnStayClose.Name = "btnStayClose";
            this.btnStayClose.Size = new System.Drawing.Size(103, 24);
            this.btnStayClose.TabIndex = 67;
            this.btnStayClose.Text = "常闭";
            this.btnStayClose.UseVisualStyleBackColor = true;
            this.btnStayClose.Click += new System.EventHandler(this.btnStayClose_Click);
            // 
            // controldoor
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(433, 239);
            this.Controls.Add(this.btnStayClose);
            this.Controls.Add(this.btnStayOpen);
            this.Controls.Add(this.btnClose);
            this.Controls.Add(this.btnOpen);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "controldoor";
            this.ShowIcon = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "远程控门";
            this.Load += new System.EventHandler(this.controldoor_Load_1);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.GroupBox groupBox;
        private System.Windows.Forms.ListView listView;
        private System.Windows.Forms.ColumnHeader id;
        private System.Windows.Forms.ColumnHeader Nombre;
        private System.Windows.Forms.ColumnHeader descripcion;
        private System.Windows.Forms.ColumnHeader direccionip;
        private System.Windows.Forms.ColumnHeader usuario;
        private System.Windows.Forms.ColumnHeader conectado;
        private System.Windows.Forms.ColumnHeader puerto;
        private System.Windows.Forms.Button buttonNuevo;
        private System.Windows.Forms.Button buttonEliminar;
        private System.Windows.Forms.ColumnHeader activo;
        private System.Windows.Forms.ColumnHeader predeterminado;
        private System.Windows.Forms.ColumnHeader ultimavez;
        private System.Windows.Forms.Button btnOpen;
        private System.Windows.Forms.Button btnClose;
        private System.Windows.Forms.Button btnStayOpen;
        private System.Windows.Forms.Button btnStayClose;
    }
}