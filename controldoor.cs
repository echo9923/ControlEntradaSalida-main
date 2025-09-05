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
    public partial class controldoor : Form
    {
        private DataChangeNotifier _notifier;
        private DeviceConnectionInfo _selectedDevice;
        
        public controldoor()
        {
            InitializeComponent();
        }
        

        private void controldoor_Load_1(object sender, EventArgs e)
        {
            // 初始化事件通知器
            _notifier = DataChangeNotifier.Instance;
            
            // 加载设备列表
            LoadDeviceList();
        }
        
        /// <summary>
        /// 加载设备列表到ComboBox
        /// </summary>
        private void LoadDeviceList()
        {
            try
            {
                // 获取所有已连接的设备
                var connectedDevices = DeviceConnectionManager.Instance.GetAllDevices()
                    .Where(d => d.IsConnected).ToList();
                
                if (connectedDevices.Count == 0)
                {
                    MessageBox.Show("没有已连接的设备，请先在设备管理中连接设备", "设备连接提示", 
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    this.Close();
                    return;
                }
                
                // 清空ComboBox
                cmbDevices.Items.Clear();
                cmbDevices.DisplayMember = "DisplayName";
                cmbDevices.ValueMember = "Device";
                
                // 添加设备到ComboBox
                foreach (var device in connectedDevices)
                {
                    var displayItem = new
                    {
                        DisplayName = $"{device.Name} ({device.IpAddress}:{device.Port})",
                        Device = device
                    };
                    cmbDevices.Items.Add(displayItem);
                }
                
                // 默认选择第一个设备
                if (cmbDevices.Items.Count > 0)
                {
                    cmbDevices.SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"加载设备列表时出错: {ex.Message}", "错误", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        
        /// <summary>
        /// 设备选择变更事件处理
        /// </summary>
        private void cmbDevices_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cmbDevices.SelectedItem != null)
            {
                dynamic selectedItem = cmbDevices.SelectedItem;
                _selectedDevice = selectedItem.Device;
                
                // 更新界面状态
                UpdateControlsState();
            }
        }
        
        /// <summary>
        /// 更新控件状态
        /// </summary>
        private void UpdateControlsState()
        {
            bool hasSelectedDevice = _selectedDevice != null && _selectedDevice.IsConnected;
            
            btnOpen.Enabled = hasSelectedDevice;
            btnClose.Enabled = hasSelectedDevice;
            btnStayOpen.Enabled = hasSelectedDevice;
            btnStayClose.Enabled = hasSelectedDevice;
        }

        private void btnOpen_Click(object sender, EventArgs e)
        {
            if (!ValidateSelectedDevice()) return;
            
            try
            {
                if (HCNetSDK.NET_DVR_ControlGateway(_selectedDevice.UserID, 1, 1))
                {
                    MessageBox.Show($"设备 [{_selectedDevice.Name}] 远程开门成功", "操作成功", 
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    
                    // 通知其他界面门控状态已变更（仅在窗体未释放时）
                    if (!this.IsDisposed && !this.Disposing && _notifier != null)
                    {
                        _notifier.NotifyDoorControlStatusChanged(
                            _selectedDevice.Id.ToString(), 
                            DoorStatus.Opened, 
                            $"开门操作 - 设备: {_selectedDevice.Name}",
                            this.GetType().Name);
                    }
                }
                else
                {
                    uint errorCode = HCNetSDK.NET_DVR_GetLastError();
                    MessageBox.Show($"设备 [{_selectedDevice.Name}] 远程开门失败\n错误代码: {errorCode}", 
                        "操作失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"开门操作异常: {ex.Message}", "异常错误", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnClose_Click(object sender, EventArgs e)
        {
            if (!ValidateSelectedDevice()) return;
            
            try
            {
                if (HCNetSDK.NET_DVR_ControlGateway(_selectedDevice.UserID, 1, 0))
                {
                    MessageBox.Show($"设备 [{_selectedDevice.Name}] 远程关门成功", "操作成功", 
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    
                    // 通知其他界面门控状态已变更（仅在窗体未释放时）
                    if (!this.IsDisposed && !this.Disposing && _notifier != null)
                    {
                        _notifier.NotifyDoorControlStatusChanged(
                            _selectedDevice.Id.ToString(), 
                            DoorStatus.Closed, 
                            $"关门操作 - 设备: {_selectedDevice.Name}",
                            this.GetType().Name);
                    }
                }
                else
                {
                    uint errorCode = HCNetSDK.NET_DVR_GetLastError();
                    MessageBox.Show($"设备 [{_selectedDevice.Name}] 远程关门失败\n错误代码: {errorCode}", 
                        "操作失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"关门操作异常: {ex.Message}", "异常错误", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnStayOpen_Click(object sender, EventArgs e)
        {
            if (!ValidateSelectedDevice()) return;
            
            try
            {
                if (HCNetSDK.NET_DVR_ControlGateway(_selectedDevice.UserID, 1, 2))
                {
                    MessageBox.Show($"设备 [{_selectedDevice.Name}] 常开设置成功", "操作成功", 
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    
                    // 通知其他界面门控状态已变更（仅在窗体未释放时）
                    if (!this.IsDisposed && !this.Disposing && _notifier != null)
                    {
                        _notifier.NotifyDoorControlStatusChanged(
                            _selectedDevice.Id.ToString(), 
                            DoorStatus.AlwaysOpen, 
                            $"常开设置 - 设备: {_selectedDevice.Name}",
                            this.GetType().Name);
                    }
                }
                else
                {
                    uint errorCode = HCNetSDK.NET_DVR_GetLastError();
                    MessageBox.Show($"设备 [{_selectedDevice.Name}] 常开设置失败\n错误代码: {errorCode}", 
                        "操作失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"常开设置异常: {ex.Message}", "异常错误", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnStayClose_Click(object sender, EventArgs e)
        {
            if (!ValidateSelectedDevice()) return;
            
            try
            {
                if (HCNetSDK.NET_DVR_ControlGateway(_selectedDevice.UserID, 1, 3))
                {
                    MessageBox.Show($"设备 [{_selectedDevice.Name}] 常闭设置成功", "操作成功", 
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    
                    // 通知其他界面门控状态已变更（仅在窗体未释放时）
                    if (!this.IsDisposed && !this.Disposing && _notifier != null)
                    {
                        _notifier.NotifyDoorControlStatusChanged(
                            _selectedDevice.Id.ToString(), 
                            DoorStatus.AlwaysClosed, 
                            $"常闭设置 - 设备: {_selectedDevice.Name}",
                            this.GetType().Name);
                    }
                }
                else
                {
                    uint errorCode = HCNetSDK.NET_DVR_GetLastError();
                    MessageBox.Show($"设备 [{_selectedDevice.Name}] 常闭设置失败\n错误代码: {errorCode}", 
                        "操作失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"常闭设置异常: {ex.Message}", "异常错误", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        
        /// <summary>
        /// 验证选定的设备是否有效
        /// </summary>
        /// <returns>设备是否有效</returns>
        private bool ValidateSelectedDevice()
        {
            // 检查窗体是否已被释放
            if (this.IsDisposed || this.Disposing)
            {
                return false;
            }
            
            if (_selectedDevice == null)
            {
                MessageBox.Show("请先选择要操作的设备", "设备选择提示", 
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }
            
            if (!_selectedDevice.IsConnected)
            {
                MessageBox.Show($"设备 [{_selectedDevice.Name}] 未连接，请重新连接后再试", "设备连接错误", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                // 刷新设备列表
                LoadDeviceList();
                return false;
            }
            
            return true;
        }
        
        /// <summary>
        /// 刷新设备列表
        /// </summary>
        public void RefreshDeviceList()
        {
            LoadDeviceList();
        }
        
        #region 窗体关闭处理
        
        /// <summary>
        /// 窗体关闭时的清理工作
        /// </summary>
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            try
            {
                // 清理事件通知器引用，防止内存泄漏
                _notifier = null;
                _selectedDevice = null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"窗体关闭清理异常: {ex.Message}");
            }
            
            base.OnFormClosing(e);
        }
        
        /// <summary>
        /// 窗体已关闭后的处理
        /// </summary>
        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            try
            {
                // 确保所有资源都已释放
                if (_notifier != null)
                {
                    _notifier = null;
                }
                
                if (_selectedDevice != null)
                {
                    _selectedDevice = null;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"窗体关闭后清理异常: {ex.Message}");
            }
            
            base.OnFormClosed(e);
        }
        
        #endregion
    }
}

