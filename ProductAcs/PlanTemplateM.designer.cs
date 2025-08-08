
namespace ControlEntradaSalida
{
    partial class PlanTemplateM
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
            this.label1 = new System.Windows.Forms.Label();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.checkBoxEnableTP = new System.Windows.Forms.CheckBox();
            this.textBoxLCID = new System.Windows.Forms.TextBox();
            this.label4 = new System.Windows.Forms.Label();
            this.textBoxPTName = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.textBoxPTNumber = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.cbDeviceType = new System.Windows.Forms.ComboBox();
            this.listViewTemplate = new System.Windows.Forms.ListView();
            this.columnHeader1 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader2 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.btnUpdate = new System.Windows.Forms.Button();
            this.textBoxWPN = new System.Windows.Forms.TextBox();
            this.textBoxHGPN = new System.Windows.Forms.TextBox();
            this.label6 = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.btnGetPT = new System.Windows.Forms.Button();
            this.btnSetPT = new System.Windows.Forms.Button();
            this.groupBox1.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(6, 18);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(80, 18);
            this.label1.TabIndex = 0;
            this.label1.Text = "设备类型:";
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.checkBoxEnableTP);
            this.groupBox1.Controls.Add(this.textBoxLCID);
            this.groupBox1.Controls.Add(this.label4);
            this.groupBox1.Controls.Add(this.textBoxPTName);
            this.groupBox1.Controls.Add(this.label3);
            this.groupBox1.Controls.Add(this.textBoxPTNumber);
            this.groupBox1.Controls.Add(this.label2);
            this.groupBox1.Controls.Add(this.cbDeviceType);
            this.groupBox1.Controls.Add(this.label1);
            this.groupBox1.Location = new System.Drawing.Point(12, 1);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(507, 110);
            this.groupBox1.TabIndex = 1;
            this.groupBox1.TabStop = false;
            // 
            // checkBoxEnableTP
            // 
            this.checkBoxEnableTP.AutoSize = true;
            this.checkBoxEnableTP.Checked = true;
            this.checkBoxEnableTP.CheckState = System.Windows.Forms.CheckState.Checked;
            this.checkBoxEnableTP.Location = new System.Drawing.Point(9, 86);
            this.checkBoxEnableTP.Name = "checkBoxEnableTP";
            this.checkBoxEnableTP.Size = new System.Drawing.Size(62, 22);
            this.checkBoxEnableTP.TabIndex = 8;
            this.checkBoxEnableTP.Text = "使能";
            this.checkBoxEnableTP.UseVisualStyleBackColor = true;
            // 
            // textBoxLCID
            // 
            this.textBoxLCID.Location = new System.Drawing.Point(386, 54);
            this.textBoxLCID.Name = "textBoxLCID";
            this.textBoxLCID.Size = new System.Drawing.Size(94, 25);
            this.textBoxLCID.TabIndex = 7;
            this.textBoxLCID.Text = "0";
            this.textBoxLCID.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.textBoxLCID_KeyPress);
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(247, 57);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(112, 18);
            this.label4.TabIndex = 6;
            this.label4.Text = "就地控制器ID:";
            // 
            // textBoxPTName
            // 
            this.textBoxPTName.Location = new System.Drawing.Point(138, 54);
            this.textBoxPTName.Name = "textBoxPTName";
            this.textBoxPTName.Size = new System.Drawing.Size(83, 25);
            this.textBoxPTName.TabIndex = 5;
            this.textBoxPTName.Text = "test";
            this.textBoxPTName.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.textBoxPTName_KeyPress);
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(6, 57);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(112, 18);
            this.label3.TabIndex = 4;
            this.label3.Text = "计划模板名称:";
            // 
            // textBoxPTNumber
            // 
            this.textBoxPTNumber.Location = new System.Drawing.Point(372, 15);
            this.textBoxPTNumber.Name = "textBoxPTNumber";
            this.textBoxPTNumber.Size = new System.Drawing.Size(89, 25);
            this.textBoxPTNumber.TabIndex = 3;
            this.textBoxPTNumber.Text = "1";
            this.textBoxPTNumber.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.textBoxPTNumber_KeyPress);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(226, 18);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(112, 18);
            this.label2.TabIndex = 2;
            this.label2.Text = "计划模板编号:";
            // 
            // cbDeviceType
            // 
            this.cbDeviceType.FormattingEnabled = true;
            this.cbDeviceType.Items.AddRange(new object[] {
            "Card",
            "CardReader",
            "Door"});
            this.cbDeviceType.Location = new System.Drawing.Point(103, 15);
            this.cbDeviceType.Name = "cbDeviceType";
            this.cbDeviceType.Size = new System.Drawing.Size(78, 26);
            this.cbDeviceType.TabIndex = 1;
            this.cbDeviceType.Text = "Card";
            // 
            // listViewTemplate
            // 
            this.listViewTemplate.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnHeader1,
            this.columnHeader2});
            this.listViewTemplate.FullRowSelect = true;
            this.listViewTemplate.GridLines = true;
            this.listViewTemplate.HideSelection = false;
            this.listViewTemplate.Location = new System.Drawing.Point(12, 128);
            this.listViewTemplate.Name = "listViewTemplate";
            this.listViewTemplate.Size = new System.Drawing.Size(208, 170);
            this.listViewTemplate.TabIndex = 2;
            this.listViewTemplate.UseCompatibleStateImageBehavior = false;
            this.listViewTemplate.View = System.Windows.Forms.View.Details;
            this.listViewTemplate.ItemSelectionChanged += new System.Windows.Forms.ListViewItemSelectionChangedEventHandler(this.listViewTemplate_ItemSelectionChanged);
            // 
            // columnHeader1
            // 
            this.columnHeader1.Text = "No.";
            // 
            // columnHeader2
            // 
            this.columnHeader2.Text = "假日计划组编号No";
            this.columnHeader2.Width = 150;
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.btnUpdate);
            this.groupBox2.Controls.Add(this.textBoxWPN);
            this.groupBox2.Controls.Add(this.textBoxHGPN);
            this.groupBox2.Controls.Add(this.label6);
            this.groupBox2.Controls.Add(this.label5);
            this.groupBox2.Location = new System.Drawing.Point(241, 117);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(278, 181);
            this.groupBox2.TabIndex = 3;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "模板配置";
            // 
            // btnUpdate
            // 
            this.btnUpdate.Location = new System.Drawing.Point(157, 137);
            this.btnUpdate.Name = "btnUpdate";
            this.btnUpdate.Size = new System.Drawing.Size(75, 23);
            this.btnUpdate.TabIndex = 4;
            this.btnUpdate.Text = "更新";
            this.btnUpdate.UseVisualStyleBackColor = true;
            this.btnUpdate.Click += new System.EventHandler(this.btnUpdate_Click);
            // 
            // textBoxWPN
            // 
            this.textBoxWPN.Location = new System.Drawing.Point(157, 95);
            this.textBoxWPN.Name = "textBoxWPN";
            this.textBoxWPN.Size = new System.Drawing.Size(100, 25);
            this.textBoxWPN.TabIndex = 3;
            this.textBoxWPN.Text = "1";
            this.textBoxWPN.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.textBoxWPN_KeyPress);
            // 
            // textBoxHGPN
            // 
            this.textBoxHGPN.Location = new System.Drawing.Point(157, 47);
            this.textBoxHGPN.Name = "textBoxHGPN";
            this.textBoxHGPN.Size = new System.Drawing.Size(100, 25);
            this.textBoxHGPN.TabIndex = 2;
            this.textBoxHGPN.Text = "1";
            this.textBoxHGPN.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.textBoxHGPN_KeyPress);
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(18, 95);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(112, 18);
            this.label6.TabIndex = 1;
            this.label6.Text = "周计划编号No:";
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(18, 50);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(144, 18);
            this.label5.TabIndex = 0;
            this.label5.Text = "假日计划组编号No:";
            // 
            // btnGetPT
            // 
            this.btnGetPT.Location = new System.Drawing.Point(241, 305);
            this.btnGetPT.Name = "btnGetPT";
            this.btnGetPT.Size = new System.Drawing.Size(75, 23);
            this.btnGetPT.TabIndex = 4;
            this.btnGetPT.Text = "获取";
            this.btnGetPT.UseVisualStyleBackColor = true;
            this.btnGetPT.Click += new System.EventHandler(this.btnGetPT_Click);
            // 
            // btnSetPT
            // 
            this.btnSetPT.Location = new System.Drawing.Point(398, 305);
            this.btnSetPT.Name = "btnSetPT";
            this.btnSetPT.Size = new System.Drawing.Size(75, 23);
            this.btnSetPT.TabIndex = 5;
            this.btnSetPT.Text = "设置";
            this.btnSetPT.UseVisualStyleBackColor = true;
            this.btnSetPT.Click += new System.EventHandler(this.btnSetPT_Click);
            // 
            // PlanTemplateM
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 18F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(531, 340);
            this.Controls.Add(this.btnSetPT);
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.btnGetPT);
            this.Controls.Add(this.listViewTemplate);
            this.Controls.Add(this.groupBox1);
            this.Font = new System.Drawing.Font("Consolas", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
            this.Name = "PlanTemplateM";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "PlanTemplateM";
            this.Load += new System.EventHandler(this.PlanTemplateM_Load);
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.ComboBox cbDeviceType;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox textBoxPTNumber;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.TextBox textBoxPTName;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.TextBox textBoxLCID;
        private System.Windows.Forms.CheckBox checkBoxEnableTP;
        private System.Windows.Forms.ListView listViewTemplate;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.Button btnUpdate;
        private System.Windows.Forms.TextBox textBoxWPN;
        private System.Windows.Forms.TextBox textBoxHGPN;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Button btnGetPT;
        private System.Windows.Forms.Button btnSetPT;
        private System.Windows.Forms.ColumnHeader columnHeader1;
        private System.Windows.Forms.ColumnHeader columnHeader2;
    }
}