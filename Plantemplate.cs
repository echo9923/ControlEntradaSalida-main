using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using MySql.Data.MySqlClient;

namespace ControlEntradaSalida
{   //设备管理界面的功能
    public partial class Plantemplate : Form
    {
        public Plantemplate()
        {
            InitializeComponent();
        }
        

        private void Plantemplate_Load_1(object sender, EventArgs e)
        {
            if (Common.m_UserID < 0)
            {
                MessageBox.Show("您必须在设备上登录", "登录错误", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
        }

     

   

        private void Plantemplate_Load(object sender, EventArgs e)
        {

        }

        private void btnWeekPlan_Click(object sender, EventArgs e)
        {
            WeekPlan dlg = new WeekPlan();
            dlg.ShowDialog();
            dlg.Dispose();
        }

        private void btnHolidayPlan_Click(object sender, EventArgs e)
        {
            HolidayPlan dlg = new HolidayPlan();
            dlg.ShowDialog();
            dlg.Dispose();
        }

        private void btnHolidayGroup_Click(object sender, EventArgs e)
        {
            HolidayGroupPlan dlg = new HolidayGroupPlan();
            dlg.ShowDialog();
            dlg.Dispose();
        }

        private void btnPlanTemplate_Click(object sender, EventArgs e)
        {
            PlanTemplateM dlg = new PlanTemplateM();
            dlg.ShowDialog();
            dlg.Dispose();
        }
    }
}

