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
        public Int32 m_lUserID = Common.m_UserID;
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
            cbDeviceType.SelectedIndex = 0;
            cbVerifyMode.SelectedIndex = 0;
            cbDoorStateMode.SelectedIndex = 0;
            cbVerifyMode.Hide();
            label7.Hide();
            cbDoorStateMode.Hide();
            label8.Hide();
            UpdateList();
        }

        private void btnGet_Click(object sender, EventArgs e)
        {
            uint dwCommand = 0;
            string[] strCommand = { "NET_DVR_GET_CARD_RIGHT_WEEK_PLAN_V50", "NET_DVR_GET_VERIFY_WEEK_PLAN", "NET_DVR_GET_WEEK_PLAN_CFG" };
            int weekPlanNumberWPIndex = 0;

            uint dwReturned = 0;
            string strTemp = null;
            uint dwSize = (uint)Marshal.SizeOf(m_struPlanCfg);
            m_struPlanCfg.dwSize = dwSize;
            IntPtr ptrPlanCfg = Marshal.AllocHGlobal((int)dwSize);
            Marshal.StructureToPtr(m_struPlanCfg, ptrPlanCfg, false);
            switch (cbDeviceType.SelectedIndex)
            {
                case 0:
                    dwCommand = (uint)HCNetSDK.NET_DVR_GET_CARD_RIGHT_WEEK_PLAN_V50;
                    uint dwConSize = (uint)Marshal.SizeOf(m_struPlanCond);
                    m_struPlanCond.dwSize = dwConSize;
                    uint.TryParse(textBoxWPNumber.Text, out m_struPlanCond.dwWeekPlanNumber);
                    ushort.TryParse(textBoxLCID.Text, out m_struPlanCond.wLocalControllerID);

                    IntPtr ptrPlanCon = Marshal.AllocHGlobal((int)dwConSize);
                    Marshal.StructureToPtr(m_struPlanCond, ptrPlanCon, false);
                    IntPtr ptrDwReturned = Marshal.AllocHGlobal(4);

                    if (!HCNetSDK.NET_DVR_GetDeviceConfig(m_lUserID, dwCommand, 1, ptrPlanCon, dwConSize, ptrDwReturned, ptrPlanCfg, dwSize))
                    {
                        Marshal.FreeHGlobal(ptrPlanCfg);
                        Marshal.FreeHGlobal(ptrPlanCon);
                        Marshal.FreeHGlobal(ptrDwReturned);
                        strTemp = string.Format("{0} FAIL, ERROR CODE {1}", strCommand[cbDeviceType.SelectedIndex], HCNetSDK.NET_DVR_GetLastError());
                        MessageBox.Show(strTemp);
                        return;
                    }
                    else
                    {
                        dwReturned = (uint)Marshal.ReadInt32(ptrDwReturned);
                        Marshal.FreeHGlobal(ptrDwReturned);
                        Marshal.FreeHGlobal(ptrPlanCon);
                        strTemp = string.Format("{0} Succ", strCommand[cbDeviceType.SelectedIndex]);
                        MessageBox.Show(strTemp);
                    }
                    break;
                case 1:
                    dwCommand = (uint)HCNetSDK.NET_DVR_GET_VERIFY_WEEK_PLAN;
                    int.TryParse(textBoxWPNumber.Text, out weekPlanNumberWPIndex);

                    if (!HCNetSDK.NET_DVR_GetDVRConfig(m_lUserID, dwCommand, weekPlanNumberWPIndex, ptrPlanCfg, dwSize, ref dwReturned))
                    {
                        Marshal.FreeHGlobal(ptrPlanCfg);
                        strTemp = string.Format("{0} FAIL, ERROR CODE {1}", strCommand[cbDeviceType.SelectedIndex], HCNetSDK.NET_DVR_GetLastError());
                        MessageBox.Show(strTemp);
                        return;
                    }
                    else
                    {
                        strTemp = string.Format("{0} Succ", strCommand[cbDeviceType.SelectedIndex]);
                        MessageBox.Show(strTemp);
                    }
                    break;
                case 2:
                    dwCommand = (uint)HCNetSDK.NET_DVR_GET_WEEK_PLAN_CFG;
                    int.TryParse(textBoxWPNumber.Text, out weekPlanNumberWPIndex);

                    if (!HCNetSDK.NET_DVR_GetDVRConfig(m_lUserID, dwCommand, weekPlanNumberWPIndex, ptrPlanCfg, dwSize, ref dwReturned))
                    {
                        Marshal.FreeHGlobal(ptrPlanCfg);
                        strTemp = string.Format("{0} FAIL, ERROR CODE {1}", strCommand[cbDeviceType.SelectedIndex], HCNetSDK.NET_DVR_GetLastError());
                        MessageBox.Show(strTemp);
                        return;
                    }
                    else
                    {
                        strTemp = string.Format("{0} Succ", strCommand[cbDeviceType.SelectedIndex]);
                        MessageBox.Show(strTemp);
                    }
                    break;
                default:
                    Marshal.FreeHGlobal(ptrPlanCfg);
                    MessageBox.Show("unknown command");
                    return;
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
            string[] strCommand = { "NET_DVR_SET_CARD_RIGHT_WEEK_PLAN_V50", "NET_DVR_SET_VERIFY_WEEK_PLAN", "NET_DVR_SET_WEEK_PLAN_CFG" };
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


            switch (cbDeviceType.SelectedIndex)
            {
                case 0:
                    dwCommand = (uint)HCNetSDK.NET_DVR_SET_CARD_RIGHT_WEEK_PLAN_V50;
                    uint dwConSize = (uint)Marshal.SizeOf(m_struPlanCond);
                    m_struPlanCond.dwSize = dwConSize;
                    IntPtr ptrPlanCon = Marshal.AllocHGlobal((int)dwConSize);
                    Marshal.StructureToPtr(m_struPlanCond, ptrPlanCon, false);
                    IntPtr ptrDwReturned = Marshal.AllocHGlobal(4);

                    uint.TryParse(textBoxWPNumber.Text, out m_struPlanCond.dwWeekPlanNumber);
                    ushort.TryParse(textBoxLCID.Text, out m_struPlanCond.wLocalControllerID);

                    if (!HCNetSDK.NET_DVR_SetDeviceConfig(m_lUserID, dwCommand, 1, ptrPlanCon, dwConSize, ptrDwReturned, ptrPlanCfg, dwSize))
                    {
                        Marshal.FreeHGlobal(ptrPlanCfg);
                        Marshal.FreeHGlobal(ptrPlanCon);
                        Marshal.FreeHGlobal(ptrDwReturned);
                        strTemp = string.Format("{0} FAIL, ERROR CODE {1}", strCommand[cbDeviceType.SelectedIndex], HCNetSDK.NET_DVR_GetLastError());
                        MessageBox.Show(strTemp);
                        return;
                    }
                    else
                    {
                        dwReturned = (uint)Marshal.ReadInt32(ptrDwReturned);
                        Marshal.FreeHGlobal(ptrDwReturned);
                        Marshal.FreeHGlobal(ptrPlanCon);
                        strTemp = string.Format("{0} Succ", strCommand[cbDeviceType.SelectedIndex]);
                        MessageBox.Show(strTemp);
                    }
                    break;
                case 1:
                    dwCommand = (uint)HCNetSDK.NET_DVR_SET_VERIFY_WEEK_PLAN;
                    int.TryParse(textBoxWPNumber.Text, out weekPlanNumberWPIndex);

                    if (!HCNetSDK.NET_DVR_SetDVRConfig(m_lUserID, dwCommand, weekPlanNumberWPIndex, ptrPlanCfg, dwSize))
                    {
                        Marshal.FreeHGlobal(ptrPlanCfg);
                        strTemp = string.Format("{0} FAIL, ERROR CODE {1}", strCommand[cbDeviceType.SelectedIndex], HCNetSDK.NET_DVR_GetLastError());
                        MessageBox.Show(strTemp);
                        return;
                    }
                    else
                    {
                        strTemp = string.Format("{0} Succ", strCommand[cbDeviceType.SelectedIndex]);
                        MessageBox.Show(strTemp);
                    }

                    break;
                case 2:
                    dwCommand = (uint)HCNetSDK.NET_DVR_SET_WEEK_PLAN_CFG;
                    int.TryParse(textBoxWPNumber.Text, out weekPlanNumberWPIndex);

                    if (!HCNetSDK.NET_DVR_SetDVRConfig(m_lUserID, dwCommand, weekPlanNumberWPIndex, ptrPlanCfg, dwSize))
                    {
                        Marshal.FreeHGlobal(ptrPlanCfg);
                        strTemp = string.Format("{0} FAIL, ERROR CODE {1}", strCommand[cbDeviceType.SelectedIndex], HCNetSDK.NET_DVR_GetLastError());
                        MessageBox.Show(strTemp);
                        return;
                    }
                    else
                    {
                        strTemp = string.Format("{0} Succ", strCommand[cbDeviceType.SelectedIndex]);
                        MessageBox.Show(strTemp);
                    }

                    break;
                default:
                    Marshal.FreeHGlobal(ptrPlanCfg);
                    MessageBox.Show("unknow command");
                    return;
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
                if (struTemp[i].byVerifyMode > 28)
                {
                    strTemp = string.Format("{0}", struTemp[i].byVerifyMode);
                }
                else
                {
                    int iDoorIndex = (int)struTemp[i].byVerifyMode;
                    strTemp = AcsDemoPublic.strVerify[iDoorIndex];
                }
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

            m_struPlanCfg.struPlanCfg[iDate].byVerifyMode = (byte)cbVerifyMode.SelectedIndex;
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
            cbVerifyMode.SelectedIndex = (int)m_struPlanCfg.struPlanCfg[i].byVerifyMode;
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
            switch (cbDeviceType.SelectedIndex)
            {
                case 0:
                    // 卡周计划
                    cbVerifyMode.Hide();
                    label7.Hide();
                    cbDoorStateMode.Hide();
                    label8.Hide();
                    break;
                case 1:
                    //读卡器验证方式周计划
                    cbVerifyMode.Show();
                    label7.Show(); ;
                    cbDoorStateMode.Hide();
                    label8.Hide();
                    break;
                case 2:
                    // 门状态周计划
                    cbVerifyMode.Hide();
                    label7.Hide();
                    cbDoorStateMode.Show();
                    label8.Show();
                    break;
                default:
                    foreach (Control c in this.Controls)
                    {
                        c.Enabled = false;
                    }
                    MessageBox.Show("Wrong Device Type!");
                    break;
            }
        }
    }
}

