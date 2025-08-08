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
        public Int32 m_lUserID = Common.m_UserID;
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
            cbDeviceType.SelectedIndex = 0;
            UpdateListGroupNo();
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
            uint dwCommand = 0;
            string[] strCommand = { "NET_DVR_GET_CARD_RIGHT_HOLIDAY_GROUP_V50", "NET_DVR_GET_VERIFY_HOLIDAY_GROUP", "NET_DVR_GET_DOOR_STATUS_HOLIDAY_GROUP" };

            uint dwReturned = 0;
            string strTemp = null;
            uint dwSize = (uint)Marshal.SizeOf(m_struGroupCfg);
            IntPtr ptrPlanCfg = Marshal.AllocHGlobal((int)dwSize);
            Marshal.StructureToPtr(m_struGroupCfg, ptrPlanCfg, false);

            switch (cbDeviceType.SelectedIndex)
            {
                case 1:
                case 2:
                    if (cbDeviceType.SelectedIndex == 2)
                    {
                        dwCommand = (uint)HCNetSDK.NET_DVR_GET_DOOR_STATUS_HOLIDAY_GROUP;
                    }
                    else
                    {
                        dwCommand = (uint)HCNetSDK.NET_DVR_GET_VERIFY_HOLIDAY_GROUP;
                    }

                    int holidayGroupNumberHGIndex;
                    int.TryParse(textBoxHGNumber.Text, out holidayGroupNumberHGIndex);

                    if (!HCNetSDK.NET_DVR_GetDVRConfig(m_lUserID, dwCommand, holidayGroupNumberHGIndex, ptrPlanCfg, dwSize, ref dwReturned))
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
                    dwCommand = (uint)HCNetSDK.NET_DVR_GET_CARD_RIGHT_HOLIDAY_GROUP_V50;
                    uint dwConSize = (uint)Marshal.SizeOf(m_struGroupCond);
                    m_struGroupCond.dwSize = dwConSize;
                    // limited input data guarantee parse success
                    uint.TryParse(textBoxHGNumber.Text, out m_struGroupCond.dwHolidayGroupNumber);
                    ushort.TryParse(textBoxLocalControllerID.Text, out m_struGroupCond.wLocalControllerID);

                    IntPtr ptrPlanCon = Marshal.AllocHGlobal((int)dwConSize);
                    Marshal.StructureToPtr(m_struGroupCond, ptrPlanCon, false);
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
                default:
                    Marshal.FreeHGlobal(ptrPlanCfg);
                    MessageBox.Show("unknown command");
                    return;
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
            //textBoxHolidayGroupNameHG.Text
            //var byteToString4 = System.Text.Encoding.ASCII.GetString(m_struGroupCfg.byGroupName);
            textBoxHGName.Text = ec.GetString(m_struGroupCfg.byGroupName);
            // Console.WriteLine(byteToString4);

            UpdateListGroupNo();

            Marshal.FreeHGlobal(ptrPlanCfg);
        }

        private void btnSetTemplate_Click(object sender, EventArgs e)
        {
            uint dwCommand = 0;
            string[] strCommand = { "NET_DVR_SET_CARD_RIGHT_HOLIDAY_GROUP_V50", "NET_DVR_SET_VERIFY_HOLIDAY_GROUP", "NET_DVR_SET_DOOR_STATUS_HOLIDAY_GROUP" };

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
            //m_struGroupCfg.byGroupName = System.Text.Encoding.UTF8.GetBytes(textBoxHolidayGroupNameHG.Text);
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

            switch (cbDeviceType.SelectedIndex)
            {
                case 1:
                case 2:
                    if (cbDeviceType.SelectedIndex == 2)
                    {
                        dwCommand = (uint)HCNetSDK.NET_DVR_SET_DOOR_STATUS_HOLIDAY_GROUP;
                    }
                    else
                    {
                        dwCommand = (uint)HCNetSDK.NET_DVR_SET_VERIFY_HOLIDAY_GROUP;
                    }

                    int holidayGroupNumberHGIndex;
                    int.TryParse(textBoxHGNumber.Text, out holidayGroupNumberHGIndex);

                    if (!HCNetSDK.NET_DVR_SetDVRConfig(m_lUserID, dwCommand, holidayGroupNumberHGIndex, ptrPlanCfg, dwSize))
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
                    dwCommand = (uint)HCNetSDK.NET_DVR_SET_CARD_RIGHT_HOLIDAY_GROUP_V50;
                    uint dwConSize = (uint)Marshal.SizeOf(m_struGroupCond);
                    m_struGroupCond.dwSize = dwConSize;

                    // limited input data guarantee parse success
                    uint.TryParse(textBoxHGNumber.Text, out m_struGroupCond.dwHolidayGroupNumber);
                    ushort.TryParse(textBoxLocalControllerID.Text, out m_struGroupCond.wLocalControllerID);

                    IntPtr ptrPlanCon = Marshal.AllocHGlobal((int)dwConSize);
                    Marshal.StructureToPtr(m_struGroupCond, ptrPlanCon, false);
                    IntPtr ptrDwReturned = Marshal.AllocHGlobal(4);

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

