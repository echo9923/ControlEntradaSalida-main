using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using MySql.Data.MySqlClient;

namespace ControlEntradaSalida
{   //设备管理界面的功能
    public partial class HolidayGroupPlan : Form
    {
        public Int32 m_lUserID = -1;
        public Int32 m_iDeviceIndex = -1;
        public int m_iDeviceType = 0;
        public HCNetSDK.NET_DVR_HOLIDAY_GROUP_CFG m_struGroupCfg = new HCNetSDK.NET_DVR_HOLIDAY_GROUP_CFG();
        public HCNetSDK.NET_DVR_HOLIDAY_GROUP_COND m_struGroupCond = new HCNetSDK.NET_DVR_HOLIDAY_GROUP_COND();
        private int iItemIndex = -1;

        public HolidayGroupPlan()
        {
            InitializeComponent();
            m_struGroupCfg.Init();
            m_struGroupCond.Init();
            
            // 设置窗口置顶显示属性，确保始终位于父窗口之上
            ConfigureWindowDisplay();
            
            // 获取当前连接的设备
            var connectedDevices = DeviceConnectionManager.Instance.GetAllDevices()
                .Where(d => d.IsConnected).ToList();
            if (connectedDevices.Count > 0)
            {
                m_lUserID = connectedDevices[0].UserID;
            }

        }
        
        /// <summary>
        /// 配置窗口显示属性，确保正确的层级关系
        /// </summary>
        private void ConfigureWindowDisplay()
        {
            // 确保窗口能够获得焦点并保持在最前
            this.TopMost = true;
            this.ShowInTaskbar = false;
            this.StartPosition = FormStartPosition.CenterParent;
            
            // 窗口激活时确保置顶
            this.Activated += (sender, e) => 
            {
                this.TopMost = true;
                this.BringToFront();
                this.Focus();
            };
            
            // 窗口显示时确保获得焦点
            this.Shown += (sender, e) =>
            {
                this.Activate();
                this.BringToFront();
            };
        }

        private void btnUpdate_Click(object sender, EventArgs e)
        {
            if (-1 == iItemIndex)
            {
                MessageBox.Show("Please select the list!!!");
                return;
            }
            // limited input data guarantee parse success
            uint.TryParse(textBoxHolidayPlanNo.Text, out m_struGroupCfg.dwHolidayPlanNo[iItemIndex]);

            UpdateListGroupNo();
        }

        private void btnGetTemplate_Click(object sender, EventArgs e)
        {
            // 仅支持门状态模式
            uint dwCommand = (uint)HCNetSDK.NET_DVR_GET_DOOR_STATUS_HOLIDAY_GROUP;
            string strTemp = null;
            uint dwReturned = 0;
            uint dwSize = (uint)Marshal.SizeOf(m_struGroupCfg);
            IntPtr ptrPlanCfg = Marshal.AllocHGlobal((int)dwSize);
            Marshal.StructureToPtr(m_struGroupCfg, ptrPlanCfg, false);

            int holidayGroupNumberHGIndex;
            int.TryParse(textBoxHGNumber.Text, out holidayGroupNumberHGIndex);

            if (!HCNetSDK.NET_DVR_GetDVRConfig(m_lUserID, dwCommand, holidayGroupNumberHGIndex, ptrPlanCfg, dwSize, ref dwReturned))
            {
                Marshal.FreeHGlobal(ptrPlanCfg);
                strTemp = string.Format("{0} 失败, 错误码 {1}", "NET_DVR_GET_DOOR_STATUS_HOLIDAY_GROUP", HCNetSDK.NET_DVR_GetLastError());
                MessageBox.Show(strTemp);
                return;
            }
            else
            {
                strTemp = string.Format("{0} 成功", "NET_DVR_GET_DOOR_STATUS_HOLIDAY_GROUP");
                MessageBox.Show(strTemp);
            }

            m_struGroupCfg = (HCNetSDK.NET_DVR_HOLIDAY_GROUP_CFG)Marshal.PtrToStructure(ptrPlanCfg, typeof(HCNetSDK.NET_DVR_HOLIDAY_GROUP_CFG));

            if (1 == m_struGroupCfg.byEnable)
            {
                checkBoxEnableHG.Checked = true;
            }
            else
            {
                checkBoxEnableHG.Checked = false;
            }

            Encoding ec = System.Text.Encoding.GetEncoding("gb2312");
            textBoxHGName.Text = ec.GetString(m_struGroupCfg.byGroupName);

            UpdateListGroupNo();

            Marshal.FreeHGlobal(ptrPlanCfg);
        }

