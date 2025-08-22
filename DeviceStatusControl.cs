using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ControlEntradaSalida
{
    public partial class DeviceStatusControl : UserControl
    {
        private DeviceConnectionInfo _device;
        private Timer _refreshTimer;
        private bool _isDoorOpen = false;
        private bool _isDoorLocked = true;

        public DeviceConnectionInfo Device
        {
            get => _device;
            set
            {
                _device = value;
                UpdateDisplay();
            }
        }

        public DeviceStatusControl()
        {
            InitializeComponent();
            InitializeTimer();
            SetupEventHandlers();
        }

        private void InitializeComponent()
        {
            this.pnlMain = new System.Windows.Forms.Panel();
            this.lblDeviceName = new System.Windows.Forms.Label();
            this.lblDeviceIP = new System.Windows.Forms.Label();
            this.lblStatus = new System.Windows.Forms.Label();
            this.lblDoorStatus = new System.Windows.Forms.Label();
            this.pbStatusIcon = new System.Windows.Forms.PictureBox();
            this.pbDoorIcon = new System.Windows.Forms.PictureBox();
            
            // pnlMain
            this.pnlMain.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.pnlMain.BackColor = System.Drawing.Color.White;
            this.pnlMain.Size = new System.Drawing.Size(180, 120);
            this.pnlMain.Margin = new System.Windows.Forms.Padding(5);
            
            // lblDeviceName
            this.lblDeviceName.AutoSize = true;
            this.lblDeviceName.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F, System.Drawing.FontStyle.Bold);
            this.lblDeviceName.Location = new System.Drawing.Point(10, 10);
            this.lblDeviceName.Size = new System.Drawing.Size(65, 15);
            this.lblDeviceName.Text = "设备名称";
            
            // lblDeviceIP
            this.lblDeviceIP.AutoSize = true;
            this.lblDeviceIP.Font = new System.Drawing.Font("Microsoft YaHei UI", 8F);
            this.lblDeviceIP.Location = new System.Drawing.Point(10, 30);
            this.lblDeviceIP.Size = new System.Drawing.Size(50, 13);
            this.lblDeviceIP.Text = "IP地址";
            
            // pbStatusIcon
            this.pbStatusIcon.Size = new System.Drawing.Size(16, 16);
            this.pbStatusIcon.Location = new System.Drawing.Point(10, 50);
            this.pbStatusIcon.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            
            // lblStatus
            this.lblStatus.AutoSize = true;
            this.lblStatus.Font = new System.Drawing.Font("Microsoft YaHei UI", 8F);
            this.lblStatus.Location = new System.Drawing.Point(35, 52);
            this.lblStatus.Size = new System.Drawing.Size(50, 13);
            this.lblStatus.Text = "状态";
            
            // pbDoorIcon
            this.pbDoorIcon.Size = new System.Drawing.Size(16, 16);
            this.pbDoorIcon.Location = new System.Drawing.Point(10, 75);
            this.pbDoorIcon.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            
            // lblDoorStatus
            this.lblDoorStatus.AutoSize = true;
            this.lblDoorStatus.Font = new System.Drawing.Font("Microsoft YaHei UI", 8F);
            this.lblDoorStatus.Location = new System.Drawing.Point(35, 77);
            this.lblDoorStatus.Size = new System.Drawing.Size(50, 13);
            this.lblDoorStatus.Text = "门状态";
            
            // 添加控件到面板
            this.pnlMain.Controls.Add(this.lblDeviceName);
            this.pnlMain.Controls.Add(this.lblDeviceIP);
            this.pnlMain.Controls.Add(this.pbStatusIcon);
            this.pnlMain.Controls.Add(this.lblStatus);
            this.pnlMain.Controls.Add(this.pbDoorIcon);
            this.pnlMain.Controls.Add(this.lblDoorStatus);
            
            // 设置用户控件属性
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.pnlMain);
            this.Size = new System.Drawing.Size(180, 120);
            this.Margin = new System.Windows.Forms.Padding(5);
        }

        private void InitializeTimer()
        {
            _refreshTimer = new Timer();
            _refreshTimer.Interval = 5000; // 5秒刷新一次门状态
            _refreshTimer.Tick += RefreshTimer_Tick;
            _refreshTimer.Start();
        }

        private void SetupEventHandlers()
        {
            // 鼠标悬停效果
            this.pnlMain.MouseEnter += (sender, e) =>
            {
                if (_device.IsConnected)
                {
                    pnlMain.BackColor = Color.LightGreen;
                }
                else
                {
                    pnlMain.BackColor = Color.LightGray;
                }
            };

            this.pnlMain.MouseLeave += (sender, e) =>
            {
                if (_device.IsConnected)
                {
                    pnlMain.BackColor = Color.White;
                }
                else
                {
                    pnlMain.BackColor = Color.LightGray;
                }
            };

            // 右键菜单
            this.pnlMain.MouseDown += (sender, e) =>
            {
                if (e.Button == MouseButtons.Right)
                {
                    ShowContextMenu(e.Location);
                }
            };

            // 双击事件 - 打开远程控制窗口
            this.pnlMain.DoubleClick += (sender, e) =>
            {
                OpenRemoteControl();
            };
        }

        private void ShowContextMenu(Point location)
        {
            var contextMenu = new ContextMenuStrip();
            
            var refreshItem = new ToolStripMenuItem("刷新状态", null, (s, e) => RefreshDeviceStatus());
            var remoteControlItem = new ToolStripMenuItem("远程控制", null, (s, e) => OpenRemoteControl());
            var propertiesItem = new ToolStripMenuItem("设备属性", null, (s, e) => ShowDeviceProperties());
            
            contextMenu.Items.AddRange(new ToolStripItem[] { refreshItem, remoteControlItem, propertiesItem });
            
            contextMenu.Show(this.pnlMain, location);
        }

        private void RefreshDeviceStatus()
        {
            if (_device != null)
            {
                DeviceConnectionManager.Instance.CheckDeviceStatus(_device);
                UpdateDisplay();
            }
        }

        private void OpenRemoteControl()
        {
            if (_device != null && _device.IsConnected)
            {
                try
                {
                    var controldoorForm = new controldoor();
                    controldoorForm.MdiParent = this.FindForm();
                    controldoorForm.Show();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"打开远程控制失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            else
            {
                MessageBox.Show("设备离线，无法进行远程控制", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void ShowDeviceProperties()
        {
            if (_device != null)
            {
                var message = $"设备名称: {_device.Name}\n" +
                             $"IP地址: {_device.IpAddress}\n" +
                             $"端口: {_device.Port}\n" +
                             $"连接状态: {(_device.IsConnected ? "在线" : "离线")}\n" +
                             $"最后检查时间: {_device.LastChecked:yyyy-MM-dd HH:mm:ss}\n" +
                             $"状态信息: {_device.StatusMessage}";
                
                MessageBox.Show(message, "设备属性", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private new Form FindForm()
        {
            Control parent = this.Parent;
            while (parent != null && !(parent is Form))
            {
                parent = parent.Parent;
            }
            return parent as Form;
        }

        private void RefreshTimer_Tick(object sender, EventArgs e)
        {
            if (_device != null && _device.IsConnected)
            {
                CheckDoorStatus();
            }
        }

        private void CheckDoorStatus()
        {
            if (_device == null || !_device.IsConnected || _device.UserID < 0)
                return;

            try
            {
                // 获取门状态（这里需要根据实际SDK调用调整）
                // 这是一个示例实现，您可能需要根据实际的海康威视SDK进行调整
                var status = GetDoorStatusFromDevice(_device.UserID);
                _isDoorOpen = status.IsOpen;
                _isDoorLocked = status.IsLocked;
                
                this.Invoke((MethodInvoker)delegate {
                    UpdateDoorStatusDisplay();
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"检查门状态时出错: {ex.Message}");
            }
        }

        private (bool IsOpen, bool IsLocked) GetDoorStatusFromDevice(int userId)
        {
            // 这里应该调用海康威视SDK获取实际门状态
            // 由于SDK限制，这里返回模拟状态
            // 实际实现应该调用类似 HCNetSDK.NET_DVR_GetDVRConfig 的方法
            
            // 模拟实现 - 返回随机状态用于测试
            return (false, true);
        }

        public void UpdateDisplay()
        {
            if (_device == null) return;

            this.Invoke((MethodInvoker)delegate {
                lblDeviceName.Text = _device.Name;
                lblDeviceIP.Text = $"{_device.IpAddress}:{_device.Port}";
                
                UpdateConnectionStatusDisplay();
                UpdateDoorStatusDisplay();
            });
        }

        private void UpdateConnectionStatusDisplay()
        {
            if (_device.IsConnected)
            {
                pbStatusIcon.BackColor = Color.Green;
                pbStatusIcon.Image = CreateStatusIcon(Color.Green, true);
                lblStatus.Text = "在线";
                lblStatus.ForeColor = Color.Green;
                pnlMain.BackColor = Color.White;
                pnlMain.BorderStyle = BorderStyle.FixedSingle;
            }
            else
            {
                pbStatusIcon.BackColor = Color.Gray;
                pbStatusIcon.Image = CreateStatusIcon(Color.Gray, false);
                lblStatus.Text = "离线";
                lblStatus.ForeColor = Color.Gray;
                pnlMain.BackColor = Color.LightGray;
                pnlMain.BorderStyle = BorderStyle.FixedSingle;
            }
        }

        private void UpdateDoorStatusDisplay()
        {
            if (!_device.IsConnected)
            {
                pbDoorIcon.BackColor = Color.Gray;
                pbDoorIcon.Image = CreateDoorIcon(Color.Gray, "offline");
                lblDoorStatus.Text = "设备离线";
                lblDoorStatus.ForeColor = Color.Gray;
                return;
            }

            if (_isDoorOpen)
            {
                pbDoorIcon.BackColor = Color.Red;
                pbDoorIcon.Image = CreateDoorIcon(Color.Red, "open");
                lblDoorStatus.Text = "门已开启";
                lblDoorStatus.ForeColor = Color.Red;
            }
            else if (_isDoorLocked)
            {
                pbDoorIcon.BackColor = Color.Blue;
                pbDoorIcon.Image = CreateDoorIcon(Color.Blue, "locked");
                lblDoorStatus.Text = "门已锁定";
                lblDoorStatus.ForeColor = Color.Blue;
            }
            else
            {
                pbDoorIcon.BackColor = Color.Orange;
                pbDoorIcon.Image = CreateDoorIcon(Color.Orange, "unlocked");
                lblDoorStatus.Text = "门已解锁";
                lblDoorStatus.ForeColor = Color.Orange;
            }
        }

        // 创建状态图标
        private Bitmap CreateStatusIcon(Color color, bool isConnected)
        {
            Bitmap bitmap = new Bitmap(16, 16);
            using (Graphics g = Graphics.FromImage(bitmap))
            {
                g.Clear(Color.Transparent);
                using (SolidBrush brush = new SolidBrush(color))
                {
                    if (isConnected)
                    {
                        // 绘制绿色圆点表示在线
                        g.FillEllipse(brush, 2, 2, 12, 12);
                    }
                    else
                    {
                        // 绘制灰色方块表示离线
                        g.FillRectangle(brush, 2, 2, 12, 12);
                    }
                }
            }
            return bitmap;
        }

        // 创建门状态图标
        private Bitmap CreateDoorIcon(Color color, string status)
        {
            Bitmap bitmap = new Bitmap(16, 16);
            using (Graphics g = Graphics.FromImage(bitmap))
            {
                g.Clear(Color.Transparent);
                using (Pen pen = new Pen(color, 2))
                {
                    // 绘制门的轮廓
                    g.DrawRectangle(pen, 3, 3, 10, 12);
                    
                    using (SolidBrush brush = new SolidBrush(color))
                    {
                        switch (status)
                        {
                            case "open":
                                // 开门状态 - 绘制打开的门
                                g.DrawLine(pen, 3, 3, 13, 3);
                                g.DrawLine(pen, 13, 3, 13, 15);
                                break;
                            case "locked":
                                // 锁定状态 - 绘制锁
                                g.FillEllipse(brush, 6, 6, 4, 4);
                                g.DrawLine(pen, 8, 10, 8, 13);
                                break;
                            case "unlocked":
                                // 解锁状态 - 绘制开锁
                                g.DrawEllipse(pen, 6, 6, 4, 4);
                                break;
                            case "offline":
                                // 离线状态 - 绘制X
                                g.DrawLine(pen, 5, 5, 11, 11);
                                g.DrawLine(pen, 11, 5, 5, 11);
                                break;
                        }
                    }
                }
            }
            return bitmap;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _refreshTimer?.Stop();
                _refreshTimer?.Dispose();
            }
            base.Dispose(disposing);
        }

        private System.Windows.Forms.Panel pnlMain;
        private System.Windows.Forms.Label lblDeviceName;
        private System.Windows.Forms.Label lblDeviceIP;
        private System.Windows.Forms.Label lblStatus;
        private System.Windows.Forms.Label lblDoorStatus;
        private System.Windows.Forms.PictureBox pbStatusIcon;
        private System.Windows.Forms.PictureBox pbDoorIcon;
    }
}