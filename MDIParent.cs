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
using System.Runtime.InteropServices;
using MySql.Data.MySqlClient;

namespace ControlEntradaSalida
{
    // 双缓冲面板类，减少闪烁
    public class BufferedPanel : Panel
    {
        public BufferedPanel()
        {
            this.DoubleBuffered = true;
            this.SetStyle(
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.UserPaint |
                ControlStyles.OptimizedDoubleBuffer,
                true);
            this.UpdateStyles();
        }
    }

    // 双缓冲FlowLayoutPanel类，减少闪烁
    public class BufferedFlowLayoutPanel : FlowLayoutPanel
    {
        public BufferedFlowLayoutPanel()
        {
            this.DoubleBuffered = true;
            this.SetStyle(
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.UserPaint |
                ControlStyles.OptimizedDoubleBuffer,
                true);
            this.UpdateStyles();
        }
    }

    public partial class MDIParent : Form, IRefreshableForm
    {
        // 设备状态面板
        private Panel mainDevicePanel;
        private Dictionary<string, BufferedFlowLayoutPanel> categoryPanels;
        private Dictionary<int, Panel> deviceStatusCards;
        private Timer animationTimer;
        private Dictionary<int, DateTime> lastUpdateTimes;
        private ToolTip deviceToolTip;
        private DataChangeNotifier _notifier;
        private readonly object _statusUpdateLock = new object();
        
        public bool IsFormVisible => this.Visible;

        //初始化窗体和控件
        public MDIParent()
        {
            InitializeComponent();
            InitializeDeviceStatusPanel();
            InitializeAnimationTimer();

            // 添加窗体事件处理，确保悬停状态正确清理
            this.Deactivate += (sender, e) => ClearAllHoverStates();
            this.Leave += (sender, e) => ClearAllHoverStates();
            
            // 初始化数据变更通知器
            InitializeDataChangeNotifier();
        }

        // 初始化动画定时器
        private void InitializeAnimationTimer()
        {
            lastUpdateTimes = new Dictionary<int, DateTime>();
            animationTimer = new Timer();
            animationTimer.Interval = 150; // 增加间隔到150ms，减少重绘频率
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
            DateTime now = DateTime.Now;
            bool needsFullRefresh = false;

            // 为在线设备添加脉冲动画效果
            foreach (var kvp in deviceStatusCards)
            {
                var cardPanel = kvp.Value;

                // 降低状态验证频率，减少不必要的重绘
                if (now.Millisecond % 300 < 150) // 每300ms验证一次状态
                {
                    if (ValidateAndCorrectHoverState(cardPanel))
                    {
                        needsFullRefresh = true;
                    }
                }

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
                if (statusIcon != null && statusIcon.BackColor.R == 40 && statusIcon.BackColor.G == 167 && statusIcon.BackColor.B == 69)
                {
                    // 计算脉冲透明度，使用更平滑的算法
                    double pulseValue = (Math.Sin(now.Millisecond * 0.008) + 1) * 0.25 + 0.5;
                    int alpha = (int)(255 * pulseValue);

                    // 只在透明度变化较大时更新
                    var currentAlpha = statusIcon.BackColor.A;
                    if (Math.Abs(alpha - currentAlpha) > 5)
                    {
                        // 更新图标颜色以产生脉冲效果
                        statusIcon.BackColor = Color.FromArgb(alpha, 40, 167, 69);
                        statusIcon.Invalidate();
                    }
                }
            }

            // 只有在需要时才进行全量刷新
            if (needsFullRefresh)
            {
                mainDevicePanel.Invalidate(false);
            }
        }

