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
    public partial class PlanTemplateM : Form
    {
        public Int32 m_lUserID = Common.m_UserID;
        public Int32 m_iDeviceIndex = -1;
        public int m_iDeviceType = 0;
        public HCNetSDK.NET_DVR_PLAN_TEMPLATE m_struTemplateCfg = new HCNetSDK.NET_DVR_PLAN_TEMPLATE();
        public HCNetSDK.NET_DVR_PLAN_TEMPLATE_COND m_struTemplateCond = new HCNetSDK.NET_DVR_PLAN_TEMPLATE_COND();

        private int iItemIndex = -1;

        public PlanTemplateM()
        {
            InitializeComponent();
            m_struTemplateCfg.Init();
            m_struTemplateCond.Init();
            cbDeviceType.SelectedIndex = 0;
            UpdateListGroupNoP();
        }

        private void listViewTemplate_ItemSelectionChanged(object sender, ListViewItemSelectionChangedEventArgs e)
        {
            iItemIndex = e.ItemIndex;
            if (iItemIndex < 0)
            {
                return;
            }
            textBoxHGPN.Text = m_struTemplateCfg.dwHolidayGroupNo[iItemIndex].ToString();
        }

        private void btnUpdate_Click(object sender, EventArgs e)
        {
            if (-1 == iItemIndex)
            {
                MessageBox.Show("Please select the list!!!");
                return;
            }
            // limited input data guarantee parse success
            uint.TryParse(textBoxHGPN.Text, out m_struTemplateCfg.dwHolidayGroupNo[iItemIndex]);

            UpdateListGroupNoP();
        }

        private void btnGetPT_Click(object sender, EventArgs e)
        {
            uint dwCommand = 0;
            string[] strCommand = { "NET_DVR_GET_CARD_RIGHT_PLAN_TEMPLATE_V50", "NET_DVR_GET_VERIFY_PLAN_TEMPLATE", "NET_DVR_GET_DOOR_STATUS_PLAN_TEMPLATE" };

            uint dwReturned = 0;
            string strTemp = null;
            uint dwSize = (uint)Marshal.SizeOf(m_struTemplateCfg);
            IntPtr ptrPlanCfg = Marshal.AllocHGlobal((int)dwSize);
            Marshal.StructureToPtr(m_struTemplateCfg, ptrPlanCfg, false);

            switch (cbDeviceType.SelectedIndex)
            {
                case 1:
                case 2:
                    if (cbDeviceType.SelectedIndex == 2)
                    {
                        dwCommand = (uint)HCNetSDK.NET_DVR_GET_DOOR_STATUS_PLAN_TEMPLATE;
                    }
                    else
                    {
                        dwCommand = (uint)HCNetSDK.NET_DVR_GET_VERIFY_PLAN_TEMPLATE;
                    }

                    int templateNumberIndex;
                    int.TryParse(textBoxPTNumber.Text, out templateNumberIndex);

                    if (!HCNetSDK.NET_DVR_GetDVRConfig(m_lUserID, dwCommand, templateNumberIndex, ptrPlanCfg, dwSize, ref dwReturned))
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
                    dwCommand = (uint)HCNetSDK.NET_DVR_GET_CARD_RIGHT_PLAN_TEMPLATE_V50;
                    uint dwConSize = (uint)Marshal.SizeOf(m_struTemplateCond);
                    m_struTemplateCond.dwSize = dwConSize;

                    // limited input data guarantee parse success
                    uint.TryParse(textBoxPTNumber.Text, out m_struTemplateCond.dwPlanTemplateNumber);
                    ushort.TryParse(textBoxLCID.Text, out m_struTemplateCond.wLocalControllerID);

                    IntPtr ptrPlanCon = Marshal.AllocHGlobal((int)dwConSize);
                    Marshal.StructureToPtr(m_struTemplateCond, ptrPlanCon, false);
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

            m_struTemplateCfg = (HCNetSDK.NET_DVR_PLAN_TEMPLATE)Marshal.PtrToStructure(ptrPlanCfg, typeof(HCNetSDK.NET_DVR_PLAN_TEMPLATE));

            if (1 == m_struTemplateCfg.byEnable)
            {
                checkBoxEnableTP.Checked = true;
            }
            else
            {
                checkBoxEnableTP.Checked = false;
            }

            Encoding ec = System.Text.Encoding.GetEncoding("gb2312");
            textBoxPTName.Text = ec.GetString(m_struTemplateCfg.byTemplateName);
            textBoxWPN.Text = m_struTemplateCfg.dwWeekPlanNo.ToString();
            UpdateListGroupNoP();

            Marshal.FreeHGlobal(ptrPlanCfg);
        }

        private void btnSetPT_Click(object sender, EventArgs e)
        {
            uint dwCommand = 0;
            string[] strCommand = { "NET_DVR_SET_CARD_RIGHT_PLAN_TEMPLATE_V50", "NET_DVR_SET_VERIFY_PLAN_TEMPLATE", "NET_DVR_SET_DOOR_STATUS_PLAN_TEMPLATE" };

            uint dwReturned = 0;
            string strTemp = null;

            if (checkBoxEnableTP.Checked)
            {
                m_struTemplateCfg.byEnable = 1;
            }
            else
            {
                m_struTemplateCfg.byEnable = 0;
            }

            for (int i = 0; i < HCNetSDK.TEMPLATE_NAME_LEN; i++)
            {
                m_struTemplateCfg.byTemplateName[i] = 0;
            }

            Encoding ec = System.Text.Encoding.GetEncoding("gb2312");
            byte[] byTempName = ec.GetBytes(textBoxPTName.Text);
            for (int i = 0; i < byTempName.Length; i++)
            {
                if (i >= m_struTemplateCfg.byTemplateName.Length)
                {
                    break;
                }
                m_struTemplateCfg.byTemplateName[i] = byTempName[i];
            }

            int iItemIndex = listViewTemplate.Items.Count;
            for (int i = 0; i < iItemIndex; i++)
            {
                uint.TryParse(listViewTemplate.Items[i].SubItems[1].Text, out m_struTemplateCfg.dwHolidayGroupNo[i]);
            }
            uint.TryParse(textBoxWPN.Text, out m_struTemplateCfg.dwWeekPlanNo);

            uint dwSize = (uint)Marshal.SizeOf(m_struTemplateCfg);
            m_struTemplateCfg.dwSize = dwSize;
            IntPtr ptrPlanCfg = Marshal.AllocHGlobal((int)dwSize);
            Marshal.StructureToPtr(m_struTemplateCfg, ptrPlanCfg, false);

            switch (cbDeviceType.SelectedIndex)
            {
                case 1:
                case 2:
                    if (cbDeviceType.SelectedIndex == 2)
                    {
                        dwCommand = (uint)HCNetSDK.NET_DVR_SET_DOOR_STATUS_PLAN_TEMPLATE;
                    }
                    else
                    {
                        dwCommand = (uint)HCNetSDK.NET_DVR_SET_VERIFY_PLAN_TEMPLATE;
                    }

                    int templateNumberIndex;
                    int.TryParse(textBoxPTNumber.Text, out templateNumberIndex);

                    if (!HCNetSDK.NET_DVR_SetDVRConfig(m_lUserID, dwCommand, templateNumberIndex, ptrPlanCfg, dwSize))
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
                    dwCommand = (uint)HCNetSDK.NET_DVR_SET_CARD_RIGHT_PLAN_TEMPLATE_V50;
                    uint dwConSize = (uint)Marshal.SizeOf(m_struTemplateCond);
                    m_struTemplateCond.dwSize = dwConSize;

                    // limited input data guarantee parse success
                    uint.TryParse(textBoxPTNumber.Text, out m_struTemplateCond.dwPlanTemplateNumber);
                    ushort.TryParse(textBoxLCID.Text, out m_struTemplateCond.wLocalControllerID);

                    IntPtr ptrPlanCon = Marshal.AllocHGlobal((int)dwConSize);
                    Marshal.StructureToPtr(m_struTemplateCond, ptrPlanCon, false);
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

        private void UpdateListGroupNoP()
        {
            listViewTemplate.BeginUpdate();
            listViewTemplate.Items.Clear();
            int iItemNum = m_struTemplateCfg.dwHolidayGroupNo.Length;
            for (int i = 0; i < iItemNum; i++)
            {
                ListViewItem listItem = new ListViewItem();
                listItem.Text = (i + 1).ToString();
                listItem.SubItems.Add(m_struTemplateCfg.dwHolidayGroupNo[i].ToString());
                listViewTemplate.Items.Add(listItem);
            }
            listViewTemplate.EndUpdate();
        }

        private void textBoxPTNumber_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar != '\b')//backspace 
            {
                if ((e.KeyChar < '0') || (e.KeyChar > '9'))//0-9 is permitted  
                {
                    e.Handled = true;
                }
            }
        }

        private void textBoxPTName_KeyPress(object sender, KeyPressEventArgs e)
        {
            //input template name is to long
            if (System.Text.Encoding.UTF8.GetBytes(textBoxPTName.Text).Length > HCNetSDK.TEMPLATE_NAME_LEN)
            {
                // disable input
                if (e.KeyChar != '\b')//backspace 
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

        private void textBoxHGPN_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar != '\b')//backspace 
            {
                if ((e.KeyChar < '0') || (e.KeyChar > '9'))//0-9 is permitted  
                {
                    e.Handled = true;
                }
            }
        }

        private void textBoxWPN_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar != '\b')//backspace 
            {
                if ((e.KeyChar < '0') || (e.KeyChar > '9'))//0-9 is permitted  
                {
                    e.Handled = true;
                }
            }
        }

        private void PlanTemplateM_Load(object sender, EventArgs e)
        {
            
        }
    }
}