        private void btnSetTemplate_Click(object sender, EventArgs e)
        {
            // 仅支持门状态模式
            uint dwCommand = (uint)HCNetSDK.NET_DVR_SET_DOOR_STATUS_HOLIDAY_GROUP;
            uint dwReturned = 0;
            string strTemp = null;

            if (checkBoxEnableHG.Checked)
            {
                m_struGroupCfg.byEnable = 1;
            }
            else
            {
                m_struGroupCfg.byEnable = 0;
            }

            for (int i = 0; i < HCNetSDK.HOLIDAY_GROUP_NAME_LEN; i++)
            {
                m_struGroupCfg.byGroupName[i] = 0;
            }
            Encoding ec = System.Text.Encoding.GetEncoding("gb2312");
            byte[] byTempName = ec.GetBytes(textBoxHGName.Text);
            for (int i = 0; i < byTempName.Length; i++)
            {
                if (i >= m_struGroupCfg.byGroupName.Length)
                {
                    break;
                }
                m_struGroupCfg.byGroupName[i] = byTempName[i];
            }

            uint dwSize = (uint)Marshal.SizeOf(m_struGroupCfg);
            m_struGroupCfg.dwSize = dwSize;
            IntPtr ptrPlanCfg = Marshal.AllocHGlobal((int)dwSize);
            Marshal.StructureToPtr(m_struGroupCfg, ptrPlanCfg, false);


            int holidayGroupNumberHGIndex;
            int.TryParse(textBoxHGNumber.Text, out holidayGroupNumberHGIndex);

            if (!HCNetSDK.NET_DVR_SetDVRConfig(m_lUserID, dwCommand, holidayGroupNumberHGIndex, ptrPlanCfg, dwSize))
            {
                Marshal.FreeHGlobal(ptrPlanCfg);
                strTemp = string.Format("{0} 失败, 错误码 {1}", "NET_DVR_SET_DOOR_STATUS_HOLIDAY_GROUP", HCNetSDK.NET_DVR_GetLastError());
                MessageBox.Show(strTemp);
                return;
            }
            else
            {
                strTemp = string.Format("{0} 成功", "NET_DVR_SET_DOOR_STATUS_HOLIDAY_GROUP");
                MessageBox.Show(strTemp);
            }
            
            Marshal.FreeHGlobal(ptrPlanCfg);
        }

        private void listViewHG_ItemSelectionChanged(object sender, ListViewItemSelectionChangedEventArgs e)
        {
            iItemIndex = e.ItemIndex;
            if (iItemIndex < 0)
            {
                return;
            }
            textBoxHolidayPlanNo.Text = m_struGroupCfg.dwHolidayPlanNo[iItemIndex].ToString();
        }

        private void textBoxHGName_KeyPress(object sender, KeyPressEventArgs e)
        {
            //input group name is to long
            if (System.Text.Encoding.UTF8.GetBytes(textBoxHGName.Text).Length > HCNetSDK.HOLIDAY_GROUP_NAME_LEN)
            {
                // disable input
                if (e.KeyChar != '\b')//backspace 
                {
                    e.Handled = true;
                }
            }
        }

        private void textBoxHGNumber_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar != '\b')//backspace 
            {
                if ((e.KeyChar < '0') || (e.KeyChar > '9'))//0-9 is permitted  
                {
                    e.Handled = true;
                }
            }
        }

        private void textBoxLocalControllerID_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar != '\b')//backspace 
            {
                if ((e.KeyChar < '0') || (e.KeyChar > '9'))//0-9 is permitted  
                {
                    e.Handled = true;
                }
            }
        }

        private void textBoxHolidayPlanNo_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar != '\b')//backspace 
            {
                if ((e.KeyChar < '0') || (e.KeyChar > '9'))//0-9 is permitted  
                {
                    e.Handled = true;
                }
            }
        }

        private void UpdateListGroupNo()
        {
            listViewHG.BeginUpdate();
            listViewHG.Items.Clear();
            int iItemNum = m_struGroupCfg.dwHolidayPlanNo.Length;
            for (int i = 0; i < iItemNum; i++)
            {
                ListViewItem listItem = new ListViewItem();
                listItem.Text = (i + 1).ToString();
                listItem.SubItems.Add(m_struGroupCfg.dwHolidayPlanNo[i].ToString());
                //listItem.SubItems.Add(textBoxHGName.Text);
                listViewHG.Items.Add(listItem);
            }
            listViewHG.EndUpdate();
        }

        private void HolidayGroupPlan_Load(object sender, EventArgs e)
        {
        }
    }
}