        // 验证和纠正悬停状态
        private bool ValidateAndCorrectHoverState(Panel cardPanel)
        {
            if (cardPanel == null || cardPanel.Tag == null) return false;

            try
            {
                var state = cardPanel.Tag as dynamic;
                if (state != null)
                {
                    // 检查鼠标实际位置是否在卡片内
                    var clientPoint = cardPanel.PointToClient(MousePosition);
                    bool isMouseActuallyOver = cardPanel.ClientRectangle.Contains(clientPoint);
                    
                    // 如果状态与实际情况不符，进行纠正
                    if (state.IsHovered != isMouseActuallyOver)
                    {
                        lock (state.SyncLock)
                        {
                            cardPanel.Tag = new { IsHovered = isMouseActuallyOver, SyncLock = state.SyncLock };
                            cardPanel.Cursor = isMouseActuallyOver ? Cursors.Hand : Cursors.Default;
                            cardPanel.Invalidate();
                            return true; // 返回true表示发生了状态变化
                        }
                    }
                }
            }
            catch
            {
                // 如果状态对象格式不正确，重置为默认状态
                var syncLock = new object();
                cardPanel.Tag = new { IsHovered = false, SyncLock = syncLock };
                cardPanel.Cursor = Cursors.Default;
                cardPanel.Invalidate();
                return true; // 返回true表示发生了状态变化
            }
            return false; // 返回false表示没有状态变化
        }
        // 初始化设备状态面板
        private void InitializeDeviceStatusPanel()
        {
            // 创建主设备面板
            mainDevicePanel = new Panel();
            mainDevicePanel.Dock = DockStyle.Top;
            mainDevicePanel.Height = 600; // 增加高度以容纳所有类别面板
            mainDevicePanel.BackColor = Color.FromArgb(248, 249, 250);
            mainDevicePanel.AutoScroll = true;
            mainDevicePanel.Padding = new Padding(15, 10, 15, 10);

            // 初始化字典
            deviceStatusCards = new Dictionary<int, Panel>();
            categoryPanels = new Dictionary<string, BufferedFlowLayoutPanel>();

            // 创建类别面板
            CreateCategoryPanels();

            // 将面板添加到窗体中，放在菜单栏下方
            this.Controls.Add(mainDevicePanel);
            mainDevicePanel.BringToFront();

            // 订阅设备状态改变事件
            DeviceConnectionManager.Instance.DeviceStatusChanged += OnDeviceStatusChanged;
        }
        
        // 获取设备类别
        private string GetDeviceCategory(int deviceId)
        {
            string category = "类别1"; // 默认类别
            Common cmn = new Common();
            string connstr = cmn.obtenerCadenaConexion();
            BaseDatosMySQL bd = new BaseDatosMySQL();
            bd.conectarMySQL(connstr);
            
            if (bd.conn != null)
            {
                string sql = "SELECT description FROM devices WHERE device_id = @device_id";
                try
                {
                    MySqlCommand cmd = new MySqlCommand(sql, bd.conn);
                    cmd.Parameters.AddWithValue("@device_id", deviceId);
                    MySqlDataReader rdr = cmd.ExecuteReader();
                    
                    if (rdr.HasRows && rdr.Read())
                    {
                        string description = rdr["description"].ToString();
                        if (!string.IsNullOrEmpty(description))
                        {
                            category = description;
                        }
                    }
                    rdr.Close();
                }
                catch (Exception ex)
                {
                    // 记录错误但使用默认类别
                    Console.WriteLine($"获取设备类别时出错: {ex.Message}");
                }
                finally
                {
                    bd.desconectarMySQL();
                }
            }
            
            return category;
        }

