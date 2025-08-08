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
    public partial class HolidayPlan : Form
    {
        public Int32 m_lUserID = Common.m_UserID;
        public Int32 m_iDeviceIndex = -1;
        public int m_iDeviceType = 0;

        public HCNetSDK.NET_DVR_HOLIDAY_PLAN_CFG m_struPlanCfgH = new HCNetSDK.NET_DVR_HOLIDAY_PLAN_CFG();
        public HCNetSDK.NET_DVR_HOLIDAY_PLAN_COND m_struPlanCond = new HCNetSDK.NET_DVR_HOLIDAY_PLAN_COND();

        private int iItemIndex = -1;

        public HolidayPlan()
        {
            InitializeComponent();
            m_struPlanCfgH.Init();
            m_struPlanCond.Init();
            cbDeviceType.SelectedIndex = 0;
            cbVerifyMode.SelectedIndex = 0;
            cbDoorStateMode.SelectedIndex = 0;
            cbVerifyMode.Hide();
            label8.Hide();
            cbDoorStateMode.Hide();
            label9.Hide();
            UpdateList();
        }

        private void btnGet_Click(object sender, EventArgs e)
        {
            uint dwCommand = 0;
            string[] strCommand = { "NET_DVR_GET_CARD_RIGHT_HOLIDAY_PLAN_V50", "NET_DVR_GET_VERIFY_HOLIDAY_PLAN", "NET_DVR_GET_DOOR_STATUS_HOLIDAY_PLAN" };

            uint dwReturned = 0;
            string strTemp = null;
            uint dwSize = (uint)Marshal.SizeOf(m_struPlanCfgH);
            m_struPlanCfgH.dwSize = dwSize;
            IntPtr ptrPlanCfgH = Marshal.AllocHGlobal((int)dwSize);
            Marshal.StructureToPtr(m_struPlanCfgH, ptrPlanCfgH, false);

            switch (cbDeviceType.SelectedIndex)
            {
                case 1:
                case 2:
                    if (cbDeviceType.SelectedIndex == 2)
                    {
                        dwCommand = (uint)HCNetSDK.NET_DVR_GET_DOOR_STATUS_HOLIDAY_PLAN;
                    }
                    else
                    {
                        dwCommand = (uint)HCNetSDK.NET_DVR_GET_VERIFY_HOLIDAY_PLAN;
                    }

                    int holidayPlanNumberIndex;
                    int.TryParse(textBoxHPNumber.Text, out holidayPlanNumberIndex);

                    if (!HCNetSDK.NET_DVR_GetDVRConfig(m_lUserID, dwCommand, holidayPlanNumberIndex, ptrPlanCfgH, dwSize, ref dwReturned))
                    {
                        Marshal.FreeHGlobal(ptrPlanCfgH);
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
                case 0:
                    dwCommand = (uint)HCNetSDK.NET_DVR_GET_CARD_RIGHT_HOLIDAY_PLAN_V50;
                    uint dwConSize = (uint)Marshal.SizeOf(m_struPlanCond);
                    m_struPlanCond.dwSize = dwConSize;
                    // limited input data guarantee parse success
                    uint.TryParse(textBoxHPNumber.Text, out m_struPlanCond.dwHolidayPlanNumber);
                    ushort.TryParse(textBoxLCID.Text, out m_struPlanCond.wLocalControllerID);

                    IntPtr ptrPlanCon = Marshal.AllocHGlobal((int)dwConSize);
                    Marshal.StructureToPtr(m_struPlanCond, ptrPlanCon, false);
                    IntPtr ptrDwReturned = Marshal.AllocHGlobal(4);

                    if (!HCNetSDK.NET_DVR_GetDeviceConfig(m_lUserID, dwCommand, 1, ptrPlanCon, dwConSize, ptrDwReturned, ptrPlanCfgH, dwSize))
                    {
                        Marshal.FreeHGlobal(ptrPlanCfgH);
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
                default:
                    Marshal.FreeHGlobal(ptrPlanCfgH);
                    MessageBox.Show("unknown command");
                    return;
            }

            m_struPlanCfgH = (HCNetSDK.NET_DVR_HOLIDAY_PLAN_CFG)Marshal.PtrToStructure(ptrPlanCfgH, typeof(HCNetSDK.NET_DVR_HOLIDAY_PLAN_CFG));

            UpdateList();

            if (1 == m_struPlanCfgH.byEnable)
            {
                checkBoxEnableHP.Checked = true;
            }
            else
            {
                checkBoxEnableHP.Checked = false;
            }

            if (!AcsDemoPublic.CheckState(m_struPlanCfgH.struBeginDate) || !AcsDemoPublic.CheckState(m_struPlanCfgH.struEndDate))
            {
                Marshal.FreeHGlobal(ptrPlanCfgH);
                return;
            }

            // set the date
            dTPStartTime.Value = new System.DateTime(m_struPlanCfgH.struBeginDate.wYear, m_struPlanCfgH.struBeginDate.byMonth, m_struPlanCfgH.struBeginDate.byDay);
            dTPEndTime.Value = new System.DateTime(m_struPlanCfgH.struEndDate.wYear, m_struPlanCfgH.struEndDate.byMonth, m_struPlanCfgH.struEndDate.byDay);

            Marshal.FreeHGlobal(ptrPlanCfgH);
        }

        private void btnSet_Click(object sender, EventArgs e)
        {
            uint dwCommand = 0;
            string[] strCommand = { "NET_DVR_SET_CARD_RIGHT_HOLIDAY_PLAN_V50", "NET_DVR_SET_VERIFY_HOLIDAY_PLAN", "NET_DVR_SET_DOOR_STATUS_HOLIDAY_PLAN" };

            uint dwReturned = 0;
            string strTemp = null;

            if (checkBoxEnableHP.Checked)
            {
                m_struPlanCfgH.byEnable = 1;
            }
            else
            {
                m_struPlanCfgH.byEnable = 0;
            }

            // set the date
            m_struPlanCfgH.struBeginDate.wYear = (ushort)dTPStartTime.Value.Year;
            m_struPlanCfgH.struBeginDate.byMonth = (byte)dTPStartTime.Value.Month;
            m_struPlanCfgH.struBeginDate.byDay = (byte)dTPStartTime.Value.Day;
            m_struPlanCfgH.struEndDate.wYear = (ushort)dTPEndTime.Value.Year;
            m_struPlanCfgH.struEndDate.byMonth = (byte)dTPEndTime.Value.Month;
            m_struPlanCfgH.struEndDate.byDay = (byte)dTPEndTime.Value.Day;

            uint dwSize = (uint)Marshal.SizeOf(m_struPlanCfgH);
            m_struPlanCfgH.dwSize = dwSize;
            IntPtr ptrPlanCfg = Marshal.AllocHGlobal((int)dwSize);
            Marshal.StructureToPtr(m_struPlanCfgH, ptrPlanCfg, false);


            switch (cbDeviceType.SelectedIndex)
            {
                case 1:
                case 2:
                    if (cbDeviceType.SelectedIndex == 2)
                    {
                        dwCommand = (uint)HCNetSDK.NET_DVR_SET_DOOR_STATUS_HOLIDAY_PLAN;
                    }
                    else
                    {
                        dwCommand = (uint)HCNetSDK.NET_DVR_SET_VERIFY_HOLIDAY_PLAN;
                    }

                    int holidayPlanNumberIndex;
                    int.TryParse(textBoxHPNumber.Text, out holidayPlanNumberIndex);

                    if (!HCNetSDK.NET_DVR_SetDVRConfig(m_lUserID, dwCommand, holidayPlanNumberIndex, ptrPlanCfg, dwSize))
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
                case 0:
                    dwCommand = (uint)HCNetSDK.NET_DVR_SET_CARD_RIGHT_HOLIDAY_PLAN_V50;
                    uint dwConSize = (uint)Marshal.SizeOf(m_struPlanCond);
                    m_struPlanCond.dwSize = dwConSize;
                    IntPtr ptrPlanCon = Marshal.AllocHGlobal((int)dwConSize);
                    Marshal.StructureToPtr(m_struPlanCond, ptrPlanCon, false);
                    IntPtr ptrDwReturned = Marshal.AllocHGlobal(4);

                    // limited input data guarantee parse success
                    // limited input data guarantee parse success
                    uint.TryParse(textBoxHPNumber.Text, out m_struPlanCond.dwHolidayPlanNumber);
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
                default:
                    Marshal.FreeHGlobal(ptrPlanCfg);
                    MessageBox.Show("unknown command");
                    return;
            }
            Marshal.FreeHGlobal(ptrPlanCfg);
        }

        private void UpdateList()
        {
            HCNetSDK.NET_DVR_SINGLE_PLAN_SEGMENT[] struTemp = new HCNetSDK.NET_DVR_SINGLE_PLAN_SEGMENT[HCNetSDK.MAX_TIMESEGMENT_V30];
            for (int i = 0; i < HCNetSDK.MAX_TIMESEGMENT_V30; i++)
            {
                struTemp[i] = m_struPlanCfgH.struPlanCfg[i];
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
            if (-1 == iItemIndex)
            {
                MessageBox.Show("Please select the list!!!");
                return;
            }
            if (checkBoxEnableTime.Checked)
            {
                m_struPlanCfgH.struPlanCfg[iItemIndex].byEnable = 1;
            }
            else
            {
                m_struPlanCfgH.struPlanCfg[iItemIndex].byEnable = 0;
            }

            m_struPlanCfgH.struPlanCfg[iItemIndex].byVerifyMode = (byte)cbVerifyMode.SelectedIndex;
            m_struPlanCfgH.struPlanCfg[iItemIndex].byDoorStatus = (byte)cbDoorStateMode.SelectedIndex;
            m_struPlanCfgH.struPlanCfg[iItemIndex].struTimeSegment.struBeginTime.byHour = (byte)dTPStartTime.Value.Hour;
            m_struPlanCfgH.struPlanCfg[iItemIndex].struTimeSegment.struBeginTime.byMinute = (byte)dTPStartTime.Value.Minute;
            m_struPlanCfgH.struPlanCfg[iItemIndex].struTimeSegment.struBeginTime.bySecond = (byte)dTPStartTime.Value.Second;
            if (m_struPlanCfgH.struPlanCfg[iItemIndex].struTimeSegment.struBeginTime.byHour == 23
                && m_struPlanCfgH.struPlanCfg[iItemIndex].struTimeSegment.struBeginTime.byMinute == 59
                && m_struPlanCfgH.struPlanCfg[iItemIndex].struTimeSegment.struBeginTime.bySecond == 59)
            {
                m_struPlanCfgH.struPlanCfg[iItemIndex].struTimeSegment.struBeginTime.byHour = 24;
                m_struPlanCfgH.struPlanCfg[iItemIndex].struTimeSegment.struBeginTime.byMinute = 0;
                m_struPlanCfgH.struPlanCfg[iItemIndex].struTimeSegment.struBeginTime.bySecond = 0;
            }
            m_struPlanCfgH.struPlanCfg[iItemIndex].struTimeSegment.struEndTime.byHour = (byte)dTPEndTime.Value.Hour;
            m_struPlanCfgH.struPlanCfg[iItemIndex].struTimeSegment.struEndTime.byMinute = (byte)dTPEndTime.Value.Minute;
            m_struPlanCfgH.struPlanCfg[iItemIndex].struTimeSegment.struEndTime.bySecond = (byte)dTPEndTime.Value.Second;
            if (m_struPlanCfgH.struPlanCfg[iItemIndex].struTimeSegment.struBeginTime.byHour == 23
                && m_struPlanCfgH.struPlanCfg[iItemIndex].struTimeSegment.struBeginTime.byMinute == 59
                && m_struPlanCfgH.struPlanCfg[iItemIndex].struTimeSegment.struBeginTime.bySecond == 59)
            {
                m_struPlanCfgH.struPlanCfg[iItemIndex].struTimeSegment.struBeginTime.byHour = 24;
                m_struPlanCfgH.struPlanCfg[iItemIndex].struTimeSegment.struBeginTime.byMinute = 0;
                m_struPlanCfgH.struPlanCfg[iItemIndex].struTimeSegment.struBeginTime.bySecond = 0;
            }

            UpdateList();
        }

        private void listViewTimeSegment_ItemSelectionChanged(object sender, ListViewItemSelectionChangedEventArgs e)
        {
            iItemIndex = e.ItemIndex;
            if (iItemIndex < 0)
            {
                return;
            }

            int i = iItemIndex;

            if (1 == m_struPlanCfgH.struPlanCfg[i].byEnable)
            {
                checkBoxEnableTime.Checked = true;
            }
            else
            {
                checkBoxEnableTime.Checked = false;
            }
            cbVerifyMode.SelectedIndex = (int)m_struPlanCfgH.struPlanCfg[i].byVerifyMode;
            cbDoorStateMode.SelectedIndex = (int)m_struPlanCfgH.struPlanCfg[i].byDoorStatus;
            HCNetSDK.NET_DVR_SIMPLE_DAYTIME struTime = new HCNetSDK.NET_DVR_SIMPLE_DAYTIME();
            if (AcsDemoPublic.CheckDate(m_struPlanCfgH.struPlanCfg[i].struTimeSegment.struBeginTime))
            {
                struTime = m_struPlanCfgH.struPlanCfg[i].struTimeSegment.struBeginTime;
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
            if (AcsDemoPublic.CheckDate(m_struPlanCfgH.struPlanCfg[i].struTimeSegment.struEndTime))
            {
                struTime = m_struPlanCfgH.struPlanCfg[i].struTimeSegment.struEndTime;
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

        private void textBoxHPNumber_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar != '\b')//backspace 
            {
                if ((e.KeyChar < '0') || (e.KeyChar > '9'))//0-9 is permitted  
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

        private void HolidayPlan_Load(object sender, EventArgs e)
        {
   
        }

        private void cbDeviceType_SelectedIndexChanged(object sender, EventArgs e)
        {
            switch (cbDeviceType.SelectedIndex)
            {
                case 0:
                    // 卡假日计划
                    cbVerifyMode.Hide();
                    label8.Hide();
                    cbDoorStateMode.Hide();
                    label9.Hide();
                    break;
                case 1:
                    //读卡器验证方式假日计划
                    cbVerifyMode.Show();
                    label8.Show(); ;
                    cbDoorStateMode.Hide();
                    label9.Hide();
                    break;
                case 2:
                    // 门状态假日计划
                    cbVerifyMode.Hide();
                    label8.Hide();
                    cbDoorStateMode.Show();
                    label9.Show();
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

