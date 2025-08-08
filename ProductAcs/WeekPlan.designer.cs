
namespace ControlEntradaSalida
{
    partial class WeekPlan
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
            this.label2 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.checkBoxEnableWP = new System.Windows.Forms.CheckBox();
            this.gBox = new System.Windows.Forms.GroupBox();
            this.textBoxLCID = new System.Windows.Forms.TextBox();
            this.textBoxWPNumber = new System.Windows.Forms.TextBox();
            this.cbDeviceType = new System.Windows.Forms.ComboBox();
            this.gBoxTime = new System.Windows.Forms.GroupBox();
            this.gBoxEditTime = new System.Windows.Forms.GroupBox();
            this.cbDoorStateMode = new System.Windows.Forms.ComboBox();
            this.cbVerifyMode = new System.Windows.Forms.ComboBox();
            this.label8 = new System.Windows.Forms.Label();
            this.label7 = new System.Windows.Forms.Label();
            this.btnAdd = new System.Windows.Forms.Button();
            this.label5 = new System.Windows.Forms.Label();
            this.checkBoxEnableTime = new System.Windows.Forms.CheckBox();
            this.dTPStartTime = new System.Windows.Forms.DateTimePicker();
            this.dTPEndTime = new System.Windows.Forms.DateTimePicker();
            this.label6 = new System.Windows.Forms.Label();
            this.listViewTimeSegment = new System.Windows.Forms.ListView();
            this.columnHeader1 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader2 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader3 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader4 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader5 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader6 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.cbDate = new System.Windows.Forms.ComboBox();
            this.label4 = new System.Windows.Forms.Label();
            this.btnGet = new System.Windows.Forms.Button();
            this.btnSet = new System.Windows.Forms.Button();
            this.gBox.SuspendLayout();
            this.gBoxTime.SuspendLayout();
            this.gBoxEditTime.SuspendLayout();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(17, 16);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(80, 18);
            this.label1.TabIndex = 0;
            this.label1.Text = "设备类型:";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(394, 16);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(96, 18);
            this.label2.TabIndex = 1;
            this.label2.Text = "周计划编号:";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(390, 56);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(112, 18);
            this.label3.TabIndex = 2;
            this.label3.Text = "就地控制器ID:";
            // 
            // checkBoxEnableWP
            // 
            this.checkBoxEnableWP.AutoSize = true;
            this.checkBoxEnableWP.Checked = true;
            this.checkBoxEnableWP.CheckState = System.Windows.Forms.CheckState.Checked;
            this.checkBoxEnableWP.Location = new System.Drawing.Point(20, 56);
            this.checkBoxEnableWP.Name = "checkBoxEnableWP";
            this.checkBoxEnableWP.Size = new System.Drawing.Size(62, 22);
            this.checkBoxEnableWP.TabIndex = 3;
            this.checkBoxEnableWP.Text = "使能";
            this.checkBoxEnableWP.UseVisualStyleBackColor = true;
            // 
            // gBox
            // 
            this.gBox.Controls.Add(this.textBoxLCID);
            this.gBox.Controls.Add(this.textBoxWPNumber);
            this.gBox.Controls.Add(this.cbDeviceType);
            this.gBox.Controls.Add(this.checkBoxEnableWP);
            this.gBox.Controls.Add(this.label3);
            this.gBox.Controls.Add(this.label2);
            this.gBox.Controls.Add(this.label1);
            this.gBox.Location = new System.Drawing.Point(12, 10);
            this.gBox.Name = "gBox";
            this.gBox.Size = new System.Drawing.Size(651, 90);
            this.gBox.TabIndex = 4;
            this.gBox.TabStop = false;
            // 
            // textBoxLCID
            // 
            this.textBoxLCID.Location = new System.Drawing.Point(527, 53);
            this.textBoxLCID.Name = "textBoxLCID";
            this.textBoxLCID.Size = new System.Drawing.Size(100, 25);
            this.textBoxLCID.TabIndex = 6;
            this.textBoxLCID.Text = "0";
            this.textBoxLCID.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.textBoxLCID_KeyPress);
            // 
            // textBoxWPNumber
            // 
            this.textBoxWPNumber.Location = new System.Drawing.Point(526, 13);
            this.textBoxWPNumber.Name = "textBoxWPNumber";
            this.textBoxWPNumber.Size = new System.Drawing.Size(100, 25);
            this.textBoxWPNumber.TabIndex = 5;
            this.textBoxWPNumber.Text = "1";
            this.textBoxWPNumber.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.textBoxWPNumber_KeyPress);
            // 
            // cbDeviceType
            // 
            this.cbDeviceType.FormattingEnabled = true;
            this.cbDeviceType.Items.AddRange(new object[] {
            "Card",
            "CardReader",
            "Door"});
            this.cbDeviceType.Location = new System.Drawing.Point(108, 13);
            this.cbDeviceType.Name = "cbDeviceType";
            this.cbDeviceType.Size = new System.Drawing.Size(121, 26);
            this.cbDeviceType.TabIndex = 4;
            this.cbDeviceType.Text = "Card";
            this.cbDeviceType.SelectedIndexChanged += new System.EventHandler(this.cbDeviceType_SelectedIndexChanged);
            // 
            // gBoxTime
            // 
            this.gBoxTime.Controls.Add(this.gBoxEditTime);
            this.gBoxTime.Controls.Add(this.listViewTimeSegment);
            this.gBoxTime.Controls.Add(this.cbDate);
            this.gBoxTime.Controls.Add(this.label4);
            this.gBoxTime.Location = new System.Drawing.Point(12, 119);
            this.gBoxTime.Name = "gBoxTime";
            this.gBoxTime.Size = new System.Drawing.Size(651, 358);
            this.gBoxTime.TabIndex = 5;
            this.gBoxTime.TabStop = false;
            this.gBoxTime.Text = "时间设置";
            // 
            // gBoxEditTime
            // 
            this.gBoxEditTime.Controls.Add(this.cbDoorStateMode);
            this.gBoxEditTime.Controls.Add(this.cbVerifyMode);
            this.gBoxEditTime.Controls.Add(this.label8);
            this.gBoxEditTime.Controls.Add(this.label7);
            this.gBoxEditTime.Controls.Add(this.btnAdd);
            this.gBoxEditTime.Controls.Add(this.label5);
            this.gBoxEditTime.Controls.Add(this.checkBoxEnableTime);
            this.gBoxEditTime.Controls.Add(this.dTPStartTime);
            this.gBoxEditTime.Controls.Add(this.dTPEndTime);
            this.gBoxEditTime.Controls.Add(this.label6);
            this.gBoxEditTime.Location = new System.Drawing.Point(21, 236);
            this.gBoxEditTime.Name = "gBoxEditTime";
            this.gBoxEditTime.Size = new System.Drawing.Size(613, 109);
            this.gBoxEditTime.TabIndex = 8;
            this.gBoxEditTime.TabStop = false;
            this.gBoxEditTime.Text = "编辑时间";
            // 
            // cbDoorStateMode
            // 
            this.cbDoorStateMode.FormattingEnabled = true;
            this.cbDoorStateMode.Items.AddRange(new object[] {
            "Invalid",
            "Sleep",
            "NormallyOpen",
            "NormallyClose",
            "normalState"});
            this.cbDoorStateMode.Location = new System.Drawing.Point(391, 69);
            this.cbDoorStateMode.Name = "cbDoorStateMode";
            this.cbDoorStateMode.Size = new System.Drawing.Size(184, 26);
            this.cbDoorStateMode.TabIndex = 12;
            // 
            // cbVerifyMode
            // 
            this.cbVerifyMode.FormattingEnabled = true;
            this.cbVerifyMode.Items.AddRange(new object[] {
            "0-Invalid",
            "1-card",
            "2-cardAndPw",
            "3-card",
            "4-cardOrPw",
            "5-Fp",
            "6-FpAndPw",
            "7-FpOrCard",
            "8-FpAndCard",
            "9-FpAndCardAndPw",
            "10-faceOrFpOrCardOrPw",
            "11-faceAndFp",
            "12-faceAndPw",
            "13-faceAndCard",
            "14-face",
            "15-employeeNoAndPw",
            "16-FpOrPw",
            "17-employeeNoAndFp",
            "18-employeeAndFpAndPw",
            "19-faceAndFpAndCard",
            "20-faceAndPwAndFp",
            "21-empolyeeNoAndFace",
            "22-faceOrFaceAndCard",
            "23-FpOrFace",
            "24-cardOrFaceOrPw",
            "25-cardOrFace",
            "26-cardOrFaceOrFp",
            "27-cardOrFpOrPw"});
            this.cbVerifyMode.Location = new System.Drawing.Point(391, 69);
            this.cbVerifyMode.Name = "cbVerifyMode";
            this.cbVerifyMode.Size = new System.Drawing.Size(184, 26);
            this.cbVerifyMode.TabIndex = 11;
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Location = new System.Drawing.Point(273, 71);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(96, 18);
            this.label8.TabIndex = 10;
            this.label8.Text = "门状态模式:";
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(273, 71);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(96, 18);
            this.label7.TabIndex = 9;
            this.label7.Text = "VerifyMode:";
            // 
            // btnAdd
            // 
            this.btnAdd.Location = new System.Drawing.Point(97, 68);
            this.btnAdd.Name = "btnAdd";
            this.btnAdd.Size = new System.Drawing.Size(75, 23);
            this.btnAdd.TabIndex = 8;
            this.btnAdd.Text = "更新";
            this.btnAdd.UseVisualStyleBackColor = true;
            this.btnAdd.Click += new System.EventHandler(this.btnAdd_Click);
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(7, 27);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(80, 18);
            this.label5.TabIndex = 3;
            this.label5.Text = "开始时间:";
            // 
            // checkBoxEnableTime
            // 
            this.checkBoxEnableTime.AutoSize = true;
            this.checkBoxEnableTime.Location = new System.Drawing.Point(10, 70);
            this.checkBoxEnableTime.Name = "checkBoxEnableTime";
            this.checkBoxEnableTime.Size = new System.Drawing.Size(62, 22);
            this.checkBoxEnableTime.TabIndex = 7;
            this.checkBoxEnableTime.Text = "使能";
            this.checkBoxEnableTime.UseVisualStyleBackColor = true;
            // 
            // dTPStartTime
            // 
            this.dTPStartTime.Format = System.Windows.Forms.DateTimePickerFormat.Time;
            this.dTPStartTime.Location = new System.Drawing.Point(97, 24);
            this.dTPStartTime.Name = "dTPStartTime";
            this.dTPStartTime.ShowUpDown = true;
            this.dTPStartTime.Size = new System.Drawing.Size(102, 25);
            this.dTPStartTime.TabIndex = 5;
            // 
            // dTPEndTime
            // 
            this.dTPEndTime.Format = System.Windows.Forms.DateTimePickerFormat.Time;
            this.dTPEndTime.Location = new System.Drawing.Point(356, 24);
            this.dTPEndTime.Name = "dTPEndTime";
            this.dTPEndTime.ShowUpDown = true;
            this.dTPEndTime.Size = new System.Drawing.Size(102, 25);
            this.dTPEndTime.TabIndex = 6;
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(273, 27);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(80, 18);
            this.label6.TabIndex = 4;
            this.label6.Text = "结束时间:";
            // 
            // listViewTimeSegment
            // 
            this.listViewTimeSegment.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.listViewTimeSegment.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnHeader1,
            this.columnHeader2,
            this.columnHeader3,
            this.columnHeader4,
            this.columnHeader5,
            this.columnHeader6});
            this.listViewTimeSegment.FullRowSelect = true;
            this.listViewTimeSegment.GridLines = true;
            this.listViewTimeSegment.HideSelection = false;
            this.listViewTimeSegment.Location = new System.Drawing.Point(20, 49);
            this.listViewTimeSegment.Name = "listViewTimeSegment";
            this.listViewTimeSegment.Size = new System.Drawing.Size(614, 183);
            this.listViewTimeSegment.TabIndex = 2;
            this.listViewTimeSegment.UseCompatibleStateImageBehavior = false;
            this.listViewTimeSegment.View = System.Windows.Forms.View.Details;
            this.listViewTimeSegment.ItemSelectionChanged += new System.Windows.Forms.ListViewItemSelectionChangedEventHandler(this.listViewTimeSegment_ItemSelectionChanged);
            // 
            // columnHeader1
            // 
            this.columnHeader1.Text = "Time Segment";
            this.columnHeader1.Width = 123;
            // 
            // columnHeader2
            // 
            this.columnHeader2.Text = "Enable";
            this.columnHeader2.Width = 101;
            // 
            // columnHeader3
            // 
            this.columnHeader3.Text = "Start Time";
            this.columnHeader3.Width = 132;
            // 
            // columnHeader4
            // 
            this.columnHeader4.Text = "End Time";
            this.columnHeader4.Width = 132;
            // 
            // columnHeader5
            // 
            this.columnHeader5.Text = "VerifyMode";
            this.columnHeader5.Width = 110;
            // 
            // columnHeader6
            // 
            this.columnHeader6.Text = "DoorStatusMode";
            this.columnHeader6.Width = 110;
            // 
            // cbDate
            // 
            this.cbDate.FormattingEnabled = true;
            this.cbDate.Items.AddRange(new object[] {
            "Monday",
            "Tuesday",
            "Wednesday",
            "Thursday",
            "Friday",
            "Saturday",
            "Sunday"});
            this.cbDate.Location = new System.Drawing.Point(278, 21);
            this.cbDate.Name = "cbDate";
            this.cbDate.Size = new System.Drawing.Size(121, 26);
            this.cbDate.TabIndex = 1;
            this.cbDate.Text = "Monday";
            this.cbDate.SelectedIndexChanged += new System.EventHandler(this.cbDate_SelectedIndexChanged);
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(231, 24);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(48, 18);
            this.label4.TabIndex = 0;
            this.label4.Text = "星期:";
            // 
            // btnGet
            // 
            this.btnGet.Location = new System.Drawing.Point(93, 483);
            this.btnGet.Name = "btnGet";
            this.btnGet.Size = new System.Drawing.Size(75, 23);
            this.btnGet.TabIndex = 6;
            this.btnGet.Text = "获取";
            this.btnGet.UseVisualStyleBackColor = true;
            this.btnGet.Click += new System.EventHandler(this.btnGet_Click);
            // 
            // btnSet
            // 
            this.btnSet.Location = new System.Drawing.Point(498, 483);
            this.btnSet.Name = "btnSet";
            this.btnSet.Size = new System.Drawing.Size(75, 23);
            this.btnSet.TabIndex = 7;
            this.btnSet.Text = "设置";
            this.btnSet.UseVisualStyleBackColor = true;
            this.btnSet.Click += new System.EventHandler(this.btnSet_Click);
            // 
            // WeekPlan
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 18F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(675, 514);
            this.Controls.Add(this.btnSet);
            this.Controls.Add(this.btnGet);
            this.Controls.Add(this.gBoxTime);
            this.Controls.Add(this.gBox);
            this.Font = new System.Drawing.Font("Consolas", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
            this.Name = "WeekPlan";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "WeekPlan";
            this.Load += new System.EventHandler(this.WeekPlan_Load);
            this.gBox.ResumeLayout(false);
            this.gBox.PerformLayout();
            this.gBoxTime.ResumeLayout(false);
            this.gBoxTime.PerformLayout();
            this.gBoxEditTime.ResumeLayout(false);
            this.gBoxEditTime.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.CheckBox checkBoxEnableWP;
        private System.Windows.Forms.GroupBox gBox;
        private System.Windows.Forms.GroupBox gBoxTime;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.ComboBox cbDeviceType;
        private System.Windows.Forms.ComboBox cbDate;
        private System.Windows.Forms.TextBox textBoxWPNumber;
        private System.Windows.Forms.TextBox textBoxLCID;
        private System.Windows.Forms.ListView listViewTimeSegment;
        private System.Windows.Forms.ColumnHeader columnHeader1;
        private System.Windows.Forms.ColumnHeader columnHeader2;
        private System.Windows.Forms.ColumnHeader columnHeader3;
        private System.Windows.Forms.ColumnHeader columnHeader4;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.DateTimePicker dTPStartTime;
        private System.Windows.Forms.DateTimePicker dTPEndTime;
        private System.Windows.Forms.CheckBox checkBoxEnableTime;
        private System.Windows.Forms.GroupBox gBoxEditTime;
        private System.Windows.Forms.Button btnAdd;
        private System.Windows.Forms.Button btnGet;
        private System.Windows.Forms.Button btnSet;
        private System.Windows.Forms.ColumnHeader columnHeader5;
        private System.Windows.Forms.ColumnHeader columnHeader6;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.ComboBox cbVerifyMode;
        private System.Windows.Forms.ComboBox cbDoorStateMode;
    }
}