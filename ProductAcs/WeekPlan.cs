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
    public partial class WeekPlan : Form
    {
        public Int32 m_lUserID = -1;
        public Int32 m_iDeviceIndex = -1;
        public int m_iDeviceType = 0;

        public HCNetSDK.NET_DVR_WEEK_PLAN_CFG m_struPlanCfg = new HCNetSDK.NET_DVR_WEEK_PLAN_CFG();
        public HCNetSDK.NET_DVR_WEEK_PLAN_COND m_struPlanCond = new HCNetSDK.NET_DVR_WEEK_PLAN_COND();

        private int iItemIndex = -1;

        public WeekPlan()
        {
            InitializeComponent();
            m_struPlanCfg.Init();
            m_struPlanCond.Init();
            
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

        private void btnGet_Click(object sender, EventArgs e)
        {
            uint dwCommand = 0;
            int weekPlanNumberWPIndex = 0;

            uint dwReturned = 0;
            string strTemp = null;
            uint dwSize = (uint)Marshal.SizeOf(m_struPlanCfg);
            m_struPlanCfg.dwSize = dwSize;
            IntPtr ptrPlanCfg = Marshal.AllocHGlobal((int)dwSize);
            Marshal.StructureToPtr(m_struPlanCfg, ptrPlanCfg, false);
            
            // 仅支持门状态模式
            dwCommand = (uint)HCNetSDK.NET_DVR_GET_WEEK_PLAN_CFG;
            int.TryParse(textBoxWPNumber.Text, out weekPlanNumberWPIndex);

            if (!HCNetSDK.NET_DVR_GetDVRConfig(m_lUserID, dwCommand, weekPlanNumberWPIndex, ptrPlanCfg, dwSize, ref dwReturned))
            {
                Marshal.FreeHGlobal(ptrPlanCfg);
                strTemp = string.Format("{0} 失败, 错误码 {1}", "NET_DVR_GET_WEEK_PLAN_CFG", HCNetSDK.NET_DVR_GetLastError());
                MessageBox.Show(strTemp);
                return;
            }
            else
            {
                strTemp = string.Format("{0} 成功", "NET_DVR_GET_WEEK_PLAN_CFG");
                MessageBox.Show(strTemp);
            }
            
            m_struPlanCfg = (HCNetSDK.NET_DVR_WEEK_PLAN_CFG)Marshal.PtrToStructure(ptrPlanCfg, typeof(HCNetSDK.NET_DVR_WEEK_PLAN_CFG));
            cbDate.SelectedIndex = 0;
            UpdateList();

            if (1 == m_struPlanCfg.byEnable)
            {
                checkBoxEnableWP.Checked = true;
            }
            else
            {
                checkBoxEnableWP.Checked = false;
            }

            Marshal.FreeHGlobal(ptrPlanCfg);
        }

        private void btnSet_Click(object sender, EventArgs e)
        {
            uint dwCommand = 0;
            int weekPlanNumberWPIndex = 0;
            uint dwReturned = 0;
            string strTemp = null;

            if (checkBoxEnableWP.Checked)
            {
                m_struPlanCfg.byEnable = 1;
            }
            else
            {
                m_struPlanCfg.byEnable = 0;
            }

            uint dwSize = (uint)Marshal.SizeOf(m_struPlanCfg);
            m_struPlanCfg.dwSize = dwSize;
            IntPtr ptrPlanCfg = Marshal.AllocHGlobal((int)dwSize);
            Marshal.StructureToPtr(m_struPlanCfg, ptrPlanCfg, false);

            // 仅支持门状态模式
            dwCommand = (uint)HCNetSDK.NET_DVR_SET_WEEK_PLAN_CFG;
            int.TryParse(textBoxWPNumber.Text, out weekPlanNumberWPIndex);

            if (!HCNetSDK.NET_DVR_SetDVRConfig(m_lUserID, dwCommand, weekPlanNumberWPIndex, ptrPlanCfg, dwSize))
            {
                Marshal.FreeHGlobal(ptrPlanCfg);
                strTemp = string.Format("{0} 失败, 错误码 {1}", "NET_DVR_SET_WEEK_PLAN_CFG", HCNetSDK.NET_DVR_GetLastError());
                MessageBox.Show(strTemp);
                return;
            }
            else
            {
                strTemp = string.Format("{0} 成功", "NET_DVR_SET_WEEK_PLAN_CFG");
                MessageBox.Show(strTemp);
            }
            
            Marshal.FreeHGlobal(ptrPlanCfg);
        }

        private void UpdateList()
        {
            int iDate = cbDate.SelectedIndex;

            HCNetSDK.NET_DVR_SINGLE_PLAN_SEGMENT[] struTemp = new HCNetSDK.NET_DVR_SINGLE_PLAN_SEGMENT[HCNetSDK.MAX_TIMESEGMENT_V30];
            for (int i = 0; i < HCNetSDK.MAX_TIMESEGMENT_V30; i++)
            {
                struTemp[i] = m_struPlanCfg.struPlanCfg[iDate * HCNetSDK.MAX_TIMESEGMENT_V30 + i];
            }

            listViewTimeSegment.BeginUpdate();
            listViewTimeSegment.Items.Clear();
            string strTemp = null;
            for (int i = 0; i < HCNetSDK.MAX_TIMESEGMENT_V30; i++)
            {
                ListViewItem listItem = new ListViewItem();
                strTemp = string.Format("{0}", i + 1);
                listItem.Text = strTemp;
                if (1 == struTemp[i].byEnable)
                {
                    strTemp = "yes";
                }
                else
                {
                    strTemp = "no";
                }
                listItem.SubItems.Add(strTemp);
                HCNetSDK.NET_DVR_SIMPLE_DAYTIME strTime = struTemp[i].struTimeSegment.struBeginTime;
                strTemp = string.Format("{0,2}:{1,2}:{2,2}", strTime.byHour, strTime.byMinute, strTime.bySecond);
                listItem.SubItems.Add(strTemp);
                strTime = struTemp[i].struTimeSegment.struEndTime;
                strTemp = string.Format("{0,2}:{1,2}:{2,2}", strTime.byHour, strTime.byMinute, strTime.bySecond);
                listItem.SubItems.Add(strTemp);
                // 仅支持门状态模式，不显示验证模式
                strTemp = "-";
                listItem.SubItems.Add(strTemp);
                if (struTemp[i].byDoorStatus > 5)
                {
                    strTemp = string.Format("{0}", struTemp[i].byDoorStatus);
                }
                else
                {
                    int iDoorIndex = (int)struTemp[i].byDoorStatus;
                    strTemp = AcsDemoPublic.strDoorStatus[iDoorIndex];
                }
                listItem.SubItems.Add(strTemp);
                listViewTimeSegment.Items.Add(listItem);
            }

            listViewTimeSegment.EndUpdate();
        }

        private void btnAdd_Click(object sender, EventArgs e)
        {
            int iDateIndex = cbDate.SelectedIndex;
            int iDate = iDateIndex *HCNetSDK.MAX_TIMESEGMENT_V30 + iItemIndex;
            if (-1 == iDate)
            {
                MessageBox.Show("Please select the list!!!");
                return;
            }
            if (checkBoxEnableTime.Checked)
            {
                m_struPlanCfg.struPlanCfg[iDate].byEnable = 1;
            }
            else
            {
                m_struPlanCfg.struPlanCfg[iDate].byEnable = 0;
            }

            m_struPlanCfg.struPlanCfg[iDate].byDoorStatus = (byte)cbDoorStateMode.SelectedIndex;
            m_struPlanCfg.struPlanCfg[iDate].struTimeSegment.struBeginTime.byHour = (byte)dTPStartTime.Value.Hour;
            m_struPlanCfg.struPlanCfg[iDate].struTimeSegment.struBeginTime.byMinute = (byte)dTPStartTime.Value.Minute;
            m_struPlanCfg.struPlanCfg[iDate].struTimeSegment.struBeginTime.bySecond = (byte)dTPStartTime.Value.Second;
            if (m_struPlanCfg.struPlanCfg[iDate].struTimeSegment.struBeginTime.byHour == 23
                && m_struPlanCfg.struPlanCfg[iDate].struTimeSegment.struBeginTime.byMinute == 59
                && m_struPlanCfg.struPlanCfg[iDate].struTimeSegment.struBeginTime.bySecond == 59)
            {
                m_struPlanCfg.struPlanCfg[iDate].struTimeSegment.struBeginTime.byHour = 24;
                m_struPlanCfg.struPlanCfg[iDate].struTimeSegment.struBeginTime.byMinute = 0;
                m_struPlanCfg.struPlanCfg[iDate].struTimeSegment.struBeginTime.bySecond = 0;
            }
            m_struPlanCfg.struPlanCfg[iDate].struTimeSegment.struEndTime.byHour = (byte)dTPEndTime.Value.Hour;
            m_struPlanCfg.struPlanCfg[iDate].struTimeSegment.struEndTime.byMinute = (byte)dTPEndTime.Value.Minute;
            m_struPlanCfg.struPlanCfg[iDate].struTimeSegment.struEndTime.bySecond = (byte)dTPEndTime.Value.Second;
            if (m_struPlanCfg.struPlanCfg[iDate].struTimeSegment.struBeginTime.byHour == 23
                && m_struPlanCfg.struPlanCfg[iDate].struTimeSegment.struBeginTime.byMinute == 59
                && m_struPlanCfg.struPlanCfg[iDate].struTimeSegment.struBeginTime.bySecond == 59)
            {
                m_struPlanCfg.struPlanCfg[iDate].struTimeSegment.struBeginTime.byHour = 24;
                m_struPlanCfg.struPlanCfg[iDate].struTimeSegment.struBeginTime.byMinute = 0;
                m_struPlanCfg.struPlanCfg[iDate].struTimeSegment.struBeginTime.bySecond = 0;
            }

            UpdateList();
        }

        private void cbDate_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateList();
        }

        private void listViewTimeSegment_ItemSelectionChanged(object sender, ListViewItemSelectionChangedEventArgs e)
        {
            iItemIndex = e.ItemIndex;
            if (iItemIndex < 0)
            {
                return;
            }
            int iDate = cbDate.SelectedIndex;
            int i = iDate * HCNetSDK.MAX_TIMESEGMENT_V30 + iItemIndex;

            if (1 == m_struPlanCfg.struPlanCfg[i].byEnable)
            {
                checkBoxEnableTime.Checked = true;
            }
            else
            {
                checkBoxEnableTime.Checked = false;
            }
            cbDoorStateMode.SelectedIndex = (int)m_struPlanCfg.struPlanCfg[i].byDoorStatus;
            HCNetSDK.NET_DVR_SIMPLE_DAYTIME struTime = new HCNetSDK.NET_DVR_SIMPLE_DAYTIME();
            if (AcsDemoPublic.CheckDate(m_struPlanCfg.struPlanCfg[i].struTimeSegment.struBeginTime))
            {
                struTime = m_struPlanCfg.struPlanCfg[i].struTimeSegment.struBeginTime;
                if (struTime.byHour == 24 && struTime.byMinute == 0 && struTime.bySecond == 0)
                {
                    struTime.byHour = 23;
                    struTime.byMinute = 59;
                    struTime.bySecond = 59;
                }
                dTPStartTime.Value = new System.DateTime(dTPStartTime.Value.Year,
                    dTPStartTime.Value.Month, dTPStartTime.Value.Day, struTime.byHour,
                    struTime.byMinute, struTime.bySecond);
            }
            if (AcsDemoPublic.CheckDate(m_struPlanCfg.struPlanCfg[i].struTimeSegment.struEndTime))
            {
                struTime = m_struPlanCfg.struPlanCfg[i].struTimeSegment.struEndTime;
                if (struTime.byHour == 24 && struTime.byMinute == 0 && struTime.bySecond == 0)
                {
                    struTime.byHour = 23;
                    struTime.byMinute = 59;
                    struTime.bySecond = 59;
                }
                dTPEndTime.Value = new System.DateTime(dTPEndTime.Value.Year,
                    dTPEndTime.Value.Month, dTPEndTime.Value.Day, struTime.byHour,
                    struTime.byMinute, struTime.bySecond);
            }
        }

        private void textBoxWPNumber_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar != '\b')//backspace 
            {
                if ((e.KeyChar < '0') || (e.KeyChar > '9'))//1-9 is permitted  
                {
                    e.Handled = true;
                }
            }
        }

        private void textBoxLCID_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar != '\b')//backspace 
            {
                if ((e.KeyChar < '0') || (e.KeyChar > '9'))//0-9 is permitted  
                {
                    e.Handled = true;
                }
            }
        }

        private void WeekPlan_Load(object sender, EventArgs e)
        {
        }

        private void cbDeviceType_SelectedIndexChanged(object sender, EventArgs e)
        {
            // 仅支持门状态周计划
            cbDoorStateMode.Show();
            label8.Show();
        }
    }
}

