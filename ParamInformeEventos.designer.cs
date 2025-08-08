
namespace ControlEntradaSalida
{
    partial class ParamInformeEventos
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
            this.groupBox5 = new System.Windows.Forms.GroupBox();
            this.label5 = new System.Windows.Forms.Label();
            this.dateTimePickerHoraFinal = new System.Windows.Forms.DateTimePicker();
            this.label6 = new System.Windows.Forms.Label();
            this.dateTimePickerHoraInicial = new System.Windows.Forms.DateTimePicker();
            this.radioButtonTodasHoras = new System.Windows.Forms.RadioButton();
            this.radioButtonRangoHoras = new System.Windows.Forms.RadioButton();
            this.groupBox4 = new System.Windows.Forms.GroupBox();
            this.label3 = new System.Windows.Forms.Label();
            this.dateTimePickerFechaFinal = new System.Windows.Forms.DateTimePicker();
            this.label2 = new System.Windows.Forms.Label();
            this.dateTimePickerFechaInicial = new System.Windows.Forms.DateTimePicker();
            this.radioButtonTodasFechas = new System.Windows.Forms.RadioButton();
            this.radioButtonRangoFechas = new System.Windows.Forms.RadioButton();
            this.groupBox3 = new System.Windows.Forms.GroupBox();
            this.textBoxApellidosEmpleado = new System.Windows.Forms.TextBox();
            this.label7 = new System.Windows.Forms.Label();
            this.textBoxNombreEmpleado = new System.Windows.Forms.TextBox();
            this.label4 = new System.Windows.Forms.Label();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.label1 = new System.Windows.Forms.Label();
            this.textBoxDocumentoEmpleado = new System.Windows.Forms.TextBox();
            this.radioButtonTodosEmpleados = new System.Windows.Forms.RadioButton();
            this.buttonVerInforme = new System.Windows.Forms.Button();
            this.listView = new System.Windows.Forms.ListView();
            this.num = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.documento = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.nombres = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.apellidos = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.fecha = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.hora = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.groupBox1.SuspendLayout();
            this.groupBox5.SuspendLayout();
            this.groupBox4.SuspendLayout();
            this.groupBox3.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.SuspendLayout();
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.groupBox5);
            this.groupBox1.Controls.Add(this.groupBox4);
            this.groupBox1.Controls.Add(this.groupBox3);
            this.groupBox1.Controls.Add(this.groupBox2);
            this.groupBox1.Controls.Add(this.buttonVerInforme);
            this.groupBox1.Location = new System.Drawing.Point(16, 14);
            this.groupBox1.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Padding = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.groupBox1.Size = new System.Drawing.Size(793, 275);
            this.groupBox1.TabIndex = 0;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "报告参数";
            // 
            // groupBox5
            // 
            this.groupBox5.Controls.Add(this.label5);
            this.groupBox5.Controls.Add(this.dateTimePickerHoraFinal);
            this.groupBox5.Controls.Add(this.label6);
            this.groupBox5.Controls.Add(this.dateTimePickerHoraInicial);
            this.groupBox5.Controls.Add(this.radioButtonTodasHoras);
            this.groupBox5.Controls.Add(this.radioButtonRangoHoras);
            this.groupBox5.Location = new System.Drawing.Point(401, 83);
            this.groupBox5.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.groupBox5.Name = "groupBox5";
            this.groupBox5.Padding = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.groupBox5.Size = new System.Drawing.Size(385, 155);
            this.groupBox5.TabIndex = 16;
            this.groupBox5.TabStop = false;
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(7, 112);
            this.label5.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(67, 15);
            this.label5.TabIndex = 15;
            this.label5.Text = "结束时刻";
            // 
            // dateTimePickerHoraFinal
            // 
            this.dateTimePickerHoraFinal.Enabled = false;
            this.dateTimePickerHoraFinal.Format = System.Windows.Forms.DateTimePickerFormat.Time;
            this.dateTimePickerHoraFinal.Location = new System.Drawing.Point(103, 112);
            this.dateTimePickerHoraFinal.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.dateTimePickerHoraFinal.Name = "dateTimePickerHoraFinal";
            this.dateTimePickerHoraFinal.Size = new System.Drawing.Size(99, 25);
            this.dateTimePickerHoraFinal.TabIndex = 8;
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(7, 68);
            this.label6.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(67, 15);
            this.label6.TabIndex = 13;
            this.label6.Text = "起始时刻";
            // 
            // dateTimePickerHoraInicial
            // 
            this.dateTimePickerHoraInicial.Enabled = false;
            this.dateTimePickerHoraInicial.Format = System.Windows.Forms.DateTimePickerFormat.Time;
            this.dateTimePickerHoraInicial.Location = new System.Drawing.Point(103, 61);
            this.dateTimePickerHoraInicial.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.dateTimePickerHoraInicial.Name = "dateTimePickerHoraInicial";
            this.dateTimePickerHoraInicial.Size = new System.Drawing.Size(99, 25);
            this.dateTimePickerHoraInicial.TabIndex = 7;
            // 
            // radioButtonTodasHoras
            // 
            this.radioButtonTodasHoras.AutoSize = true;
            this.radioButtonTodasHoras.Checked = true;
            this.radioButtonTodasHoras.Location = new System.Drawing.Point(8, 22);
            this.radioButtonTodasHoras.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.radioButtonTodasHoras.Name = "radioButtonTodasHoras";
            this.radioButtonTodasHoras.Size = new System.Drawing.Size(88, 19);
            this.radioButtonTodasHoras.TabIndex = 5;
            this.radioButtonTodasHoras.TabStop = true;
            this.radioButtonTodasHoras.Text = "所有时刻";
            this.radioButtonTodasHoras.UseVisualStyleBackColor = true;
            this.radioButtonTodasHoras.CheckedChanged += new System.EventHandler(this.radioButtonTodasHoras_CheckedChanged);
            // 
            // radioButtonRangoHoras
            // 
            this.radioButtonRangoHoras.AutoSize = true;
            this.radioButtonRangoHoras.Location = new System.Drawing.Point(219, 22);
            this.radioButtonRangoHoras.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.radioButtonRangoHoras.Name = "radioButtonRangoHoras";
            this.radioButtonRangoHoras.Size = new System.Drawing.Size(88, 19);
            this.radioButtonRangoHoras.TabIndex = 6;
            this.radioButtonRangoHoras.Text = "时刻范围";
            this.radioButtonRangoHoras.UseVisualStyleBackColor = true;
            this.radioButtonRangoHoras.CheckedChanged += new System.EventHandler(this.radioButtonRangoHoras_CheckedChanged);
            // 
            // groupBox4
            // 
            this.groupBox4.Controls.Add(this.label3);
            this.groupBox4.Controls.Add(this.dateTimePickerFechaFinal);
            this.groupBox4.Controls.Add(this.label2);
            this.groupBox4.Controls.Add(this.dateTimePickerFechaInicial);
            this.groupBox4.Controls.Add(this.radioButtonTodasFechas);
            this.groupBox4.Controls.Add(this.radioButtonRangoFechas);
            this.groupBox4.Location = new System.Drawing.Point(8, 83);
            this.groupBox4.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.groupBox4.Name = "groupBox4";
            this.groupBox4.Padding = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.groupBox4.Size = new System.Drawing.Size(385, 155);
            this.groupBox4.TabIndex = 15;
            this.groupBox4.TabStop = false;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(7, 112);
            this.label3.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(67, 15);
            this.label3.TabIndex = 15;
            this.label3.Text = "结束日期";
            // 
            // dateTimePickerFechaFinal
            // 
            this.dateTimePickerFechaFinal.Enabled = false;
            this.dateTimePickerFechaFinal.Location = new System.Drawing.Point(103, 112);
            this.dateTimePickerFechaFinal.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.dateTimePickerFechaFinal.Name = "dateTimePickerFechaFinal";
            this.dateTimePickerFechaFinal.Size = new System.Drawing.Size(265, 25);
            this.dateTimePickerFechaFinal.TabIndex = 8;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(7, 68);
            this.label2.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(67, 15);
            this.label2.TabIndex = 13;
            this.label2.Text = "起始日期";
            // 
            // dateTimePickerFechaInicial
            // 
            this.dateTimePickerFechaInicial.Enabled = false;
            this.dateTimePickerFechaInicial.Location = new System.Drawing.Point(103, 61);
            this.dateTimePickerFechaInicial.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.dateTimePickerFechaInicial.Name = "dateTimePickerFechaInicial";
            this.dateTimePickerFechaInicial.Size = new System.Drawing.Size(265, 25);
            this.dateTimePickerFechaInicial.TabIndex = 7;
            // 
            // radioButtonTodasFechas
            // 
            this.radioButtonTodasFechas.AutoSize = true;
            this.radioButtonTodasFechas.Checked = true;
            this.radioButtonTodasFechas.Location = new System.Drawing.Point(8, 22);
            this.radioButtonTodasFechas.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.radioButtonTodasFechas.Name = "radioButtonTodasFechas";
            this.radioButtonTodasFechas.Size = new System.Drawing.Size(88, 19);
            this.radioButtonTodasFechas.TabIndex = 5;
            this.radioButtonTodasFechas.TabStop = true;
            this.radioButtonTodasFechas.Text = "所有日期";
            this.radioButtonTodasFechas.UseVisualStyleBackColor = true;
            this.radioButtonTodasFechas.Click += new System.EventHandler(this.radioButtonTodasFechas_Click);
            // 
            // radioButtonRangoFechas
            // 
            this.radioButtonRangoFechas.AutoSize = true;
            this.radioButtonRangoFechas.Location = new System.Drawing.Point(219, 22);
            this.radioButtonRangoFechas.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.radioButtonRangoFechas.Name = "radioButtonRangoFechas";
            this.radioButtonRangoFechas.Size = new System.Drawing.Size(88, 19);
            this.radioButtonRangoFechas.TabIndex = 6;
            this.radioButtonRangoFechas.Text = "日期范围";
            this.radioButtonRangoFechas.UseVisualStyleBackColor = true;
            this.radioButtonRangoFechas.Click += new System.EventHandler(this.radioButtonRangoFechas_Click);
            // 
            // groupBox3
            // 
            this.groupBox3.Controls.Add(this.textBoxApellidosEmpleado);
            this.groupBox3.Controls.Add(this.label7);
            this.groupBox3.Controls.Add(this.textBoxNombreEmpleado);
            this.groupBox3.Controls.Add(this.label4);
            this.groupBox3.Location = new System.Drawing.Point(401, 13);
            this.groupBox3.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.groupBox3.Name = "groupBox3";
            this.groupBox3.Padding = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.groupBox3.Size = new System.Drawing.Size(383, 63);
            this.groupBox3.TabIndex = 14;
            this.groupBox3.TabStop = false;
            // 
            // textBoxApellidosEmpleado
            // 
            this.textBoxApellidosEmpleado.Location = new System.Drawing.Point(197, 29);
            this.textBoxApellidosEmpleado.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.textBoxApellidosEmpleado.Name = "textBoxApellidosEmpleado";
            this.textBoxApellidosEmpleado.Size = new System.Drawing.Size(176, 25);
            this.textBoxApellidosEmpleado.TabIndex = 21;
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(193, 9);
            this.label7.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(67, 15);
            this.label7.TabIndex = 20;
            this.label7.Text = "员工姓氏";
            // 
            // textBoxNombreEmpleado
            // 
            this.textBoxNombreEmpleado.Location = new System.Drawing.Point(12, 29);
            this.textBoxNombreEmpleado.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.textBoxNombreEmpleado.Name = "textBoxNombreEmpleado";
            this.textBoxNombreEmpleado.Size = new System.Drawing.Size(176, 25);
            this.textBoxNombreEmpleado.TabIndex = 19;
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(8, 10);
            this.label4.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(67, 15);
            this.label4.TabIndex = 11;
            this.label4.Text = "员工名字";
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.label1);
            this.groupBox2.Controls.Add(this.textBoxDocumentoEmpleado);
            this.groupBox2.Controls.Add(this.radioButtonTodosEmpleados);
            this.groupBox2.Location = new System.Drawing.Point(8, 13);
            this.groupBox2.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Padding = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.groupBox2.Size = new System.Drawing.Size(385, 63);
            this.groupBox2.TabIndex = 13;
            this.groupBox2.TabStop = false;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(4, 10);
            this.label1.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(67, 15);
            this.label1.TabIndex = 5;
            this.label1.Text = "员工文档";
            // 
            // textBoxDocumentoEmpleado
            // 
            this.textBoxDocumentoEmpleado.Location = new System.Drawing.Point(8, 29);
            this.textBoxDocumentoEmpleado.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.textBoxDocumentoEmpleado.Name = "textBoxDocumentoEmpleado";
            this.textBoxDocumentoEmpleado.Size = new System.Drawing.Size(165, 25);
            this.textBoxDocumentoEmpleado.TabIndex = 1;
            this.textBoxDocumentoEmpleado.Click += new System.EventHandler(this.textBoxDocumentoEmpleado_Click);
            // 
            // radioButtonTodosEmpleados
            // 
            this.radioButtonTodosEmpleados.AutoSize = true;
            this.radioButtonTodosEmpleados.Location = new System.Drawing.Point(211, 30);
            this.radioButtonTodosEmpleados.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.radioButtonTodosEmpleados.Name = "radioButtonTodosEmpleados";
            this.radioButtonTodosEmpleados.Size = new System.Drawing.Size(88, 19);
            this.radioButtonTodosEmpleados.TabIndex = 2;
            this.radioButtonTodosEmpleados.TabStop = true;
            this.radioButtonTodosEmpleados.Text = "所有员工";
            this.radioButtonTodosEmpleados.UseVisualStyleBackColor = true;
            this.radioButtonTodosEmpleados.CheckedChanged += new System.EventHandler(this.radioButtonTodosEmpleados_CheckedChanged);
            // 
            // buttonVerInforme
            // 
            this.buttonVerInforme.Image = global::ControlEntradaSalida.Properties.Resources.Report_16x;
            this.buttonVerInforme.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.buttonVerInforme.Location = new System.Drawing.Point(644, 241);
            this.buttonVerInforme.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.buttonVerInforme.Name = "buttonVerInforme";
            this.buttonVerInforme.Size = new System.Drawing.Size(143, 27);
            this.buttonVerInforme.TabIndex = 13;
            this.buttonVerInforme.Text = "查询报告";
            this.buttonVerInforme.UseVisualStyleBackColor = true;
            this.buttonVerInforme.Click += new System.EventHandler(this.buttonVerInforme_Click);
            // 
            // listView
            // 
            this.listView.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.num,
            this.documento,
            this.nombres,
            this.apellidos,
            this.fecha,
            this.hora});
            this.listView.HideSelection = false;
            this.listView.Location = new System.Drawing.Point(16, 295);
            this.listView.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.listView.Name = "listView";
            this.listView.Size = new System.Drawing.Size(792, 320);
            this.listView.TabIndex = 14;
            this.listView.UseCompatibleStateImageBehavior = false;
            this.listView.View = System.Windows.Forms.View.Details;
            // 
            // num
            // 
            this.num.Text = "序号";
            // 
            // documento
            // 
            this.documento.Text = "文档";
            this.documento.Width = 70;
            // 
            // nombres
            // 
            this.nombres.Text = "名字";
            this.nombres.Width = 92;
            // 
            // apellidos
            // 
            this.apellidos.Text = "姓氏";
            this.apellidos.Width = 87;
            // 
            // fecha
            // 
            this.fecha.Text = "日期";
            // 
            // hora
            // 
            this.hora.Text = "时刻";
            // 
            // ParamInformeEventos
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(828, 630);
            this.Controls.Add(this.listView);
            this.Controls.Add(this.groupBox1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "ParamInformeEventos";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "事件日志";
            this.Load += new System.EventHandler(this.ParamInformeConsumos_Load);
            this.groupBox1.ResumeLayout(false);
            this.groupBox5.ResumeLayout(false);
            this.groupBox5.PerformLayout();
            this.groupBox4.ResumeLayout(false);
            this.groupBox4.PerformLayout();
            this.groupBox3.ResumeLayout(false);
            this.groupBox3.PerformLayout();
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.Button buttonVerInforme;
        private System.Windows.Forms.RadioButton radioButtonRangoFechas;
        private System.Windows.Forms.RadioButton radioButtonTodasFechas;
        private System.Windows.Forms.RadioButton radioButtonTodosEmpleados;
        private System.Windows.Forms.GroupBox groupBox4;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.DateTimePicker dateTimePickerFechaFinal;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.DateTimePicker dateTimePickerFechaInicial;
        private System.Windows.Forms.GroupBox groupBox3;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox textBoxDocumentoEmpleado;
        private System.Windows.Forms.ListView listView;
        private System.Windows.Forms.ColumnHeader documento;
        private System.Windows.Forms.ColumnHeader nombres;
        private System.Windows.Forms.ColumnHeader apellidos;
        private System.Windows.Forms.GroupBox groupBox5;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.DateTimePicker dateTimePickerHoraFinal;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.DateTimePicker dateTimePickerHoraInicial;
        private System.Windows.Forms.RadioButton radioButtonTodasHoras;
        private System.Windows.Forms.RadioButton radioButtonRangoHoras;
        private System.Windows.Forms.TextBox textBoxNombreEmpleado;
        private System.Windows.Forms.ColumnHeader num;
        private System.Windows.Forms.ColumnHeader fecha;
        private System.Windows.Forms.ColumnHeader hora;
        private System.Windows.Forms.TextBox textBoxApellidosEmpleado;
        private System.Windows.Forms.Label label7;
    }
}