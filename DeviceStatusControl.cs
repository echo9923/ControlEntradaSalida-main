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
        private DeviceStatusManager _statusManager;
        private DeviceStatusInfo _lastStatus;

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
            _statusManager = DeviceStatusManager.Instance;
            InitializeTimer();
            SetupEventHandlers();

            // 订阅设备状态变更事件，与 DeviceConnectionManager 保持同步
            DeviceConnectionManager.Instance.DeviceStatusChanged += OnDeviceStatusChanged;
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
            _refreshTimer.Interval = 10000; // 降低到10秒，主要用于门状态的细节检查
            _refreshTimer.Tick += RefreshTimer_Tick;
            _refreshTimer.Start();
        }

        /// <summary>
        /// 处理设备状态变更事件 - 与 DeviceConnectionManager 保持同步
        /// </summary>
        private void OnDeviceStatusChanged(object sender, DeviceStatusChangedEventArgs e)
        {
            // 只处理当前设备的状态变更
            if (_device != null && e.Device.Id == _device.Id)
            {
                // 更新本地设备引用
                _device = e.Device;

                // 线程安全地更新UI显示
                this.Invoke((MethodInvoker)delegate {
                    UpdateDisplay();
                });
            }
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
                // 使用真实的SDK调用获取设备状态
                _lastStatus = _statusManager.GetRealDeviceStatus(_device);
                
                // 根据SDK返回的状态更新本地变量
                UpdateLocalStatusFromSDK(_lastStatus);
                
                // 线程安全更新UI
                this.Invoke((MethodInvoker)delegate {
                    UpdateDoorStatusDisplay();
                    UpdateDeviceHealthIndicator(_lastStatus);
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"检查门状态时出错: {ex.Message}");
                this.Invoke((MethodInvoker)delegate {
                    ShowOfflineStatus("状态检查失败");
                });
            }
        }

        /// <summary>
        /// 根据SDK返回的状态更新本地状态变量
        /// </summary>
        /// <param name="statusInfo">SDK返回的设备状态信息</param>
        private void UpdateLocalStatusFromSDK(DeviceStatusInfo statusInfo)
        {
            if (statusInfo == null || !statusInfo.IsOnline)
            {
                _isDoorOpen = false;
                _isDoorLocked = true;
                return;
            }

            // 根据海康威视SDK状态判断门的开启状态
            _isDoorOpen = statusInfo.DoorStatus == DoorStatus.AlwaysOpen;
            
            // 根据门锁状态判断锁定状态：常开状态表示门锁打开（未锁定）
            _isDoorLocked = statusInfo.DoorLockStatus != DoorLockStatus.NormalOpen;
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
            // 优先检查设备是否已启用
            if (!_device.IsEnabled)
            {
                pbStatusIcon.BackColor = Color.Red;
                pbStatusIcon.Image = CreateStatusIcon(Color.Red, false);
                lblStatus.Text = "已禁用";
                lblStatus.ForeColor = Color.Red;
                pnlMain.BackColor = Color.LightGray;
                pnlMain.BorderStyle = BorderStyle.FixedSingle;
                return;
            }

            // 检查连接状态
            if (_device.IsConnected)
            {
                // 根据具体状态设置颜色
                Color statusColor = Color.Green;
                string statusText = "在线";

                switch (_device.Status)
                {
                    case DeviceStatus.Online:
                        statusColor = Color.Green;
                        statusText = "在线";
                        break;
                    case DeviceStatus.AlwaysOpen:
                        statusColor = Color.Orange;
                        statusText = "常开";
                        break;
                    case DeviceStatus.AlwaysClose:
                        statusColor = Color.Red;
                        statusText = "常闭";
                        break;
                    case DeviceStatus.Unknown:
                        statusColor = Color.Yellow;
                        statusText = "状态未知";
                        break;
                    default:
                        statusColor = Color.Green;
                        statusText = "在线";
                        break;
                }

                pbStatusIcon.BackColor = statusColor;
                pbStatusIcon.Image = CreateStatusIcon(statusColor, true);
                lblStatus.Text = statusText;
                lblStatus.ForeColor = statusColor;
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

        /// <summary>
        /// 更新设备健康指示器
        /// </summary>
        /// <param name="statusInfo">设备状态信息</param>
        private void UpdateDeviceHealthIndicator(DeviceStatusInfo statusInfo)
        {
            if (statusInfo == null || !statusInfo.IsOnline)
            {
                ShowOfflineStatus("设备离线");
                return;
            }

            // 根据综合状态更新显示
            switch (statusInfo.OverallStatus)
            {
                case DeviceOverallStatus.Online:
                    UpdateHealthStatus(Color.Green, "正常", statusInfo.StatusMessage);
                    break;
                case DeviceOverallStatus.OnlineWithWarning:
                    UpdateHealthStatus(Color.Orange, "警告", statusInfo.StatusMessage);
                    break;
                case DeviceOverallStatus.OnlineWithError:
                    UpdateHealthStatus(Color.Red, "错误", statusInfo.StatusMessage);
                    break;
                case DeviceOverallStatus.Offline:
                default:
                    ShowOfflineStatus(statusInfo.StatusMessage);
                    break;
            }
        }

        /// <summary>
        /// 更新健康状态显示
        /// </summary>
        private void UpdateHealthStatus(Color color, string status, string tooltip)
        {
            pbStatusIcon.BackColor = color;
            pbStatusIcon.Image = CreateStatusIcon(color, true);
            lblStatus.Text = $"在线 ({status})"; 
            lblStatus.ForeColor = color;
            
            // 设置工具提示
            if (!string.IsNullOrEmpty(tooltip))
            {
                var toolTip = new ToolTip();
                toolTip.SetToolTip(pnlMain, tooltip);
            }
        }

        /// <summary>
        /// 显示离线状态
        /// </summary>
        /// <param name="message">离线消息</param>
        private void ShowOfflineStatus(string message)
        {
            pbStatusIcon.BackColor = Color.Gray;
            pbStatusIcon.Image = CreateStatusIcon(Color.Gray, false);
            lblStatus.Text = "离线";
            lblStatus.ForeColor = Color.Gray;
            
            pbDoorIcon.BackColor = Color.Gray;
            pbDoorIcon.Image = CreateDoorIcon(Color.Gray, "offline");
            lblDoorStatus.Text = message;
            lblDoorStatus.ForeColor = Color.Gray;
            
            pnlMain.BackColor = Color.LightGray;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                // 清理定时器
                _refreshTimer?.Stop();
                _refreshTimer?.Dispose();

                // 取消订阅设备状态变更事件
                try
                {
                    DeviceConnectionManager.Instance.DeviceStatusChanged -= OnDeviceStatusChanged;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"取消订阅设备状态事件时发生异常: {ex.Message}");
                }
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