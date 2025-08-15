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
    }
}

