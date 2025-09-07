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
        
        // 设备列表缓存
        private List<DeviceConnectionInfo> availableDevices = new List<DeviceConnectionInfo>();

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
            
            // 初始化设备列表
            InitializeDeviceComboBoxes();

        }
        
        /// <summary>
        /// 初始化设备下拉框
        /// </summary>
        private void InitializeDeviceComboBoxes()
        {
            try
            {
                // 获取所有可用设备
                availableDevices = DeviceConnectionManager.Instance.GetAllDevices()
                    .Where(d => d.IsEnabled).ToList();
                    
                // 初始化读取设备下拉框（仅已连接设备）
                cbReadDevice.Items.Clear();
                var connectedDevices = availableDevices.Where(d => d.IsConnected).ToList();
                foreach (var device in connectedDevices)
                {
                    cbReadDevice.Items.Add(new DeviceComboBoxItem
                    {
                        Device = device,
                        DisplayText = $"{device.Name} ({device.IpAddress})"
                    });
                }
                
                // 初始化写入设备下拉框（包含“所有设备”选项）
                cbWriteDevice.Items.Clear();
                cbWriteDevice.Items.Add(new DeviceComboBoxItem
                {
                    Device = null,
                    DisplayText = "所有设备"
                });
                foreach (var device in connectedDevices)
                {
                    cbWriteDevice.Items.Add(new DeviceComboBoxItem
                    {
                        Device = device,
                        DisplayText = $"{device.Name} ({device.IpAddress})"
                    });
                }
                
                // 设置默认选中第一个设备
                if (cbReadDevice.Items.Count > 0)
                {
                    cbReadDevice.SelectedIndex = 0;
                    var selectedItem = (DeviceComboBoxItem)cbReadDevice.SelectedItem;
                    m_lUserID = selectedItem.Device?.UserID ?? -1;
                }
                
                if (cbWriteDevice.Items.Count > 0)
                {
                    cbWriteDevice.SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"初始化设备列表时发生错误：{ex.Message}", "错误", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        
        /// <summary>
        /// 设备下拉框项类
        /// </summary>
        private class DeviceComboBoxItem
        {
            public DeviceConnectionInfo Device { get; set; }
            public string DisplayText { get; set; }
            
            public override string ToString()
            {
                return DisplayText;
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
            // 检查是否选中了设备
            if (cbReadDevice.SelectedItem == null)
            {
                MessageBox.Show("请选择要读取配置的设备！", "提示", 
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            
            var selectedItem = (DeviceComboBoxItem)cbReadDevice.SelectedItem;
            var selectedDevice = selectedItem.Device;
            
            if (selectedDevice == null || !selectedDevice.IsConnected)
            {
                MessageBox.Show("选中的设备未连接，无法读取配置！", "错误", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            
            // 使用选中设备的UserID
            m_lUserID = selectedDevice.UserID;
            
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
                uint errorCode = HCNetSDK.NET_DVR_GetLastError();
                strTemp = $"从设备 {selectedDevice.Name} 读取配置失败，错误码：{errorCode}";
                MessageBox.Show(strTemp, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            else
            {
                strTemp = $"从设备 {selectedDevice.Name} 读取配置成功！";
                MessageBox.Show(strTemp, "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
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
            // 检查是否选中了设备
            if (cbWriteDevice.SelectedItem == null)
            {
                MessageBox.Show("请选择要写入配置的设备！", "提示", 
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            
            var selectedItem = (DeviceComboBoxItem)cbWriteDevice.SelectedItem;
            var selectedDevice = selectedItem.Device;
            
            // 判断是否选择了“所有设备”
            bool writeToAllDevices = (selectedDevice == null);
            
            List<DeviceConnectionInfo> targetDevices;
            if (writeToAllDevices)
            {
                // 获取所有已连接的设备
                targetDevices = availableDevices.Where(d => d.IsConnected).ToList();
                if (targetDevices.Count == 0)
                {
                    MessageBox.Show("没有可用的连接设备！", "错误", 
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
            }
            else
            {
                // 仅选中的单个设备
                if (!selectedDevice.IsConnected)
                {
                    MessageBox.Show("选中的设备未连接，无法写入配置！", "错误", 
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                targetDevices = new List<DeviceConnectionInfo> { selectedDevice };
            }
            
            // 确认对话框
            string confirmMessage = writeToAllDevices 
                ? $"确认要将配置写入所有 {targetDevices.Count} 个设备吗？" 
                : $"确认要将配置写入设备 {selectedDevice.Name} 吗？";
                
            if (MessageBox.Show(confirmMessage, "确认操作", 
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
            {
                return;
            }
            
            // 准备配置数据
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

            uint dwCommand = (uint)HCNetSDK.NET_DVR_SET_WEEK_PLAN_CFG;
            int.TryParse(textBoxWPNumber.Text, out int weekPlanNumberWPIndex);
            
            // 对每个设备进行写入操作
            int successCount = 0;
            int failureCount = 0;
            List<string> errorMessages = new List<string>();
            
            foreach (var device in targetDevices)
            {
                try
                {
                    if (!HCNetSDK.NET_DVR_SetDVRConfig(device.UserID, dwCommand, weekPlanNumberWPIndex, ptrPlanCfg, dwSize))
                    {
                        uint errorCode = HCNetSDK.NET_DVR_GetLastError();
                        string errorMsg = $"设备 {device.Name}：错误码 {errorCode}";
                        errorMessages.Add(errorMsg);
                        failureCount++;
                    }
                    else
                    {
                        successCount++;
                    }
                }
                catch (Exception ex)
                {
                    errorMessages.Add($"设备 {device.Name}：{ex.Message}");
                    failureCount++;
                }
            }
            
            Marshal.FreeHGlobal(ptrPlanCfg);
            
            // 显示结果
            string resultMessage = $"写入结果：\n成功: {successCount} 个设备\n失败: {failureCount} 个设备";
            if (errorMessages.Count > 0)
            {
                resultMessage += "\n\n错误详情：\n" + string.Join("\n", errorMessages.Take(5));
                if (errorMessages.Count > 5)
                {
                    resultMessage += $"\n...(还有 {errorMessages.Count - 5} 个错误)";
                }
            }
            
            MessageBoxIcon icon = failureCount == 0 ? MessageBoxIcon.Information : 
                                 (successCount == 0 ? MessageBoxIcon.Error : MessageBoxIcon.Warning);
            MessageBox.Show(resultMessage, "操作结果", MessageBoxButtons.OK, icon);
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
                    strTemp = "是";
                }
                else
                {
                    strTemp = "否";
                }
                listItem.SubItems.Add(strTemp);
                HCNetSDK.NET_DVR_SIMPLE_DAYTIME strTime = struTemp[i].struTimeSegment.struBeginTime;
                strTemp = string.Format("{0:D2}:{1:D2}", strTime.byHour, strTime.byMinute);
                listItem.SubItems.Add(strTemp);
                strTime = struTemp[i].struTimeSegment.struEndTime;
                strTemp = string.Format("{0:D2}:{1:D2}", strTime.byHour, strTime.byMinute);
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
            int iDate = iDateIndex * HCNetSDK.MAX_TIMESEGMENT_V30 + iItemIndex;
            if (iItemIndex == -1)
            {
                MessageBox.Show("请先选中一个时段再进行编辑！");
                return;
            }

            // 时间校验
            var startTime = dTPStartTime.Value;
            var endTime = dTPEndTime.Value;

            if (!TimeSegmentHelper.ValidateTimeSegment(startTime, endTime))
            {
                TimeSegmentHelper.ShowInvalidTimeMessage();
                return;
            }

            // 冲突检查
            HCNetSDK.NET_DVR_SINGLE_PLAN_SEGMENT[] currentDaySegments = new HCNetSDK.NET_DVR_SINGLE_PLAN_SEGMENT[HCNetSDK.MAX_TIMESEGMENT_V30];
            for (int i = 0; i < HCNetSDK.MAX_TIMESEGMENT_V30; i++)
            {
                currentDaySegments[i] = m_struPlanCfg.struPlanCfg[iDateIndex * HCNetSDK.MAX_TIMESEGMENT_V30 + i];
            }

            if (TimeSegmentHelper.CheckTimeConflict(startTime, endTime, currentDaySegments, iItemIndex))
            {
                TimeSegmentHelper.ShowTimeConflictMessage(iItemIndex);
                return;
            }

            // 应用更改
            if (checkBoxEnableTime.Checked)
            {
                m_struPlanCfg.struPlanCfg[iDate].byEnable = 1;
            }
            else
            {
                m_struPlanCfg.struPlanCfg[iDate].byEnable = 0;
            }

            m_struPlanCfg.struPlanCfg[iDate].byDoorStatus = (byte)cbDoorStateMode.SelectedIndex;
            m_struPlanCfg.struPlanCfg[iDate].struTimeSegment.struBeginTime.byHour = (byte)startTime.Hour;
            m_struPlanCfg.struPlanCfg[iDate].struTimeSegment.struBeginTime.byMinute = (byte)startTime.Minute;
            m_struPlanCfg.struPlanCfg[iDate].struTimeSegment.struBeginTime.bySecond = 0; // 固定为0

            // 处理24:00特殊情况
            if (endTime.Hour == 23 && endTime.Minute == 59)
            {
                m_struPlanCfg.struPlanCfg[iDate].struTimeSegment.struEndTime.byHour = 24;
                m_struPlanCfg.struPlanCfg[iDate].struTimeSegment.struEndTime.byMinute = 0;
                m_struPlanCfg.struPlanCfg[iDate].struTimeSegment.struEndTime.bySecond = 0;
            }
            else
            {
                m_struPlanCfg.struPlanCfg[iDate].struTimeSegment.struEndTime.byHour = (byte)endTime.Hour;
                m_struPlanCfg.struPlanCfg[iDate].struTimeSegment.struEndTime.byMinute = (byte)endTime.Minute;
                m_struPlanCfg.struPlanCfg[iDate].struTimeSegment.struEndTime.bySecond = 0; // 固定为0
            }

            UpdateList();
            
            // 显示成功提示
            MessageBox.Show("时段更新成功！", "操作成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
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

        private void WeekPlan_Load(object sender, EventArgs e)
        {
            try
            {
                // 再次刷新，避免窗体构造早于设备加载/连接
                InitializeDeviceComboBoxes();

                // 订阅设备连接状态变化，动态刷新下拉框
                DeviceConnectionManager.Instance.DeviceConnectionStateChanged += OnDeviceConnectionStateChanged;
            }
            catch
            {
                // 忽略加载阶段的非致命异常
            }
        }

        private void OnDeviceConnectionStateChanged(object sender, DeviceConnectionEventArgs e)
        {
            // 跨线程安全刷新
            SafeUIUpdater.UpdateUI(this, () =>
            {
                if (!this.IsDisposed)
                {
                    InitializeDeviceComboBoxes();
                }
            });
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            try
            {
                DeviceConnectionManager.Instance.DeviceConnectionStateChanged -= OnDeviceConnectionStateChanged;
            }
            catch { }
            base.OnFormClosed(e);
        }

    }
}

