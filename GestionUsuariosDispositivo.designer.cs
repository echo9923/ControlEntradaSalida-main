
namespace ControlEntradaSalida
{
    partial class GestionUsuariosDispositivo
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
            this.listView = new System.Windows.Forms.ListView();
            this.employeeNo = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.name = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.userType = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.validEnable = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.beginTime = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.endTime = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.numOfCard = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.numOfFace = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.estado = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.buttonConsultar = new System.Windows.Forms.Button();
            this.checkBoxSeleccionarTodos = new System.Windows.Forms.CheckBox();
            this.textBoxTotal = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.buttonAgregarTarjeta = new System.Windows.Forms.Button();
            this.buttonBackup = new System.Windows.Forms.Button();
            this.buttonEliminar = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // listView
            // 
            this.listView.CheckBoxes = true;
            this.listView.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.employeeNo,
            this.name,
            this.userType,
            this.validEnable,
            this.beginTime,
            this.endTime,
            this.numOfCard,
            this.numOfFace,
            this.estado});
            this.listView.FullRowSelect = true;
            this.listView.HideSelection = false;
            this.listView.Location = new System.Drawing.Point(12, 76);
            this.listView.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.listView.MultiSelect = false;
            this.listView.Name = "listView";
            this.listView.Size = new System.Drawing.Size(790, 302);
            this.listView.TabIndex = 17;
            this.listView.UseCompatibleStateImageBehavior = false;
            this.listView.View = System.Windows.Forms.View.Details;
            this.listView.SelectedIndexChanged += new System.EventHandler(this.listView_SelectedIndexChanged);
            // 
            // employeeNo
            // 
            this.employeeNo.Text = "员工号";
            this.employeeNo.Width = 72;
            // 
            // name
            // 
            this.name.Text = "姓名";
            this.name.Width = 120;
            // 
            // userType
            // 
            this.userType.Text = "用户类型";
            this.userType.Width = 100;
            // 
            // validEnable
            // 
            this.validEnable.Text = "有效启用";
            this.validEnable.Width = 73;
            // 
            // beginTime
            // 
            this.beginTime.Text = "权限起始";
            this.beginTime.Width = 100;
            // 
            // endTime
            // 
            this.endTime.Text = "权限结束";
            this.endTime.Width = 100;
            // 
            // numOfCard
            // 
            this.numOfCard.DisplayIndex = 7;
            this.numOfCard.Text = "卡数量";
            this.numOfCard.Width = 72;
            // 
            // numOfFace
            // 
            this.numOfFace.DisplayIndex = 6;
            this.numOfFace.Text = "面部数据";
            this.numOfFace.Width = 72;
            // 
            // estado
            // 
            this.estado.Text = "状态";
            // 
            // buttonConsultar
            // 
            this.buttonConsultar.Image = global::ControlEntradaSalida.Properties.Resources.Search_16x;
            this.buttonConsultar.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.buttonConsultar.Location = new System.Drawing.Point(12, 22);
            this.buttonConsultar.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.buttonConsultar.Name = "buttonConsultar";
            this.buttonConsultar.Size = new System.Drawing.Size(90, 22);
            this.buttonConsultar.TabIndex = 19;
            this.buttonConsultar.Text = "查询";
            this.buttonConsultar.UseVisualStyleBackColor = true;
            this.buttonConsultar.Click += new System.EventHandler(this.buttonConsultar_Click);
            // 
            // checkBoxSeleccionarTodos
            // 
            this.checkBoxSeleccionarTodos.AutoSize = true;
            this.checkBoxSeleccionarTodos.Location = new System.Drawing.Point(12, 54);
            this.checkBoxSeleccionarTodos.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.checkBoxSeleccionarTodos.Name = "checkBoxSeleccionarTodos";
            this.checkBoxSeleccionarTodos.Size = new System.Drawing.Size(48, 16);
            this.checkBoxSeleccionarTodos.TabIndex = 22;
            this.checkBoxSeleccionarTodos.Text = "全选";
            this.checkBoxSeleccionarTodos.UseVisualStyleBackColor = true;
            this.checkBoxSeleccionarTodos.CheckedChanged += new System.EventHandler(this.checkBoxSeleccionarTodos_CheckedChanged);
            // 
            // textBoxTotal
            // 
            this.textBoxTotal.Location = new System.Drawing.Point(200, 51);
            this.textBoxTotal.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.textBoxTotal.Name = "textBoxTotal";
            this.textBoxTotal.ReadOnly = true;
            this.textBoxTotal.Size = new System.Drawing.Size(49, 21);
            this.textBoxTotal.TabIndex = 23;
            this.textBoxTotal.TextChanged += new System.EventHandler(this.textBoxTotal_TextChanged);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(143, 55);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(53, 12);
            this.label1.TabIndex = 24;
            this.label1.Text = "用户总数";
            // 
            // buttonAgregarTarjeta
            // 
            this.buttonAgregarTarjeta.Image = global::ControlEntradaSalida.Properties.Resources.Edit_grey_16x;
            this.buttonAgregarTarjeta.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.buttonAgregarTarjeta.Location = new System.Drawing.Point(256, 383);
            this.buttonAgregarTarjeta.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.buttonAgregarTarjeta.Name = "buttonAgregarTarjeta";
            this.buttonAgregarTarjeta.Size = new System.Drawing.Size(116, 22);
            this.buttonAgregarTarjeta.TabIndex = 21;
            this.buttonAgregarTarjeta.Text = "添加卡片";
            this.buttonAgregarTarjeta.UseVisualStyleBackColor = true;
            this.buttonAgregarTarjeta.Click += new System.EventHandler(this.buttonAgregarTarjeta_Click);
            // 
            // buttonBackup
            // 
            this.buttonBackup.Image = global::ControlEntradaSalida.Properties.Resources.Save_grey_16x;
            this.buttonBackup.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.buttonBackup.Location = new System.Drawing.Point(134, 383);
            this.buttonBackup.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.buttonBackup.Name = "buttonBackup";
            this.buttonBackup.Size = new System.Drawing.Size(116, 22);
            this.buttonBackup.TabIndex = 20;
            this.buttonBackup.Text = "备份";
            this.buttonBackup.UseVisualStyleBackColor = true;
            this.buttonBackup.Click += new System.EventHandler(this.buttonBackup_Click);
            // 
            // buttonEliminar
            // 
            this.buttonEliminar.Image = global::ControlEntradaSalida.Properties.Resources.DeleteAllRows_16x;
            this.buttonEliminar.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.buttonEliminar.Location = new System.Drawing.Point(12, 383);
            this.buttonEliminar.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.buttonEliminar.Name = "buttonEliminar";
            this.buttonEliminar.Size = new System.Drawing.Size(116, 22);
            this.buttonEliminar.TabIndex = 18;
            this.buttonEliminar.Text = "删除";
            this.buttonEliminar.UseVisualStyleBackColor = true;
            this.buttonEliminar.Click += new System.EventHandler(this.buttonEliminar_Click);
            // 
            // GestionUsuariosDispositivo
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(814, 415);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.textBoxTotal);
            this.Controls.Add(this.checkBoxSeleccionarTodos);
            this.Controls.Add(this.buttonAgregarTarjeta);
            this.Controls.Add(this.buttonBackup);
            this.Controls.Add(this.buttonConsultar);
            this.Controls.Add(this.buttonEliminar);
            this.Controls.Add(this.listView);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "GestionUsuariosDispositivo";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "设备用户权限管理";
            this.Load += new System.EventHandler(this.GestionUsuariosDispositivo_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ListView listView;
        private System.Windows.Forms.ColumnHeader employeeNo;
        private System.Windows.Forms.ColumnHeader name;
        private System.Windows.Forms.ColumnHeader userType;
        private System.Windows.Forms.ColumnHeader validEnable;
        private System.Windows.Forms.ColumnHeader beginTime;
        private System.Windows.Forms.ColumnHeader endTime;
        private System.Windows.Forms.Button buttonEliminar;
        private System.Windows.Forms.Button buttonConsultar;
        private System.Windows.Forms.ColumnHeader numOfFace;
        private System.Windows.Forms.Button buttonBackup;
        private System.Windows.Forms.ColumnHeader numOfCard;
        private System.Windows.Forms.ColumnHeader estado;
        private System.Windows.Forms.Button buttonAgregarTarjeta;
        private System.Windows.Forms.CheckBox checkBoxSeleccionarTodos;
        private System.Windows.Forms.TextBox textBoxTotal;
        private System.Windows.Forms.Label label1;
    }
}