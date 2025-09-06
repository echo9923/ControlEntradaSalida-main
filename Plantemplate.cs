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
            // 获取当前连接的设备
            var connectedDevices = DeviceConnectionManager.Instance.GetAllDevices()
                .Where(d => d.IsConnected).ToList();
            if (connectedDevices.Count == 0)
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
            ShowChildDialogTopMost(dlg);
        }

        private void btnHolidayPlan_Click(object sender, EventArgs e)
        {
            HolidayPlan dlg = new HolidayPlan();
            ShowChildDialogTopMost(dlg);
        }

        private void btnHolidayGroup_Click(object sender, EventArgs e)
        {
            HolidayGroupPlan dlg = new HolidayGroupPlan();
            ShowChildDialogTopMost(dlg);
        }

        private void btnPlanTemplate_Click(object sender, EventArgs e)
        {
            PlanTemplateM dlg = new PlanTemplateM();
            ShowChildDialogTopMost(dlg);
        }
        
        /// <summary>
        /// 以模态对话框形式显示子窗体，确保始终位于父窗体之上
        /// </summary>
        /// <param name="form">要显示的窗体</param>
        private void ShowChildDialogTopMost(Form form)
        {
            if (form == null) return;

            try
            {
                // 设置子窗体属性确保置顶显示
                form.StartPosition = FormStartPosition.CenterParent;
                form.ShowInTaskbar = false;
                form.TopMost = true;
                
                // 以模态对话框形式显示，指定此窗体为父窗体
                var result = form.ShowDialog(this);
                
                // 对话框关闭后手动释放资源
                form.Dispose();
            }
            catch (ObjectDisposedException)
            {
                // 子窗体已被释放，忽略此异常
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"显示子对话框异常: {ex.Message}");
                // 确保在异常情况下也释放资源
                form?.Dispose();
            }
        }
    }
}

