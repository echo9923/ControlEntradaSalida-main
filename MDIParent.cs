using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ControlEntradaSalida
{
    public partial class MDIParent : Form
    {
        // 设备状态面板
        private FlowLayoutPanel deviceStatusPanel;
        private Dictionary<int, Panel> deviceStatusCards;
        private Timer animationTimer;
        private Dictionary<int, DateTime> lastUpdateTimes;
        private ToolTip deviceToolTip;

        //初始化窗体和控件
        public MDIParent()
        {
            InitializeComponent();
            InitializeDeviceStatusPanel();
            InitializeAnimationTimer();

            // 添加窗体事件处理，确保悬停状态正确清理
            this.Deactivate += (sender, e) => ClearAllHoverStates();
            this.Leave += (sender, e) => ClearAllHoverStates();
        }

        // 初始化动画定时器
        private void InitializeAnimationTimer()
        {
            lastUpdateTimes = new Dictionary<int, DateTime>();
            animationTimer = new Timer();
            animationTimer.Interval = 100; // 100ms间隔
            animationTimer.Tick += AnimationTimer_Tick;
            animationTimer.Start();

            // 初始化工具提示
            deviceToolTip = new ToolTip();
            deviceToolTip.AutoPopDelay = 5000;
            deviceToolTip.InitialDelay = 500;
            deviceToolTip.ReshowDelay = 100;
            deviceToolTip.ShowAlways = true;
            deviceToolTip.IsBalloon = true;
        }

        // 动画定时器事件
        private void AnimationTimer_Tick(object sender, EventArgs e)
        {
            // 为在线设备添加脉冲动画效果
            foreach (var kvp in deviceStatusCards)
            {
                var cardPanel = kvp.Value;

                // 移除悬停状态检查逻辑，避免与MouseEnter/MouseLeave事件冲突
                // 让Windows Forms的原生鼠标事件处理机制来管理悬停状态

                // 查找状态图标
                Panel statusIcon = null;
                foreach (Control control in cardPanel.Controls)
                {
                    if (control is Panel && control.Size.Width == 16)
                    {
                        statusIcon = (Panel)control;
                        break;
                    }
                }

                // 如果是在线状态的绿色图标，添加脉冲效果
                if (statusIcon != null && statusIcon.BackColor == Color.FromArgb(40, 167, 69))
                {
                    // 计算脉冲透明度
                    double pulseValue = (Math.Sin(DateTime.Now.Millisecond * 0.01) + 1) * 0.3 + 0.4;
                    int alpha = (int)(255 * pulseValue);

                    // 更新图标颜色以产生脉冲效果
                    statusIcon.BackColor = Color.FromArgb(alpha, 40, 167, 69);
                    statusIcon.Invalidate();
                }
            }
        }
        // 初始化设备状态面板
        private void InitializeDeviceStatusPanel()
        {
            // 创建设备状态面板
            deviceStatusPanel = new FlowLayoutPanel();
            deviceStatusPanel.Dock = DockStyle.Top;
            deviceStatusPanel.Height = 140; // 增加高度以适应新的卡片设计
            deviceStatusPanel.BackColor = Color.FromArgb(248, 249, 250); // 现代化的浅灰背景
            deviceStatusPanel.AutoScroll = true;
            deviceStatusPanel.WrapContents = true;
            deviceStatusPanel.FlowDirection = FlowDirection.LeftToRight;
            deviceStatusPanel.Padding = new Padding(15, 10, 15, 10); // 添加内边距

            // 初始化字典
            deviceStatusCards = new Dictionary<int, Panel>();

            // 将面板添加到窗体中，放在菜单栏下方
            this.Controls.Add(deviceStatusPanel);
            deviceStatusPanel.BringToFront();

            // 订阅设备状态改变事件
            DeviceConnectionManager.Instance.DeviceStatusChanged += OnDeviceStatusChanged;
        }

        // 设备状态改变事件处理
        private void OnDeviceStatusChanged(object sender, DeviceStatusChangedEventArgs e)
        {
            // 确保在UI线程上更新控件
            if (deviceStatusPanel.InvokeRequired)
            {
                deviceStatusPanel.Invoke(new Action(() => UpdateDeviceStatus(e.Device)));
            }
            else
            {
                UpdateDeviceStatus(e.Device);
            }
        }

        // 更新设备状态显示
        private void UpdateDeviceStatus(DeviceConnectionInfo device)
        {
            // 如果还没有为该设备创建控件，则创建它们
            if (!deviceStatusCards.ContainsKey(device.Id))
            {
                CreateDeviceStatusCard(device);
            }

            // 更新设备状态卡片
            UpdateDeviceStatusCard(device);
        }

        // 强制清理所有卡片的悬停状态
        private void ClearAllHoverStates()
        {
            foreach (var kvp in deviceStatusCards)
            {
                Panel cardPanel = kvp.Value;
                if (cardPanel != null && cardPanel.Tag != null && (bool)cardPanel.Tag)
                {
                    cardPanel.Tag = false;
                    cardPanel.Cursor = Cursors.Default;
                    cardPanel.Invalidate();
                }
            }
        }

        // 创建设备状态卡片
        private void CreateDeviceStatusCard(DeviceConnectionInfo device)
        {
            // 创建卡片面板
            Panel cardPanel = new Panel();
            cardPanel.Size = new Size(180, 110); // 增大卡片尺寸
            cardPanel.BackColor = Color.White;
            cardPanel.Margin = new Padding(8, 8, 8, 8);

            // 添加卡片阴影效果和圆角（通过Paint事件实现）
            cardPanel.Paint += (sender, e) =>
            {
                Panel panel = sender as Panel;
                bool isHovered = panel != null && panel.Tag != null && (bool)panel.Tag;

                // 根据悬停状态绘制不同的阴影效果
                if (isHovered)
                {
                    // 悬停时的增强阴影
                    using (var shadowBrush = new SolidBrush(Color.FromArgb(40, 0, 0, 0)))
                    {
                        e.Graphics.FillRectangle(shadowBrush, 3, 3, panel.Width - 3, panel.Height - 3);
                    }
                }
                else
                {
                    // 默认阴影
                    using (var shadowBrush = new SolidBrush(Color.FromArgb(20, 0, 0, 0)))
                    {
                        e.Graphics.FillRectangle(shadowBrush, 2, 2, panel.Width - 2, panel.Height - 2);
                    }
                }

                // 根据悬停状态绘制不同的背景和边框
                if (isHovered)
                {
                    // 悬停时的背景和蓝色边框
                    using (var brush = new SolidBrush(Color.FromArgb(248, 249, 250)))
                    using (var pen = new Pen(Color.FromArgb(0, 123, 255), 2))
                    {
                        var rect = new Rectangle(0, 0, panel.Width - 4, panel.Height - 4);
                        var radius = 8;
                        var path = GetRoundedRectPath(rect, radius);
                        e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                        e.Graphics.FillPath(brush, path);
                        e.Graphics.DrawPath(pen, path);
                    }
                }
                else
                {
                    // 默认背景和边框
                    using (var brush = new SolidBrush(Color.White))
                    using (var pen = new Pen(Color.FromArgb(230, 230, 230), 1))
                    {
                        var rect = new Rectangle(0, 0, panel.Width - 3, panel.Height - 3);
                        var radius = 8;
                        var path = GetRoundedRectPath(rect, radius);
                        e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                        e.Graphics.FillPath(brush, path);
                        e.Graphics.DrawPath(pen, path);
                    }
                }
            };

            // 创建设备名称标签
            Label nameLabel = new Label();
            nameLabel.Text = device.Name;
            nameLabel.Font = new Font("Microsoft YaHei", 9F, FontStyle.Bold); // 使用微软雅黑字体
            nameLabel.TextAlign = ContentAlignment.MiddleCenter;
            nameLabel.Location = new Point(10, 8);
            nameLabel.Size = new Size(160, 25);
            nameLabel.ForeColor = Color.FromArgb(51, 51, 51); // 深灰色文字
            nameLabel.BackColor = Color.Transparent;

            // 创建状态图标（使用圆形设计）
            Panel statusIcon = new Panel();
            statusIcon.Size = new Size(16, 16);
            statusIcon.Location = new Point(15, 45);
            statusIcon.Paint += (sender, e) =>
            {
                e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                using (var brush = new SolidBrush(statusIcon.BackColor))
                {
                    e.Graphics.FillEllipse(brush, 0, 0, 15, 15);
                }
            };

            // 创建状态标签
            Label statusLabel = new Label();
            statusLabel.Size = new Size(120, 20);
            statusLabel.Location = new Point(40, 42);
            statusLabel.TextAlign = ContentAlignment.MiddleLeft;
            statusLabel.Font = new Font("Microsoft YaHei", 8.5F, FontStyle.Regular);
            statusLabel.BackColor = Color.Transparent;

            // 创建设备IP地址标签
            Label ipLabel = new Label();
            ipLabel.Text = device.IpAddress;
            ipLabel.Font = new Font("Consolas", 7.5F, FontStyle.Regular); // 使用等宽字体显示IP
            ipLabel.TextAlign = ContentAlignment.MiddleCenter;
            ipLabel.Location = new Point(10, 70);
            ipLabel.Size = new Size(160, 15);
            ipLabel.ForeColor = Color.FromArgb(128, 128, 128); // 灰色IP地址
            ipLabel.BackColor = Color.Transparent;

            // 移除时间显示功能 - 不再创建updateTimeLabel

            // 根据设备状态设置初始显示
            UpdateStatusDisplay(device, statusIcon, statusLabel);

            // 将控件添加到卡片面板
            cardPanel.Controls.Add(nameLabel);
            cardPanel.Controls.Add(statusIcon);
            cardPanel.Controls.Add(statusLabel);
            cardPanel.Controls.Add(ipLabel);

            // 添加鼠标悬停效果和点击效果
            // 使用Tag属性存储悬停状态，确保状态管理的可靠性
            cardPanel.Tag = false; // 初始化悬停状态为false

            cardPanel.MouseEnter += (sender, e) =>
            {
                Panel panel = sender as Panel;
                if (panel != null)
                {
                    // 设置悬停状态为true
                    panel.Tag = true;
                    panel.Cursor = Cursors.Hand;
                    panel.Invalidate(); // 触发重绘以显示悬停效果
                }
            };

            cardPanel.MouseLeave += (sender, e) =>
            {
                Panel panel = sender as Panel;
                if (panel != null)
                {
                    // 清除悬停状态
                    panel.Tag = false;
                    panel.Cursor = Cursors.Default;
                    panel.Invalidate(); // 触发重绘以恢复默认效果
                }
            };

            // 添加点击效果
            cardPanel.Click += (sender, e) =>
            {
                // 可以在这里添加点击设备卡片的功能，比如显示详细信息
                MessageBox.Show($"设备: {device.Name}\nIP: {device.IpAddress}\n状态: {statusLabel.Text}",
                    "设备信息", MessageBoxButtons.OK, MessageBoxIcon.Information);
            };

            // 设置工具提示（移除时间信息）
            string tooltipText = $"设备名称: {device.Name}\n" +
                               $"IP地址: {device.IpAddress}\n" +
                               $"设备ID: {device.Id}\n" +
                               $"当前状态: {statusLabel.Text}\n" +
                               $"点击查看详细信息";
            deviceToolTip.SetToolTip(cardPanel, tooltipText);

            // 将卡片面板添加到主面板和字典中
            deviceStatusPanel.Controls.Add(cardPanel);
            deviceStatusCards.Add(device.Id, cardPanel);
        }

        // 获取圆角矩形路径的辅助方法
        private System.Drawing.Drawing2D.GraphicsPath GetRoundedRectPath(Rectangle rect, int radius)
        {
            var path = new System.Drawing.Drawing2D.GraphicsPath();
            path.AddArc(rect.X, rect.Y, radius, radius, 180, 90);
            path.AddArc(rect.X + rect.Width - radius, rect.Y, radius, radius, 270, 90);
            path.AddArc(rect.X + rect.Width - radius, rect.Y + rect.Height - radius, radius, radius, 0, 90);
            path.AddArc(rect.X, rect.Y + rect.Height - radius, radius, radius, 90, 90);
            path.CloseAllFigures();
            return path;
        }



        // 更新设备状态卡片
        private void UpdateDeviceStatusCard(DeviceConnectionInfo device)
        {
            if (deviceStatusCards.ContainsKey(device.Id))
            {
                Panel cardPanel = deviceStatusCards[device.Id];

                // 查找状态图标和标签
                Panel statusIcon = null;
                Label statusLabel = null;

                // 遍历控件查找状态图标和状态标签
                foreach (Control control in cardPanel.Controls)
                {
                    if (control is Panel && control.Size.Width == 16) // 状态图标面板
                    {
                        statusIcon = (Panel)control;
                    }
                    else if (control is Label)
                    {
                        Label label = (Label)control;
                        if (label.Location.X == 40 && label.Location.Y == 42) // 状态标签
                        {
                            statusLabel = label;
                        }
                    }
                }

                // 更新状态显示
                if (statusIcon != null && statusLabel != null)
                {
                    UpdateStatusDisplay(device, statusIcon, statusLabel);
                }

                // 更新工具提示（移除时间信息）
                string tooltipText = $"设备名称: {device.Name}\n" +
                                   $"IP地址: {device.IpAddress}\n" +
                                   $"设备ID: {device.Id}\n" +
                                   $"当前状态: {(statusLabel != null ? statusLabel.Text : "未知")}\n" +
                                   $"点击查看详细信息";
                deviceToolTip.SetToolTip(cardPanel, tooltipText);
            }
        }

        // 更新状态显示
        private void UpdateStatusDisplay(DeviceConnectionInfo device, Panel statusIcon, Label statusLabel)
        {
            // 如果设备被禁用，显示为红色
            if (!device.IsEnabled)
            {
                statusIcon.BackColor = Color.FromArgb(220, 53, 69); // 现代化的红色
                statusLabel.Text = "已禁用";
                statusLabel.ForeColor = Color.FromArgb(220, 53, 69);
                statusIcon.Invalidate(); // 重绘圆形图标
                return;
            }

            // 根据设备状态设置图标颜色和标签文本
            switch (device.Status)
            {
                case DeviceStatus.Online:
                    statusIcon.BackColor = Color.FromArgb(40, 167, 69); // 现代化的绿色
                    statusLabel.Text = "在线";
                    statusLabel.ForeColor = Color.FromArgb(40, 167, 69);
                    break;
                case DeviceStatus.Offline:
                    statusIcon.BackColor = Color.FromArgb(108, 117, 125); // 现代化的灰色
                    statusLabel.Text = "离线";
                    statusLabel.ForeColor = Color.FromArgb(108, 117, 125);
                    break;
                case DeviceStatus.AlwaysOpen:
                    statusIcon.BackColor = Color.FromArgb(255, 193, 7); // 现代化的黄色
                    statusLabel.Text = "常开";
                    statusLabel.ForeColor = Color.FromArgb(255, 143, 0); // 橙色文字
                    break;
                case DeviceStatus.AlwaysClose:
                    statusIcon.BackColor = Color.FromArgb(220, 53, 69); // 现代化的红色
                    statusLabel.Text = "常闭";
                    statusLabel.ForeColor = Color.FromArgb(220, 53, 69);
                    break;
                default:
                    statusIcon.BackColor = Color.FromArgb(108, 117, 125); // 现代化的灰色
                    statusLabel.Text = "未知";
                    statusLabel.ForeColor = Color.FromArgb(108, 117, 125);
                    break;
            }

            // 重绘圆形图标以应用新颜色
            statusIcon.Invalidate();
        }

        // 设备管理窗口
        private void gestiónDeDispositivosToolStripMenuItem_Click(object sender, EventArgs e)
        {
            GestionDispositivos frmGestionDispositivos = new GestionDispositivos();
            frmGestionDispositivos.MdiParent = this;
            frmGestionDispositivos.Show();
        }
        //员工管理窗口
        private void gestionDeEmpleadosToolStripMenuItem_Click(object sender, EventArgs e)
        {
            GestionEmpleados frmGestionEmpleados = new GestionEmpleados();
            frmGestionEmpleados.MdiParent = this;
            frmGestionEmpleados.Show();
        }
        //进出记录采集窗口
        private void CapturarEntradaSalidaToolStripMenuItem_Click(object sender, EventArgs e)
        {
            CapturaEntradaSalida frmCapturaEntradaSalida = new CapturaEntradaSalida();
            frmCapturaEntradaSalida.MdiParent = this;
            frmCapturaEntradaSalida.Show();
        }
        //程序退出
        private void salirToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        //事件报表参数窗口
        private void eventosToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ParamInformeEventos frmParamInformeEventos = new ParamInformeEventos();
            frmParamInformeEventos.MdiParent = this;
            frmParamInformeEventos.Show();
        }
        //窗体加载时的初始化
        private void MDIParent_Load(object sender, EventArgs e)
        {
            if (!Common.InicializarSDKHikVision())//SDK初始化
                MessageBox.Show("海康威视SDK初始化失败", "初始化错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            if (!Common.CrearDirectorioData())
                MessageBox.Show("创建数据目录失败", "文件错误", MessageBoxButtons.OK, MessageBoxIcon.Error);

            // 初始化设备连接管理器
            DeviceConnectionManager.Instance.LoadAllDevices();

            // 初始化设备状态显示
            InitializeDeviceStatusDisplay();
        }

        // 初始化设备状态显示
        private void InitializeDeviceStatusDisplay()
        {
            var devices = DeviceConnectionManager.Instance.GetAllDevices();

            // 首先为所有设备创建卡片（不检查状态），避免UI阻塞
            foreach (var device in devices)
            {
                CreateDeviceStatusCard(device);
            }

            // 然后异步检查所有设备状态
            Task.Run(async () =>
            {
                // 并行检查所有设备状态，提高加载速度
                var tasks = devices.Select(device => Task.Run(() =>
                {
                    // 为每个设备检查状态
                    DeviceConnectionManager.Instance.CheckDeviceStatus(device);
                })).ToArray();

                // 等待所有设备状态检查完成
                await Task.WhenAll(tasks);
            });
        }
        //窗体关闭前的清理工作
        private void MDIParent_FormClosing(object sender, FormClosingEventArgs e)
        {
            // 停止动画定时器
            if (animationTimer != null)
            {
                animationTimer.Stop();
                animationTimer.Dispose();
            }

            // 释放工具提示资源
            if (deviceToolTip != null)
            {
                deviceToolTip.Dispose();
            }

            // 取消订阅设备状态改变事件
            DeviceConnectionManager.Instance.DeviceStatusChanged -= OnDeviceStatusChanged;

            // 断开所有设备连接
            DeviceConnectionManager.Instance.DisconnectAllDevices();
            DeviceConnectionManager.Instance.Dispose();

            HCNetSDK.NET_DVR_Cleanup();
        }
        //设备用户信息窗口
        private void consultarDatosDispositivoToolStripMenuItem_Click(object sender, EventArgs e)
        {
            GestionUsuariosDispositivo frmGestionUsuariosDispositivo = new GestionUsuariosDispositivo();
            frmGestionUsuariosDispositivo.MdiParent = this;
            frmGestionUsuariosDispositivo.Show();

        }
        //进出记录报表窗口
        private void entradasYSalidasToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ParamInformeEntradaSalida frmParamInformeEntradaSalida = new ParamInformeEntradaSalida();
            frmParamInformeEntradaSalida.MdiParent = this;
            frmParamInformeEntradaSalida.Show();
        }

        private void menuStrip1_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {

        }

        private void controldoor_Click(object sender, EventArgs e)
        {
            controldoor frmGestionUsuariosDispositivo = new controldoor();
            frmGestionUsuariosDispositivo.MdiParent = this;
            frmGestionUsuariosDispositivo.Show();
        }

        private void Plantemplate_Click(object sender, EventArgs e)
        {
            Plantemplate frmGestionUsuariosDispositivo = new Plantemplate();
            frmGestionUsuariosDispositivo.MdiParent = this;
            frmGestionUsuariosDispositivo.Show();
        }
    }
}
