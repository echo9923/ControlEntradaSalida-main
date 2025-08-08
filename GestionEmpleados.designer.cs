
namespace ControlEntradaSalida
{
    partial class GestionEmpleados
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
            this.components = new System.ComponentModel.Container();
            this.groupBox = new System.Windows.Forms.GroupBox();
            this.label4 = new System.Windows.Forms.Label();
            this.cmbCategoria = new System.Windows.Forms.ComboBox();
            this.buttonFiltrar = new System.Windows.Forms.Button();
            this.label6 = new System.Windows.Forms.Label();
            this.textBoxTarjeta = new System.Windows.Forms.TextBox();
            this.cmbEstado = new System.Windows.Forms.ComboBox();
            this.label10 = new System.Windows.Forms.Label();
            this.listView = new System.Windows.Forms.ListView();
            this.documento = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.estado = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.tarjeta = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.nombres = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.apellidos = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.foto = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.Categoria = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.buttonNuevo = new System.Windows.Forms.Button();
            this.buttonEliminar = new System.Windows.Forms.Button();
            this.buttonAgregar = new System.Windows.Forms.Button();
            this.buttonCapturarFoto = new System.Windows.Forms.Button();
            this.textBoxApellidos = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.textBoxNombres = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.textBoxDocumento = new System.Windows.Forms.TextBox();
            this.pictureBoxUsuario = new System.Windows.Forms.PictureBox();
            this.errorProvider = new System.Windows.Forms.ErrorProvider(this.components);
            this.groupBox.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxUsuario)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.errorProvider)).BeginInit();
            this.SuspendLayout();
            // 
            // groupBox
            // 
            this.groupBox.Controls.Add(this.label4);
            this.groupBox.Controls.Add(this.cmbCategoria);
            this.groupBox.Controls.Add(this.buttonFiltrar);
            this.groupBox.Controls.Add(this.label6);
            this.groupBox.Controls.Add(this.textBoxTarjeta);
            this.groupBox.Controls.Add(this.cmbEstado);
            this.groupBox.Controls.Add(this.label10);
            this.groupBox.Controls.Add(this.listView);
            this.groupBox.Controls.Add(this.buttonNuevo);
            this.groupBox.Controls.Add(this.buttonEliminar);
            this.groupBox.Controls.Add(this.buttonAgregar);
            this.groupBox.Controls.Add(this.buttonCapturarFoto);
            this.groupBox.Controls.Add(this.textBoxApellidos);
            this.groupBox.Controls.Add(this.label3);
            this.groupBox.Controls.Add(this.textBoxNombres);
            this.groupBox.Controls.Add(this.label2);
            this.groupBox.Controls.Add(this.label1);
            this.groupBox.Controls.Add(this.textBoxDocumento);
            this.groupBox.Controls.Add(this.pictureBoxUsuario);
            this.groupBox.Location = new System.Drawing.Point(16, 14);
            this.groupBox.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.groupBox.Name = "groupBox";
            this.groupBox.Padding = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.groupBox.Size = new System.Drawing.Size(988, 492);
            this.groupBox.TabIndex = 0;
            this.groupBox.TabStop = false;
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(711, 81);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(45, 15);
            this.label4.TabIndex = 30;
            this.label4.Text = "类别*";
            // 
            // cmbCategoria
            // 
            this.cmbCategoria.FormattingEnabled = true;
            this.cmbCategoria.Items.AddRange(new object[] {
            "normal",
            "special"});
            this.cmbCategoria.Location = new System.Drawing.Point(711, 100);
            this.cmbCategoria.Name = "cmbCategoria";
            this.cmbCategoria.Size = new System.Drawing.Size(259, 23);
            this.cmbCategoria.TabIndex = 2;
            this.cmbCategoria.Text = "normal";
            this.cmbCategoria.SelectedIndexChanged += new System.EventHandler(this.cmbCategoria_SelectedIndexChanged);
            this.cmbCategoria.Validating += new System.ComponentModel.CancelEventHandler(this.cmbCategoria_Validating);
            // 
            // buttonFiltrar
            // 
            this.buttonFiltrar.Image = global::ControlEntradaSalida.Properties.Resources.Filter_16x;
            this.buttonFiltrar.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.buttonFiltrar.Location = new System.Drawing.Point(711, 204);
            this.buttonFiltrar.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.buttonFiltrar.Name = "buttonFiltrar";
            this.buttonFiltrar.Size = new System.Drawing.Size(151, 27);
            this.buttonFiltrar.TabIndex = 15;
            this.buttonFiltrar.Text = "筛选";
            this.buttonFiltrar.UseVisualStyleBackColor = true;
            this.buttonFiltrar.Click += new System.EventHandler(this.buttonFiltrar_Click);
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(707, 18);
            this.label6.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(69, 15);
            this.label6.TabIndex = 28;
            this.label6.Text = "No. 卡号";
            // 
            // textBoxTarjeta
            // 
            this.textBoxTarjeta.Location = new System.Drawing.Point(711, 39);
            this.textBoxTarjeta.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.textBoxTarjeta.Name = "textBoxTarjeta";
            this.textBoxTarjeta.ReadOnly = true;
            this.textBoxTarjeta.Size = new System.Drawing.Size(259, 25);
            this.textBoxTarjeta.TabIndex = 3;
            // 
            // cmbEstado
            // 
            this.cmbEstado.FormattingEnabled = true;
            this.cmbEstado.Items.AddRange(new object[] {
            "ACTIVO",
            "INACTIVO"});
            this.cmbEstado.Location = new System.Drawing.Point(416, 38);
            this.cmbEstado.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.cmbEstado.Name = "cmbEstado";
            this.cmbEstado.Size = new System.Drawing.Size(216, 23);
            this.cmbEstado.TabIndex = 2;
            this.cmbEstado.Text = "ACTIVO";
            this.cmbEstado.SelectedIndexChanged += new System.EventHandler(this.cmbEstado_SelectedIndexChanged);
            this.cmbEstado.Validating += new System.ComponentModel.CancelEventHandler(this.cmbEstado_Validating);
            // 
            // label10
            // 
            this.label10.AutoSize = true;
            this.label10.Location = new System.Drawing.Point(412, 22);
            this.label10.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label10.Name = "label10";
            this.label10.Size = new System.Drawing.Size(45, 15);
            this.label10.TabIndex = 25;
            this.label10.Text = "状态*";
            // 
            // listView
            // 
            this.listView.CheckBoxes = true;
            this.listView.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.documento,
            this.estado,
            this.tarjeta,
            this.nombres,
            this.apellidos,
            this.foto,
            this.Categoria});
            this.listView.FullRowSelect = true;
            this.listView.HideSelection = false;
            this.listView.Location = new System.Drawing.Point(8, 249);
            this.listView.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.listView.MultiSelect = false;
            this.listView.Name = "listView";
            this.listView.Size = new System.Drawing.Size(971, 235);
            this.listView.TabIndex = 16;
            this.listView.UseCompatibleStateImageBehavior = false;
            this.listView.View = System.Windows.Forms.View.Details;
            this.listView.Click += new System.EventHandler(this.listView_Click);
            // 
            // documento
            // 
            this.documento.Text = "文档";
            this.documento.Width = 74;
            // 
            // estado
            // 
            this.estado.Text = "状态";
            // 
            // tarjeta
            // 
            this.tarjeta.Text = "No. 卡号";
            this.tarjeta.Width = 74;
            // 
            // nombres
            // 
            this.nombres.Text = "名字";
            this.nombres.Width = 91;
            // 
            // apellidos
            // 
            this.apellidos.Text = "姓氏";
            this.apellidos.Width = 94;
            // 
            // foto
            // 
            this.foto.Text = "照片";
            this.foto.Width = 200;
            // 
            // Categoria
            // 
            this.Categoria.Text = "类别";
            this.Categoria.Width = 120;
            // 
            // buttonNuevo
            // 
            this.buttonNuevo.Image = global::ControlEntradaSalida.Properties.Resources.NewItem_16x;
            this.buttonNuevo.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.buttonNuevo.Location = new System.Drawing.Point(235, 204);
            this.buttonNuevo.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.buttonNuevo.Name = "buttonNuevo";
            this.buttonNuevo.Size = new System.Drawing.Size(151, 27);
            this.buttonNuevo.TabIndex = 14;
            this.buttonNuevo.Text = "新建";
            this.buttonNuevo.UseVisualStyleBackColor = true;
            this.buttonNuevo.Click += new System.EventHandler(this.buttonNuevo_Click);
            // 
            // buttonEliminar
            // 
            this.buttonEliminar.Image = global::ControlEntradaSalida.Properties.Resources.DeleteAllRows_16x;
            this.buttonEliminar.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.buttonEliminar.Location = new System.Drawing.Point(552, 204);
            this.buttonEliminar.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.buttonEliminar.Name = "buttonEliminar";
            this.buttonEliminar.Size = new System.Drawing.Size(151, 27);
            this.buttonEliminar.TabIndex = 13;
            this.buttonEliminar.Text = "删除";
            this.buttonEliminar.UseVisualStyleBackColor = true;
            this.buttonEliminar.Click += new System.EventHandler(this.buttonEliminar_Click);
            // 
            // buttonAgregar
            // 
            this.buttonAgregar.Image = global::ControlEntradaSalida.Properties.Resources.Save_grey_16x;
            this.buttonAgregar.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.buttonAgregar.Location = new System.Drawing.Point(393, 204);
            this.buttonAgregar.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.buttonAgregar.Name = "buttonAgregar";
            this.buttonAgregar.Size = new System.Drawing.Size(151, 27);
            this.buttonAgregar.TabIndex = 12;
            this.buttonAgregar.Text = "保存";
            this.buttonAgregar.UseVisualStyleBackColor = true;
            this.buttonAgregar.Click += new System.EventHandler(this.buttonAgregar_Click);
            // 
            // buttonCapturarFoto
            // 
            this.buttonCapturarFoto.Image = global::ControlEntradaSalida.Properties.Resources.CaptureFrame_16x;
            this.buttonCapturarFoto.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.buttonCapturarFoto.Location = new System.Drawing.Point(8, 204);
            this.buttonCapturarFoto.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.buttonCapturarFoto.Name = "buttonCapturarFoto";
            this.buttonCapturarFoto.Size = new System.Drawing.Size(151, 27);
            this.buttonCapturarFoto.TabIndex = 11;
            this.buttonCapturarFoto.Text = "采集照片";
            this.buttonCapturarFoto.UseVisualStyleBackColor = true;
            this.buttonCapturarFoto.Click += new System.EventHandler(this.buttonCapturarFoto_Click);
            // 
            // textBoxApellidos
            // 
            this.textBoxApellidos.CharacterCasing = System.Windows.Forms.CharacterCasing.Upper;
            this.textBoxApellidos.Location = new System.Drawing.Point(167, 163);
            this.textBoxApellidos.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.textBoxApellidos.Name = "textBoxApellidos";
            this.textBoxApellidos.Size = new System.Drawing.Size(377, 25);
            this.textBoxApellidos.TabIndex = 5;
            this.textBoxApellidos.Validating += new System.ComponentModel.CancelEventHandler(this.textBoxApellidos_Validating);
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(167, 144);
            this.label3.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(45, 15);
            this.label3.TabIndex = 5;
            this.label3.Text = "姓氏*";
            // 
            // textBoxNombres
            // 
            this.textBoxNombres.CharacterCasing = System.Windows.Forms.CharacterCasing.Upper;
            this.textBoxNombres.Location = new System.Drawing.Point(167, 99);
            this.textBoxNombres.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.textBoxNombres.Name = "textBoxNombres";
            this.textBoxNombres.Size = new System.Drawing.Size(377, 25);
            this.textBoxNombres.TabIndex = 4;
            this.textBoxNombres.Validating += new System.ComponentModel.CancelEventHandler(this.textBoxNombres_Validating);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(163, 81);
            this.label2.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(45, 15);
            this.label2.TabIndex = 3;
            this.label2.Text = "名字*";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(163, 22);
            this.label1.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(45, 15);
            this.label1.TabIndex = 2;
            this.label1.Text = "文档*";
            // 
            // textBoxDocumento
            // 
            this.textBoxDocumento.CharacterCasing = System.Windows.Forms.CharacterCasing.Upper;
            this.textBoxDocumento.Location = new System.Drawing.Point(167, 40);
            this.textBoxDocumento.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.textBoxDocumento.Name = "textBoxDocumento";
            this.textBoxDocumento.Size = new System.Drawing.Size(200, 25);
            this.textBoxDocumento.TabIndex = 1;
            this.textBoxDocumento.Validating += new System.ComponentModel.CancelEventHandler(this.textBoxDocumento_Validating);
            // 
            // pictureBoxUsuario
            // 
            this.pictureBoxUsuario.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.pictureBoxUsuario.Location = new System.Drawing.Point(8, 22);
            this.pictureBoxUsuario.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.pictureBoxUsuario.Name = "pictureBoxUsuario";
            this.pictureBoxUsuario.Size = new System.Drawing.Size(150, 164);
            this.pictureBoxUsuario.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            this.pictureBoxUsuario.TabIndex = 0;
            this.pictureBoxUsuario.TabStop = false;
            // 
            // errorProvider
            // 
            this.errorProvider.ContainerControl = this;
            // 
            // GestionEmpleados
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1019, 519);
            this.Controls.Add(this.groupBox);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "GestionEmpleados";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "员工管理";
            this.Load += new System.EventHandler(this.GestionUsuarios_Load);
            this.groupBox.ResumeLayout(false);
            this.groupBox.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxUsuario)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.errorProvider)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.GroupBox groupBox;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox textBoxDocumento;
        private System.Windows.Forms.PictureBox pictureBoxUsuario;
        private System.Windows.Forms.ListView listView;
        private System.Windows.Forms.ColumnHeader documento;
        private System.Windows.Forms.ColumnHeader tarjeta;
        private System.Windows.Forms.ColumnHeader nombres;
        private System.Windows.Forms.ColumnHeader apellidos;
        private System.Windows.Forms.Button buttonNuevo;
        private System.Windows.Forms.Button buttonEliminar;
        private System.Windows.Forms.Button buttonAgregar;
        private System.Windows.Forms.Button buttonCapturarFoto;
        private System.Windows.Forms.TextBox textBoxApellidos;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.TextBox textBoxNombres;
        private System.Windows.Forms.ColumnHeader foto;
        private System.Windows.Forms.Label label10;
        private System.Windows.Forms.ColumnHeader estado;
        private System.Windows.Forms.ComboBox cmbEstado;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.TextBox textBoxTarjeta;
        private System.Windows.Forms.Button buttonFiltrar;
        private System.Windows.Forms.ErrorProvider errorProvider;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.ComboBox cmbCategoria;
        private System.Windows.Forms.ColumnHeader Categoria;
    }
}