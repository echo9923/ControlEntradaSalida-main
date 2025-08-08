
namespace ControlEntradaSalida
{
    partial class HolidayGroupPlan
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
            this.checkBoxEnableHG = new System.Windows.Forms.CheckBox();
            this.textBoxLocalControllerID = new System.Windows.Forms.TextBox();
            this.textBoxHGName = new System.Windows.Forms.TextBox();
            this.label4 = new System.Windows.Forms.Label();
            this.textBoxHGNumber = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.cbDeviceType = new System.Windows.Forms.ComboBox();
            this.listViewHG = new System.Windows.Forms.ListView();
            this.columnHeader1 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader2 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.btnUpdate = new System.Windows.Forms.Button();
            this.textBoxHolidayPlanNo = new System.Windows.Forms.TextBox();
            this.label5 = new System.Windows.Forms.Label();
            this.btnGetTemplate = new System.Windows.Forms.Button();
            this.btnSetTemplate = new System.Windows.Forms.Button();
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
            this.groupBox1.Controls.Add(this.checkBoxEnableHG);
            this.groupBox1.Controls.Add(this.textBoxLocalControllerID);
            this.groupBox1.Controls.Add(this.textBoxHGName);
            this.groupBox1.Controls.Add(this.label4);
            this.groupBox1.Controls.Add(this.textBoxHGNumber);
            this.groupBox1.Controls.Add(this.label3);
            this.groupBox1.Controls.Add(this.label2);
            this.groupBox1.Controls.Add(this.cbDeviceType);
            this.groupBox1.Controls.Add(this.label1);
            this.groupBox1.Location = new System.Drawing.Point(12, 12);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(507, 118);
            this.groupBox1.TabIndex = 1;
            this.groupBox1.TabStop = false;
            // 
            // checkBoxEnableHG
            // 
            this.checkBoxEnableHG.AutoSize = true;
            this.checkBoxEnableHG.Checked = true;
            this.checkBoxEnableHG.CheckState = System.Windows.Forms.CheckState.Checked;
            this.checkBoxEnableHG.Location = new System.Drawing.Point(9, 94);
            this.checkBoxEnableHG.Name = "checkBoxEnableHG";
            this.checkBoxEnableHG.Size = new System.Drawing.Size(62, 22);
            this.checkBoxEnableHG.TabIndex = 6;
            this.checkBoxEnableHG.Text = "使能";
            this.checkBoxEnableHG.UseVisualStyleBackColor = true;
            // 
            // textBoxLocalControllerID
            // 
            this.textBoxLocalControllerID.Location = new System.Drawing.Point(410, 55);
            this.textBoxLocalControllerID.Name = "textBoxLocalControllerID";
            this.textBoxLocalControllerID.Size = new System.Drawing.Size(85, 25);
            this.textBoxLocalControllerID.TabIndex = 6;
            this.textBoxLocalControllerID.Text = "0";
            this.textBoxLocalControllerID.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.textBoxLocalControllerID_KeyPress);
            // 
            // textBoxHGName
            // 
            this.textBoxHGName.Location = new System.Drawing.Point(146, 55);
            this.textBoxHGName.Name = "textBoxHGName";
            this.textBoxHGName.Size = new System.Drawing.Size(100, 25);
            this.textBoxHGName.TabIndex = 5;
            this.textBoxHGName.Text = "test";
            this.textBoxHGName.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.textBoxHGName_KeyPress);
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(271, 58);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(112, 18);
            this.label4.TabIndex = 3;
            this.label4.Text = "就地控制器ID:";
            // 
            // textBoxHGNumber
            // 
            this.textBoxHGNumber.Location = new System.Drawing.Point(353, 15);
            this.textBoxHGNumber.Name = "textBoxHGNumber";
            this.textBoxHGNumber.Size = new System.Drawing.Size(92, 25);
            this.textBoxHGNumber.TabIndex = 4;
            this.textBoxHGNumber.Text = "1";
            this.textBoxHGNumber.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.textBoxHGNumber_KeyPress);
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(200, 18);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(128, 18);
            this.label3.TabIndex = 3;
            this.label3.Text = "假日组计划编号:";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(6, 58);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(96, 18);
            this.label2.TabIndex = 2;
            this.label2.Text = "假日组名称:";
            // 
            // cbDeviceType
            // 
            this.cbDeviceType.FormattingEnabled = true;
            this.cbDeviceType.Items.AddRange(new object[] {
            "Card",
            "CardReader",
            "Door"});
            this.cbDeviceType.Location = new System.Drawing.Point(86, 15);
            this.cbDeviceType.Name = "cbDeviceType";
            this.cbDeviceType.Size = new System.Drawing.Size(108, 26);
            this.cbDeviceType.TabIndex = 1;
            this.cbDeviceType.Text = "Card";
            // 
            // listViewHG
            // 
            this.listViewHG.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnHeader1,
            this.columnHeader2});
            this.listViewHG.FullRowSelect = true;
            this.listViewHG.GridLines = true;
            this.listViewHG.HideSelection = false;
            this.listViewHG.Location = new System.Drawing.Point(12, 136);
            this.listViewHG.Name = "listViewHG";
            this.listViewHG.Size = new System.Drawing.Size(203, 222);
            this.listViewHG.TabIndex = 2;
            this.listViewHG.UseCompatibleStateImageBehavior = false;
            this.listViewHG.View = System.Windows.Forms.View.Details;
            this.listViewHG.ItemSelectionChanged += new System.Windows.Forms.ListViewItemSelectionChangedEventHandler(this.listViewHG_ItemSelectionChanged);
            // 
            // columnHeader1
            // 
            this.columnHeader1.Text = "No.";
            this.columnHeader1.Width = 61;
            // 
            // columnHeader2
            // 
            this.columnHeader2.Text = "假日计划 No";
            this.columnHeader2.Width = 130;
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.btnUpdate);
            this.groupBox2.Controls.Add(this.textBoxHolidayPlanNo);
            this.groupBox2.Controls.Add(this.label5);
            this.groupBox2.Location = new System.Drawing.Point(237, 149);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(282, 152);
            this.groupBox2.TabIndex = 3;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "假日计划配置";
            // 
            // btnUpdate
            // 
            this.btnUpdate.Location = new System.Drawing.Point(131, 97);
            this.btnUpdate.Name = "btnUpdate";
            this.btnUpdate.Size = new System.Drawing.Size(75, 23);
            this.btnUpdate.TabIndex = 2;
            this.btnUpdate.Text = "更新";
            this.btnUpdate.UseVisualStyleBackColor = true;
            this.btnUpdate.Click += new System.EventHandler(this.btnUpdate_Click);
            // 
            // textBoxHolidayPlanNo
            // 
            this.textBoxHolidayPlanNo.Location = new System.Drawing.Point(131, 49);
            this.textBoxHolidayPlanNo.Name = "textBoxHolidayPlanNo";
            this.textBoxHolidayPlanNo.Size = new System.Drawing.Size(75, 25);
            this.textBoxHolidayPlanNo.TabIndex = 1;
            this.textBoxHolidayPlanNo.Text = "1";
            this.textBoxHolidayPlanNo.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.textBoxHolidayPlanNo_KeyPress);
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(20, 52);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(96, 18);
            this.label5.TabIndex = 0;
            this.label5.Text = "假日计划No:";
            // 
            // btnGetTemplate
            // 
            this.btnGetTemplate.Location = new System.Drawing.Point(237, 326);
            this.btnGetTemplate.Name = "btnGetTemplate";
            this.btnGetTemplate.Size = new System.Drawing.Size(75, 23);
            this.btnGetTemplate.TabIndex = 4;
            this.btnGetTemplate.Text = "获取";
            this.btnGetTemplate.UseVisualStyleBackColor = true;
            this.btnGetTemplate.Click += new System.EventHandler(this.btnGetTemplate_Click);
            // 
            // btnSetTemplate
            // 
            this.btnSetTemplate.Location = new System.Drawing.Point(403, 326);
            this.btnSetTemplate.Name = "btnSetTemplate";
            this.btnSetTemplate.Size = new System.Drawing.Size(75, 23);
            this.btnSetTemplate.TabIndex = 5;
            this.btnSetTemplate.Text = "设置";
            this.btnSetTemplate.UseVisualStyleBackColor = true;
            this.btnSetTemplate.Click += new System.EventHandler(this.btnSetTemplate_Click);
            // 
            // HolidayGroupPlan
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 18F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(531, 391);
            this.Controls.Add(this.btnSetTemplate);
            this.Controls.Add(this.btnGetTemplate);
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.listViewHG);
            this.Controls.Add(this.groupBox1);
            this.Font = new System.Drawing.Font("Consolas", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
            this.Name = "HolidayGroupPlan";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "HolidayGroupPlan";
            this.Load += new System.EventHandler(this.HolidayGroupPlan_Load);
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
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.TextBox textBoxHGName;
        private System.Windows.Forms.TextBox textBoxHGNumber;
        private System.Windows.Forms.ListView listViewHG;
        private System.Windows.Forms.ColumnHeader columnHeader1;
        private System.Windows.Forms.ColumnHeader columnHeader2;
        private System.Windows.Forms.CheckBox checkBoxEnableHG;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.TextBox textBoxLocalControllerID;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.TextBox textBoxHolidayPlanNo;
        private System.Windows.Forms.Button btnUpdate;
        private System.Windows.Forms.Button btnGetTemplate;
        private System.Windows.Forms.Button btnSetTemplate;
    }
}