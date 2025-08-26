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
        
        public controldoor()
        {
            InitializeComponent();
        }
        

        private void controldoor_Load_1(object sender, EventArgs e)
        {
            // 获取当前连接的设备
            var connectedDevices = DeviceConnectionManager.Instance.GetAllDevices()
                .Where(d => d.IsConnected).ToList();
            if (connectedDevices.Count == 0)
            {
                MessageBox.Show("您必须在设备上登录", "登录错误", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            
            // 初始化事件通知器
            _notifier = DataChangeNotifier.Instance;
        }

        private void btnOpen_Click(object sender, EventArgs e)
        {
            // 获取当前连接的设备
            var connectedDevices = DeviceConnectionManager.Instance.GetAllDevices()
                .Where(d => d.IsConnected).ToList();
            if (connectedDevices.Count > 0)
            {
                int userID = connectedDevices[0].UserID;
                if (HCNetSDK.NET_DVR_ControlGateway(userID, 1, 1))
                {
                    MessageBox.Show("远程开门成功");
                    
                    // 通知其他界面门控状态已变更
                    string deviceId = GetConnectedDeviceId(connectedDevices[0]);
                    _notifier?.NotifyDoorControlStatusChanged(
                        deviceId, 
                        DoorStatus.Opened, 
                        "开门操作",
                        this.GetType().Name);
                }
                else
                {
                    MessageBox.Show("远程开门失败，错误代码:" + HCNetSDK.NET_DVR_GetLastError());
                }
            }
            else
            {
                MessageBox.Show("没有连接的设备", "设备错误", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void btnClose_Click(object sender, EventArgs e)
        {
            // 获取当前连接的设备
            var connectedDevices = DeviceConnectionManager.Instance.GetAllDevices()
                .Where(d => d.IsConnected).ToList();
            if (connectedDevices.Count > 0)
            {
                int userID = connectedDevices[0].UserID;
                if (HCNetSDK.NET_DVR_ControlGateway(userID, 1, 0))
                {
                    MessageBox.Show("远程关门成功");
                    
                    // 通知其他界面门控状态已变更
                    string deviceId = GetConnectedDeviceId(connectedDevices[0]);
                    _notifier?.NotifyDoorControlStatusChanged(
                        deviceId, 
                        DoorStatus.Closed, 
                        "关门操作",
                        this.GetType().Name);
                }
                else
                {
                    MessageBox.Show("远程关门失败，错误代码:" + HCNetSDK.NET_DVR_GetLastError());
                }
            }
            else
            {
                MessageBox.Show("没有连接的设备", "设备错误", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void btnStayOpen_Click(object sender, EventArgs e)
        {
            // 获取当前连接的设备
            var connectedDevices = DeviceConnectionManager.Instance.GetAllDevices()
                .Where(d => d.IsConnected).ToList();
            if (connectedDevices.Count > 0)
            {
                int userID = connectedDevices[0].UserID;
                if (HCNetSDK.NET_DVR_ControlGateway(userID, 1, 2))
                {
                    MessageBox.Show("常开设置成功");
                    
                    // 通知其他界面门控状态已变更
                    string deviceId = GetConnectedDeviceId(connectedDevices[0]);
                    _notifier?.NotifyDoorControlStatusChanged(
                        deviceId, 
                        DoorStatus.AlwaysOpen, 
                        "常开设置",
                        this.GetType().Name);
                }
                else
                {
                    MessageBox.Show("常开设置失败，错误代码:" + HCNetSDK.NET_DVR_GetLastError());
                }
            }
            else
            {
                MessageBox.Show("没有连接的设备", "设备错误", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void btnStayClose_Click(object sender, EventArgs e)
        {
            // 获取当前连接的设备
            var connectedDevices = DeviceConnectionManager.Instance.GetAllDevices()
                .Where(d => d.IsConnected).ToList();
            if (connectedDevices.Count > 0)
            {
                int userID = connectedDevices[0].UserID;
                if (HCNetSDK.NET_DVR_ControlGateway(userID, 1, 3))
                {
                    MessageBox.Show("常闭设置成功");
                    
                    // 通知其他界面门控状态已变更
                    string deviceId = GetConnectedDeviceId(connectedDevices[0]);
                    _notifier?.NotifyDoorControlStatusChanged(
                        deviceId, 
                        DoorStatus.AlwaysClosed, 
                        "常闭设置",
                        this.GetType().Name);
                }
                else
                {
                    MessageBox.Show("常闭设置失败，错误代码:" + HCNetSDK.NET_DVR_GetLastError());
                }
            }
            else
            {
                MessageBox.Show("没有连接的设备", "设备错误", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
        
        /// <summary>
        /// 获取已连接设备的ID
        /// </summary>
        /// <param name="device">设备对象</param>
        /// <returns>设备ID</returns>
        private string GetConnectedDeviceId(object device)
        {
            try
            {
                // 尝试从设备对象中获取ID，这里需要根据实际的设备对象结构调整
                var deviceType = device.GetType();
                var idProperty = deviceType.GetProperty("DeviceId") ?? 
                               deviceType.GetProperty("Id") ?? 
                               deviceType.GetProperty("IP"); // 如果没有ID，使用IP作为标识
                
                if (idProperty != null)
                {
                    return idProperty.GetValue(device)?.ToString() ?? "unknown";
                }
                
                return "unknown";
            }
            catch
            {
                return "unknown";
            }
        }
    }
}

