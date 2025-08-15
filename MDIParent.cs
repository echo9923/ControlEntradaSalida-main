using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
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
        
        //初始化窗体和控件
        public MDIParent()
        {
            InitializeComponent();
            InitializeDeviceStatusPanel();
        }
        // 初始化设备状态面板
        private void InitializeDeviceStatusPanel()
        {
            // 创建设备状态面板
            deviceStatusPanel = new FlowLayoutPanel();
            deviceStatusPanel.Dock = DockStyle.Top;
            deviceStatusPanel.Height = 120;
            deviceStatusPanel.BackColor = Color.Transparent;
            deviceStatusPanel.AutoScroll = true;
            deviceStatusPanel.WrapContents = true;
            deviceStatusPanel.FlowDirection = FlowDirection.LeftToRight;
            
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
        
        // 创建设备状态卡片
        private void CreateDeviceStatusCard(DeviceConnectionInfo device)
        {
            // 创建卡片面板
            Panel cardPanel = new Panel();
            cardPanel.Size = new Size(150, 100);
            cardPanel.BorderStyle = BorderStyle.FixedSingle;
            cardPanel.BackColor = Color.White;
            cardPanel.Margin = new Padding(10, 10, 10, 10);
            
            // 创建设备名称标签
            Label nameLabel = new Label();
            nameLabel.Text = device.Name;
            nameLabel.Font = new Font(nameLabel.Font, FontStyle.Bold);
            nameLabel.TextAlign = ContentAlignment.MiddleCenter;
            nameLabel.Dock = DockStyle.Top;
            nameLabel.Height = 20;
            nameLabel.ForeColor = Color.Black;
            
            // 创建状态面板
            Panel statusPanel = new Panel();
            statusPanel.Dock = DockStyle.Fill;
            statusPanel.BackColor = Color.Transparent;
            
            // 创建状态图标
            PictureBox statusIcon = new PictureBox();
            statusIcon.Size = new Size(30, 30);
            statusIcon.Location = new Point(10, 20);
            statusIcon.SizeMode = PictureBoxSizeMode.StretchImage;
            
            // 创建状态标签
            Label statusLabel = new Label();
            statusLabel.Size = new Size(100, 30);
            statusLabel.Location = new Point(50, 20);
            statusLabel.TextAlign = ContentAlignment.MiddleLeft;
            
            // 根据设备状态设置初始显示
            UpdateStatusDisplay(device, statusIcon, statusLabel);
            
            // 将控件添加到状态面板
            statusPanel.Controls.Add(statusIcon);
            statusPanel.Controls.Add(statusLabel);
            
            // 将控件添加到卡片面板
            cardPanel.Controls.Add(nameLabel);
            cardPanel.Controls.Add(statusPanel);
            
            // 将卡片面板添加到主面板和字典中
            deviceStatusPanel.Controls.Add(cardPanel);
            deviceStatusCards.Add(device.Id, cardPanel);
        }
        
        // 更新设备状态卡片
        private void UpdateDeviceStatusCard(DeviceConnectionInfo device)
        {
            if (deviceStatusCards.ContainsKey(device.Id))
            {
                Panel cardPanel = deviceStatusCards[device.Id];
                
                // 查找状态图标和标签
                PictureBox statusIcon = null;
                Label statusLabel = null;
                
                // 遍历控件查找状态图标和标签
                foreach (Control control in cardPanel.Controls)
                {
                    if (control is Panel)
                    {
                        foreach (Control innerControl in control.Controls)
                        {
                            if (innerControl is PictureBox)
                            {
                                statusIcon = (PictureBox)innerControl;
                            }
                            else if (innerControl is Label)
                            {
                                statusLabel = (Label)innerControl;
                            }
                        }
                    }
                }
                
                // 更新状态显示
                if (statusIcon != null && statusLabel != null)
                {
                    UpdateStatusDisplay(device, statusIcon, statusLabel);
                }
            }
        }
        
        // 更新状态显示
        private void UpdateStatusDisplay(DeviceConnectionInfo device, PictureBox statusIcon, Label statusLabel)
        {
            // 如果设备被禁用，显示为红色
            if (!device.IsEnabled)
            {
                statusIcon.BackColor = Color.Red; // 禁用状态显示红色
                statusLabel.Text = "已禁用";
                statusLabel.ForeColor = Color.Red;
                return;
            }
            
            // 根据设备状态设置图标颜色和标签文本
            switch (device.Status)
            {
                case DeviceStatus.Online:
                    statusIcon.BackColor = Color.Green; // 在线状态显示绿色
                    statusLabel.Text = "在线";
                    statusLabel.ForeColor = Color.Green;
                    break;
                case DeviceStatus.Offline:
                    statusIcon.BackColor = Color.Gray; // 离线状态显示灰色
                    statusLabel.Text = "离线";
                    statusLabel.ForeColor = Color.Gray;
                    break;
                case DeviceStatus.AlwaysOpen:
                    statusIcon.BackColor = Color.Yellow; // 常开状态显示黄色
                    statusLabel.Text = "常开";
                    statusLabel.ForeColor = Color.Orange;
                    break;
                case DeviceStatus.AlwaysClose:
                    statusIcon.BackColor = Color.Red; // 常闭状态显示红色
                    statusLabel.Text = "常闭";
                    statusLabel.ForeColor = Color.Red;
                    break;
                default:
                    statusIcon.BackColor = Color.Gray; // 未知状态显示灰色
                    statusLabel.Text = "未知";
                    statusLabel.ForeColor = Color.Gray;
                    break;
            }
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