        // 创建类别面板
        private void CreateCategoryPanels()
        {
            string[] categories = { "类别1", "类别2", "类别3" };
            int yPosition = 0;

            foreach (string category in categories)
            {
                // 创建类别标题标签
                Label categoryLabel = new Label();
                categoryLabel.Text = category;
                categoryLabel.Font = new Font("Microsoft YaHei", 10, FontStyle.Bold);
                categoryLabel.ForeColor = Color.FromArgb(52, 58, 64);
                categoryLabel.AutoSize = true;
                categoryLabel.Location = new Point(5, yPosition);
                mainDevicePanel.Controls.Add(categoryLabel);

                yPosition += 25;

                // 创建类别设备面板
                BufferedFlowLayoutPanel categoryPanel = new BufferedFlowLayoutPanel();
                categoryPanel.Location = new Point(0, yPosition);
                categoryPanel.Width = mainDevicePanel.Width - 30;
                categoryPanel.Height = 150; // 增加高度以容纳更多卡片
                categoryPanel.BackColor = Color.Transparent;
                categoryPanel.WrapContents = true;
                categoryPanel.FlowDirection = FlowDirection.LeftToRight;
                categoryPanel.Padding = new Padding(5, 0, 5, 0);
                categoryPanel.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
                categoryPanel.AutoSize = true; // 启用自动调整大小
                categoryPanel.AutoSizeMode = AutoSizeMode.GrowAndShrink; // 根据内容自动增长和收缩

                categoryPanels[category] = categoryPanel;
                mainDevicePanel.Controls.Add(categoryPanel);

                yPosition += 170; // 调整间距以适应新的面板高度
            }
        }

        // 清理所有设备状态卡片
        private void ClearAllDeviceStatusCards()
        {
            // 从所有类别面板中移除控件
            foreach (var panel in categoryPanels.Values)
            {
                panel.Controls.Clear();
            }
            
            // 清空字典
            deviceStatusCards.Clear();
        }
        
        // 初始化数据变更通知器
        private void InitializeDataChangeNotifier()
        {
            _notifier = DataChangeNotifier.Instance;
            _notifier.DeviceDataChanged += OnDeviceDataChanged;
            _notifier.DoorControlStatusChanged += OnDoorControlStatusChanged;
        }

        // 设备状态改变事件处理
        private void OnDeviceStatusChanged(object sender, DeviceStatusChangedEventArgs e)
        {
            // 确保在UI线程上更新控件
            if (mainDevicePanel.InvokeRequired)
            {
                mainDevicePanel.Invoke(new Action(() => UpdateDeviceStatus(e.Device)));
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
                if (cardPanel != null && cardPanel.Tag != null)
                {
                    var state = cardPanel.Tag as dynamic;
                    if (state != null && state.IsHovered)
                    {
                        // 使用同步锁确保状态更新的原子性
                        lock (state.SyncLock)
                        {
                            cardPanel.Tag = new { IsHovered = false, SyncLock = state.SyncLock };
                            cardPanel.Cursor = Cursors.Default;
                            cardPanel.Invalidate();
                        }
                    }
                }
            }
        }

