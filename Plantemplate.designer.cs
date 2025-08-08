
namespace ControlEntradaSalida
{
    partial class Plantemplate
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
            this.btnWeekPlan = new System.Windows.Forms.Button();
            this.btnHolidayPlan = new System.Windows.Forms.Button();
            this.btnHolidayGroup = new System.Windows.Forms.Button();
            this.btnPlanTemplate = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // btnWeekPlan
            // 
            this.btnWeekPlan.BackColor = System.Drawing.SystemColors.Control;
            this.btnWeekPlan.Location = new System.Drawing.Point(151, 33);
            this.btnWeekPlan.Name = "btnWeekPlan";
            this.btnWeekPlan.Size = new System.Drawing.Size(158, 29);
            this.btnWeekPlan.TabIndex = 54;
            this.btnWeekPlan.Text = "周计划管理";
            this.btnWeekPlan.UseVisualStyleBackColor = true;
            this.btnWeekPlan.Click += new System.EventHandler(this.btnWeekPlan_Click);
            // 
            // btnHolidayPlan
            // 
            this.btnHolidayPlan.Location = new System.Drawing.Point(151, 82);
            this.btnHolidayPlan.Name = "btnHolidayPlan";
            this.btnHolidayPlan.Size = new System.Drawing.Size(158, 29);
            this.btnHolidayPlan.TabIndex = 55;
            this.btnHolidayPlan.Text = "假日计划管理";
            this.btnHolidayPlan.UseVisualStyleBackColor = true;
            this.btnHolidayPlan.Click += new System.EventHandler(this.btnHolidayPlan_Click);
            // 
            // btnHolidayGroup
            // 
            this.btnHolidayGroup.Location = new System.Drawing.Point(151, 132);
            this.btnHolidayGroup.Name = "btnHolidayGroup";
            this.btnHolidayGroup.Size = new System.Drawing.Size(158, 29);
            this.btnHolidayGroup.TabIndex = 56;
            this.btnHolidayGroup.Text = "假日组计划管理";
            this.btnHolidayGroup.UseVisualStyleBackColor = true;
            this.btnHolidayGroup.Click += new System.EventHandler(this.btnHolidayGroup_Click);
            // 
            // btnPlanTemplate
            // 
            this.btnPlanTemplate.Cursor = System.Windows.Forms.Cursors.Default;
            this.btnPlanTemplate.Location = new System.Drawing.Point(151, 182);
            this.btnPlanTemplate.Name = "btnPlanTemplate";
            this.btnPlanTemplate.Size = new System.Drawing.Size(158, 29);
            this.btnPlanTemplate.TabIndex = 57;
            this.btnPlanTemplate.Text = "计划模板管理";
            this.btnPlanTemplate.UseVisualStyleBackColor = true;
            this.btnPlanTemplate.Click += new System.EventHandler(this.btnPlanTemplate_Click);
            // 
            // Plantemplate
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(454, 261);
            this.Controls.Add(this.btnPlanTemplate);
            this.Controls.Add(this.btnHolidayGroup);
            this.Controls.Add(this.btnHolidayPlan);
            this.Controls.Add(this.btnWeekPlan);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "Plantemplate";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "计划管理";
            this.Load += new System.EventHandler(this.Plantemplate_Load);
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
        private System.Windows.Forms.Button btnWeekPlan;
        private System.Windows.Forms.Button btnHolidayPlan;
        private System.Windows.Forms.Button btnHolidayGroup;
        private System.Windows.Forms.Button btnPlanTemplate;
    }
}