        // 创建设备状态卡片
        private void CreateDeviceStatusCard(DeviceConnectionInfo device)
        {
            // 创建卡片面板
            Panel cardPanel = new BufferedPanel(); // 使用自定义的双缓冲面板
            cardPanel.Size = new Size(180, 110); // 增大卡片尺寸
            cardPanel.BackColor = Color.White;
            cardPanel.Margin = new Padding(8, 8, 8, 8);

            // 添加卡片阴影效果和圆角（通过Paint事件实现）
            cardPanel.Paint += (sender, e) =>
            {
                Panel panel = sender as Panel;
                bool isHovered = false;
                
                // 检查新的状态管理对象
                if (panel != null && panel.Tag != null)
                {
                    var state = panel.Tag as dynamic;
                    if (state != null)
                    {
                        isHovered = state.IsHovered;
                    }
                }

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
            Panel statusIcon = new BufferedPanel();
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
            // 添加状态同步锁，防止快速操作时的竞态条件
            var syncLock = new object();
            cardPanel.Tag = new { IsHovered = false, SyncLock = syncLock };

            cardPanel.MouseEnter += (sender, e) =>
            {
                Panel panel = sender as Panel;
                if (panel != null)
                {
                    lock (syncLock)
                    {
                        var state = panel.Tag as dynamic;
                        if (state != null && !state.IsHovered)
                        {
                            // 更新悬停状态为true
                            panel.Tag = new { IsHovered = true, SyncLock = syncLock };
                            panel.Cursor = Cursors.Hand;
                            panel.Invalidate(); // 触发重绘以显示悬停效果
                        }
                    }
                }
            };

            cardPanel.MouseLeave += (sender, e) =>
            {
                Panel panel = sender as Panel;
                if (panel != null)
                {
                    lock (syncLock)
                    {
                        var state = panel.Tag as dynamic;
                        if (state != null && state.IsHovered)
                        {
                            // 清除悬停状态
                            panel.Tag = new { IsHovered = false, SyncLock = syncLock };
                            panel.Cursor = Cursors.Default;
                            panel.Invalidate(); // 触发重绘以恢复默认效果
                        }
                    }
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

            // 获取设备类别并添加到对应的类别面板
            string deviceCategory = GetDeviceCategory(device.Id);
            
            // 如果类别不存在于预定义类别中，使用默认类别
            if (!categoryPanels.ContainsKey(deviceCategory))
            {
                deviceCategory = "类别1";
            }
            
            // 将卡片面板添加到对应的类别面板和字典中
            if (categoryPanels.ContainsKey(deviceCategory))
            {
                categoryPanels[deviceCategory].Controls.Add(cardPanel);
            }
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

            // 优先使用 IsConnected 状态，然后参考 Status 枚举进行细化
            if (!device.IsConnected)
            {
                statusIcon.BackColor = Color.FromArgb(108, 117, 125); // 现代化的灰色
                statusLabel.Text = "离线";
                statusLabel.ForeColor = Color.FromArgb(108, 117, 125);
                statusIcon.Invalidate();
                return;
            }

            // 设备已连接时，根据具体状态进行显示
            switch (device.Status)
            {
                case DeviceStatus.Online:
                    statusIcon.BackColor = Color.FromArgb(40, 167, 69); // 现代化的绿色
                    statusLabel.Text = "在线";
                    statusLabel.ForeColor = Color.FromArgb(40, 167, 69);
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
                case DeviceStatus.Unknown:
                    statusIcon.BackColor = Color.FromArgb(255, 193, 7); // 现代化的黄色
                    statusLabel.Text = "状态未知";
                    statusLabel.ForeColor = Color.FromArgb(255, 143, 0);
                    break;
                case DeviceStatus.Offline:
                default:
                    // 这些情况通常不会到达，因为我们已经在上面处理了 !IsConnected
                    statusIcon.BackColor = Color.FromArgb(108, 117, 125); // 现代化的灰色
                    statusLabel.Text = "离线";
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
            ShowOwnedTopMost(frmGestionDispositivos);
        }
        //员工管理窗口
        private void gestionDeEmpleadosToolStripMenuItem_Click(object sender, EventArgs e)
        {
            GestionEmpleados frmGestionEmpleados = new GestionEmpleados();
            ShowOwnedTopMost(frmGestionEmpleados);
        }
        //进出记录采集窗口
        private void CapturarEntradaSalidaToolStripMenuItem_Click(object sender, EventArgs e)
        {
            CapturaEntradaSalida frmCapturaEntradaSalida = new CapturaEntradaSalida();
            ShowOwnedTopMost(frmCapturaEntradaSalida);
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
            ShowOwnedTopMost(frmParamInformeEventos);
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

            // 清理现有的设备卡片，避免重复创建
            ClearAllDeviceStatusCards();

            // 为所有设备创建卡片（不检查状态），避免UI阻塞
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
            
            // 取消订阅数据变更事件
            if (_notifier != null)
            {
                _notifier.DeviceDataChanged -= OnDeviceDataChanged;
                _notifier.DoorControlStatusChanged -= OnDoorControlStatusChanged;
            }

            // 断开所有设备连接
            DeviceConnectionManager.Instance.DisconnectAllDevices();
            DeviceConnectionManager.Instance.Dispose();

            HCNetSDK.NET_DVR_Cleanup();
        }
        //设备用户信息窗口
        private void consultarDatosDispositivoToolStripMenuItem_Click(object sender, EventArgs e)
        {
            GestionUsuariosDispositivo frmGestionUsuariosDispositivo = new GestionUsuariosDispositivo();
            ShowOwnedTopMost(frmGestionUsuariosDispositivo);

        }
        //进出记录报表窗口
        private void entradasYSalidasToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ParamInformeEntradaSalida frmParamInformeEntradaSalida = new ParamInformeEntradaSalida();
            ShowOwnedTopMost(frmParamInformeEntradaSalida);
        }

        private void menuStrip1_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {

        }

        private void controldoor_Click(object sender, EventArgs e)
        {
            controldoor frmGestionUsuariosDispositivo = new controldoor();
            ShowOwnedTopMost(frmGestionUsuariosDispositivo);
        }

        private void Plantemplate_Click(object sender, EventArgs e)
        {
            Plantemplate frmGestionUsuariosDispositivo = new Plantemplate();
            ShowOwnedTopMost(frmGestionUsuariosDispositivo);
        }
        
        private void webBrowserToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ShowWebBrowserOption();
        }
        
        /// <summary>
        /// 显示网页浏览选项对话框
        /// </summary>
        private void ShowWebBrowserOption()
        {
            DialogResult result = MessageBox.Show(
                "请选择网页打开方式：\n\n是：在新窗口中打开\n否：在当前界面中打开\n取消：取消操作",
                "网页浏览选项",
                MessageBoxButtons.YesNoCancel,
                MessageBoxIcon.Question);

            switch (result)
            {
                case DialogResult.Yes:
                    OpenWebBrowserInNewWindow();
                    break;
                case DialogResult.No:
                    OpenWebBrowserInCurrentWindow();
                    break;
                case DialogResult.Cancel:
                    // 用户取消操作
                    break;
            }
        }

        /// <summary>
        /// 在新窗口中打开网页浏览器
        /// </summary>
        private void OpenWebBrowserInNewWindow()
        {
            WebBrowserForm webForm = new WebBrowserForm();
            ShowOwnedTopMost(webForm);
        }

        /// <summary>
        /// 在当前界面中打开网页浏览器
        /// </summary>
        private void OpenWebBrowserInCurrentWindow()
        {
            // 隐藏设备状态面板
            if (mainDevicePanel != null)
            {
                mainDevicePanel.Visible = false;
            }

            // 创建内嵌的WebBrowser控件
            CreateEmbeddedWebBrowser();
        }

        /// <summary>
        /// 创建内嵌的WebBrowser控件
        /// </summary>
        private void CreateEmbeddedWebBrowser()
        {
            // 检查是否已经存在WebBrowser控件
            WebBrowser existingBrowser = this.Controls.OfType<WebBrowser>().FirstOrDefault();
            if (existingBrowser != null)
            {
                existingBrowser.Visible = true;
                existingBrowser.BringToFront();
                return;
            }

            // 创建新的WebBrowser控件
            WebBrowser webBrowser = new WebBrowser();
            webBrowser.Dock = DockStyle.Fill;
            webBrowser.Url = new Uri("https://www.bilibili.com/");
            webBrowser.ScriptErrorsSuppressed = true;

            // 添加返回按钮面板
            Panel controlPanel = new Panel();
            controlPanel.Height = 40;
            controlPanel.Dock = DockStyle.Top;
            controlPanel.BackColor = Color.FromArgb(240, 240, 240);

            Button backButton = new Button();
            backButton.Text = "返回主界面";
            backButton.Size = new Size(100, 30);
            backButton.Location = new Point(10, 5);
            backButton.BackColor = Color.FromArgb(0, 123, 255);
            backButton.ForeColor = Color.White;
            backButton.FlatStyle = FlatStyle.Flat;
            backButton.Click += (s, e) => ReturnToMainInterface();

            Button refreshButton = new Button();
            refreshButton.Text = "刷新";
            refreshButton.Size = new Size(60, 30);
            refreshButton.Location = new Point(120, 5);
            refreshButton.BackColor = Color.FromArgb(40, 167, 69);
            refreshButton.ForeColor = Color.White;
            refreshButton.FlatStyle = FlatStyle.Flat;
            refreshButton.Click += (s, e) => webBrowser.Refresh();

            controlPanel.Controls.Add(backButton);
            controlPanel.Controls.Add(refreshButton);

            // 将控件添加到主窗体
            this.Controls.Add(controlPanel);
            this.Controls.Add(webBrowser);
            
            controlPanel.BringToFront();
            webBrowser.BringToFront();
        }

        /// <summary>
        /// 返回主界面
        /// </summary>
        private void ReturnToMainInterface()
        {
            // 移除WebBrowser控件和控制面板
            var webBrowser = this.Controls.OfType<WebBrowser>().FirstOrDefault();
            var controlPanel = this.Controls.OfType<Panel>().FirstOrDefault(p => p.Dock == DockStyle.Top && p.Height == 40);
            
            if (webBrowser != null)
            {
                this.Controls.Remove(webBrowser);
                webBrowser.Dispose();
            }
            
            if (controlPanel != null)
            {
                this.Controls.Remove(controlPanel);
                controlPanel.Dispose();
            }

            // 显示设备状态面板
            if (mainDevicePanel != null)
            {
                mainDevicePanel.Visible = true;
                mainDevicePanel.BringToFront();
            }
        }
        
        #region IRefreshableForm 实现
        
        /// <summary>
        /// 刷新设备数据
        /// </summary>
        public void RefreshDeviceData()
        {
            SafeUIUpdater.UpdateUI(this, () => 
            {
                InitializeDeviceStatusDisplay();
            });
        }
        
        /// <summary>
        /// 刷新员工数据（主界面不需要）
        /// </summary>
        public void RefreshEmployeeData()
        {
            // 主界面不需要刷新员工数据
        }
        
        /// <summary>
        /// 刷新门状态
        /// </summary>
        /// <param name="deviceId">设备ID</param>
        /// <param name="status">门状态</param>
        public void RefreshDoorStatus(string deviceId, DoorStatus status)
        {
            SafeUIUpdater.UpdateUI(this, () => 
            {
                UpdateDoorStatusInCards(deviceId, status);
            });
        }
        
        #endregion
        
        #region 数据变更事件处理
        
        // 处理设备数据变更事件
        private void OnDeviceDataChanged(object sender, DeviceDataChangedEventArgs e)
        {
            if (this.IsFormVisible)
            {
                // 根据变更类型执行不同的操作
                switch (e.ChangeType)
                {
                    case DeviceChangeType.Added:
                    case DeviceChangeType.Deleted:
                    case DeviceChangeType.Updated:
                        // 重新加载设备数据并刷新显示
                        DeviceConnectionManager.Instance.LoadAllDevices();
                        InitializeDeviceStatusDisplay();
                        break;
                }
            }
        }
        
        /// <summary>
        /// 处理门控状态变更事件
        /// </summary>
        private void OnDoorControlStatusChanged(object sender, DoorControlStatusChangedEventArgs e)
        {
            if (this.IsFormVisible)
            {
                RefreshDoorStatus(e.DeviceId, e.Status);
            }
        }
        
        /// <summary>
        /// 更新设备状态卡片中的门状态显示
        /// </summary>
        /// <param name="deviceId">设备ID</param>
        /// <param name="doorStatus">门状态</param>
        private void UpdateDoorStatusInCards(string deviceId, DoorStatus doorStatus)
        {
            lock (_statusUpdateLock)
            {
                try
                {
                    // 在所有设备卡片中查找匹配的设备
                    foreach (var kvp in deviceStatusCards)
                    {
                        var cardPanel = kvp.Value;
                        
                        // 查找设备名称标签，用于匹配设备
                        Label nameLabel = null;
                        Panel statusIcon = null;
                        Label statusLabel = null;
                        
                        foreach (Control control in cardPanel.Controls)
                        {
                            if (control is Label label)
                            {
                                if (label.Location.Y == 8) // 设备名称标签
                                {
                                    nameLabel = label;
                                }
                                else if (label.Location.X == 40 && label.Location.Y == 42) // 状态标签
                                {
                                    statusLabel = label;
                                }
                            }
                            else if (control is Panel panel && panel.Size.Width == 16)
                            {
                                statusIcon = panel;
                            }
                        }
                        
                        // 尝试匹配设备（可以根据名称或IP匹配）
                        if (nameLabel != null && (nameLabel.Text.Contains(deviceId) || deviceId.Contains(nameLabel.Text)))
                        {
                            UpdateCardDoorStatus(statusIcon, statusLabel, doorStatus);
                            break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"更新门状态卡片显示异常: {ex.Message}");
                }
            }
        }
        
        /// <summary>
        /// 更新卡片中的门状态显示
        /// </summary>
        /// <param name="statusIcon">状态图标</param>
        /// <param name="statusLabel">状态标签</param>
        /// <param name="doorStatus">门状态</param>
        private void UpdateCardDoorStatus(Panel statusIcon, Label statusLabel, DoorStatus doorStatus)
        {
            if (statusIcon == null || statusLabel == null) return;
            
            try
            {
                string statusText = GetDoorStatusDisplayText(doorStatus);
                Color statusColor = GetDoorStatusColor(doorStatus);
                
                // 更新状态显示
                statusIcon.BackColor = statusColor;
                statusLabel.Text = statusText;
                statusLabel.ForeColor = statusColor;
                
                // 重绘图标
                statusIcon.Invalidate();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"更新卡片门状态显示异常: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 获取门状态显示文本
        /// </summary>
        /// <param name="status">门状态</param>
        /// <returns>显示文本</returns>
        private string GetDoorStatusDisplayText(DoorStatus status)
        {
            return status switch
            {
                DoorStatus.Opened => "门已开启",
                DoorStatus.Closed => "门已关闭",
                DoorStatus.AlwaysOpen => "门常开",
                DoorStatus.AlwaysClosed => "门常闭",
                _ => "门状态未知"
            };
        }
        
        /// <summary>
        /// 获取门状态颜色
        /// </summary>
        /// <param name="status">门状态</param>
        /// <returns>状态颜色</returns>
        private Color GetDoorStatusColor(DoorStatus status)
        {
            return status switch
            {
                DoorStatus.Opened => Color.FromArgb(40, 167, 69), // 绿色
                DoorStatus.Closed => Color.FromArgb(220, 53, 69), // 红色
                DoorStatus.AlwaysOpen => Color.FromArgb(255, 193, 7), // 黄色
                DoorStatus.AlwaysClosed => Color.FromArgb(220, 53, 69), // 红色
                _ => Color.FromArgb(108, 117, 125) // 灰色
            };
        }
        
        #endregion

        /// <summary>
        /// 以置顶的拥有窗体方式显示子窗体，确保始终位于主界面之上
        /// </summary>
        /// <param name="form">要显示的窗体</param>
        private void ShowOwnedTopMost(Form form)
        {
            if (form == null) return;

            // 作为拥有窗体显示，避免被主界面上方卡片遮挡
            form.StartPosition = FormStartPosition.CenterParent;
            form.ShowInTaskbar = false;
            form.TopMost = true;
            // 不要在 FormClosed 中手动 Dispose，避免在显示阶段被过早释放
            // 子窗体应在自身 OnFormClosed 中清理资源

            try
            {
                form.Show(this);
                if (!form.IsDisposed)
                {
                    form.Activate();
                }
            }
            catch (ObjectDisposedException)
            {
                // 子窗体在 Load/Shown 中触发了关闭/释放，忽略
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"显示子窗体异常: {ex.Message}");
            }
        }
    }
}
